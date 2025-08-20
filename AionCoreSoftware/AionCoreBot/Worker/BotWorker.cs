using AionCoreBot;
using AionCoreBot.Application.Account.Interfaces;
using AionCoreBot.Application.Analysis.Indicators;
using AionCoreBot.Application.Analysis.Interfaces;
using AionCoreBot.Application.Candles.Interfaces;
using AionCoreBot.Application.Candles.Services;
using AionCoreBot.Application.Logging;
using AionCoreBot.Application.Maintenance;
using AionCoreBot.Application.Signals.Interfaces;
using AionCoreBot.Application.Signals.Services;
using AionCoreBot.Application.Strategy.Interfaces;
using AionCoreBot.Application.Strategy.Services;
using AionCoreBot.Application.Trades.Interfaces;
using AionCoreBot.Domain.Enums;
using AionCoreBot.Domain.Models;
using AionCoreBot.Helpers;
using AionCoreBot.Helpers.Converters;
using AionCoreBot.Infrastructure.Comms.Clients;
using AionCoreBot.Infrastructure.Comms.Websocket;
using AionCoreBot.Infrastructure.Data;
using AionCoreBot.Infrastructure.Interfaces;
using AionCoreBot.Infrastructure.Repositories;
using AionCoreBot.Worker;
using AionCoreBot.Worker.Interfaces;
using AionCoreBot.Worker.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Threading;
using static System.Formats.Asn1.AsnWriter;

public class BotWorker
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly IAccountSyncService _accountSyncService;
    private readonly ITradeManager _tradeManager;
    private readonly ILogService _logService;

    public BotWorker(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        IAccountSyncService accountSyncService,
        ITradeManager tradeManager,
        ILogService logService)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _accountSyncService = accountSyncService;
        _tradeManager = tradeManager;
        _logService = logService;
    }

    public async Task RunAsync(CancellationToken stoppingToken)
    {
        var symbols = _configuration.GetSection("BinanceExchange:EURPairs").Get<List<string>>() ?? new();

        #region Initialisatie
        await _logService.LogAsync("[BOOT] Cleaning up old data...", LogClass.Info, nameof(BotWorker));
        using (var scope = _serviceProvider.CreateScope())
        {
            var cleanupService = scope.ServiceProvider.GetRequiredService<IDataCleanupService>();
            await cleanupService.ClearAllDataAsync();
        }

        await _logService.LogAsync("[BOOT] Fetching Account Data", LogClass.Info, nameof(BotWorker));
        await _accountSyncService.InitializeAsync();

        await _logService.LogAsync("[BOOT] Downloading historical candles...", LogClass.Info, nameof(BotWorker));
        using (var scope = _serviceProvider.CreateScope())
        {
            var candleService = scope.ServiceProvider.GetRequiredService<ICandleInitializationService>();
            await candleService.DownloadHistoricalCandlesAsync(stoppingToken);
        }

        await _logService.LogAsync("[BOOT] Calculating historical indicators...", LogClass.Info, nameof(BotWorker));
        using (var scope = _serviceProvider.CreateScope())
        {
            var analyzerOrchestrator = scope.ServiceProvider.GetRequiredService<IAnalyzerWorker>();
            await analyzerOrchestrator.RunAllAsync();
        }

        await _logService.LogAsync("[BOOT] Evaluating historical signals...", LogClass.Info, nameof(BotWorker));
        using (var scope = _serviceProvider.CreateScope())
        {
            var signalInitService = scope.ServiceProvider.GetRequiredService<ISignalInitializationService>();
            await signalInitService.EvaluateHistoricalSignalsAsync(stoppingToken);
        }

        await _logService.LogAsync("[BOOT] Executing Trading Strategy on startup", LogClass.Info, nameof(BotWorker));
        using (var scope = _serviceProvider.CreateScope())
        {
            var strategyService = scope.ServiceProvider.GetRequiredService<IStrategyService>();
            await strategyService.ExecuteStrategyAsync(stoppingToken);
            await _logService.LogAsync($"[STRATEGY] Uitgevoerd na initialisatie ({DateTime.UtcNow:HH:mm} UTC)", LogClass.Info, nameof(BotWorker));
        }

        await _logService.LogAsync("[BOOT] Init complete. Switching to live mode.", LogClass.Info, nameof(BotWorker));
        #endregion

        await StartLiveLoopAsync(stoppingToken);
    }

    public async Task StartLiveLoopAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // ✅ 1. Bereken delay tot volgende 4h candle sluit
                var delay = GetDelayUntilNext4hCandle();
                var wakeTime = DateTime.UtcNow + delay;

                await _logService.LogAsync(
                    $"[SLEEP] Slaap {delay.TotalMinutes:F0} minuten, wakker om {wakeTime:yyyy-MM-dd HH:mm} UTC",
                    LogClass.Info,
                    nameof(BotWorker));

                if (delay.TotalMilliseconds > 0)
                    await Task.Delay(delay, stoppingToken);

                await _logService.LogAsync($"[CANDLE] Nieuwe 4h-candle gesloten om {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC",
                    LogClass.Info,
                    nameof(BotWorker));

                using var scope = _serviceProvider.CreateScope();

                // ✅ 2. Candles aggregeren (4h)
                var aggregator = scope.ServiceProvider.GetRequiredService<CandleAggregator>();
                await aggregator.AggregateAsync();

                // ✅ 3. Strategie uitvoeren
                var strategyService = scope.ServiceProvider.GetRequiredService<IStrategyService>();
                await strategyService.ExecuteStrategyAsync(stoppingToken);
                await _logService.LogAsync($"[STRATEGY] Uitgevoerd na 4h-sluiting ({DateTime.UtcNow:HH:mm} UTC)",
                    LogClass.Info,
                    nameof(BotWorker));

                // ✅ 4. Open trades syncen met Binance
                await _tradeManager.SyncWithExchangeAsync(stoppingToken);
                await _logService.LogAsync("[TRADESYNC] Open trades gesynchroniseerd met Binance",
                    LogClass.Info,
                    nameof(BotWorker));
            }
            catch (Exception ex)
            {
                await _logService.LogAsync($"[BOTWORKER ERROR] {ex.Message}",
                    LogClass.Error,
                    nameof(BotWorker),
                    ex.ToString());
            }
        }
    }

    private TimeSpan GetDelayUntilNext4hCandle()
    {
        var now = DateTime.UtcNow;

        // ✅ 4h blokken zijn 0, 4, 8, 12, 16, 20
        var currentBlockStartHour = (now.Hour / 4) * 4;

        // Starttijd van dit blok
        var currentBlockStart = new DateTime(now.Year, now.Month, now.Day, currentBlockStartHour, 0, 0, DateTimeKind.Utc);

        // Volgend blok is altijd +4h
        var nextBlock = currentBlockStart.AddHours(4);

        // Als nextBlock al in het verleden ligt, nog een extra +4h
        if (nextBlock <= now)
            nextBlock = nextBlock.AddHours(4);

        return nextBlock - now;
    }
}
