using AionCoreBot.Domain.Models;
using AionCoreBot.Helpers;
using AionCoreBot.Helpers.Converters;
using AionCoreBot.Infrastructure.Comms.Websocket;
using AionCoreBot.Infrastructure.Interfaces;
using AionCoreBot.Worker;
using AionCoreBot.Worker.Interfaces;
using AionCoreBot.Worker.Services;
using System.Threading;
using Microsoft.Extensions.Configuration;
using AionCoreBot.Application.Signals.Interfaces;
using AionCoreBot.Application.Maintenance;
using AionCoreBot;
using AionCoreBot.Application.Account.Interfaces;
using AionCoreBot.Application.Analysis.Indicators;
using AionCoreBot.Application.Analysis.Interfaces;
using AionCoreBot.Application.Candles.Interfaces;
using AionCoreBot.Application.Candles.Services;
using AionCoreBot.Application.Signals.Services;
using AionCoreBot.Application.Strategy.Interfaces;
using AionCoreBot.Application.Strategy.Services;
using AionCoreBot.Application.Trades.Interfaces;
using AionCoreBot.Infrastructure.Comms.Clients;
using AionCoreBot.Infrastructure.Data;
using AionCoreBot.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

public class BotWorker
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly IAccountSyncService _accountSyncService;
    private readonly ITradeManager _tradeManager;

    public BotWorker(IServiceProvider serviceProvider, IConfiguration configuration, IAccountSyncService accountSyncService, ITradeManager tradeManager)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _accountSyncService = accountSyncService;
        _tradeManager = tradeManager;
    }

    public async Task RunAsync(CancellationToken stoppingToken)
    {
        var symbols = _configuration.GetSection("BinanceExchange:EURPairs").Get<List<string>>() ?? new();

        #region Initialisatie
        Console.WriteLine("[BOOT] Cleaning up old data...");
        using (var scope = _serviceProvider.CreateScope())
        {
            var cleanupService = scope.ServiceProvider.GetRequiredService<IDataCleanupService>();
            await cleanupService.ClearAllDataAsync();
        }

        Console.WriteLine("[BOOT] Fetching Account Data");
        await _accountSyncService.InitializeAsync();


        Console.WriteLine("[BOOT] Downloading historical candles...");
        using (var scope = _serviceProvider.CreateScope())
        {
            var candleService = scope.ServiceProvider.GetRequiredService<ICandleInitializationService>();
            await candleService.DownloadHistoricalCandlesAsync(stoppingToken);
        }

        Console.WriteLine("[BOOT] Calculating historical indicators...");
        using (var scope = _serviceProvider.CreateScope())
        {
            var analyzerOrchestrator = scope.ServiceProvider.GetRequiredService<IAnalyzerWorker>();
            await analyzerOrchestrator.RunAllAsync();
        }

        Console.WriteLine("[BOOT] Evaluating historical signals...");
        using (var scope = _serviceProvider.CreateScope())
        {
            var signalInitService = scope.ServiceProvider.GetRequiredService<ISignalInitializationService>();
            await signalInitService.EvaluateHistoricalSignalsAsync(stoppingToken);
        }

        Console.WriteLine("[BOOT] Init complete. Switching to live mode.");
        #endregion

        await StartLiveLoopAsync(stoppingToken);
    }

    public async Task StartLiveLoopAsync(CancellationToken stoppingToken)
    {
        DateTime lastStrategyExecutionHour = DateTime.MinValue;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // ✅ 1. Bereken hoeveel tijd tot volgende 4h-candle sluit
                var delay = GetDelayUntilNext4hCandle();
                var wakeTime = DateTime.UtcNow + delay;

                Console.WriteLine($"[SLEEP] Slaap {delay.TotalMinutes:F0} minuten, wakker om {wakeTime:yyyy-MM-dd HH:mm} UTC");

                await Task.Delay(delay, stoppingToken);

                // ✅ 2. Zodra nieuwe 4h-candle sluit → alles uitvoeren
                Console.WriteLine($"[CANDLE] Nieuwe 4h-candle gesloten om {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC");

                using var scope = _serviceProvider.CreateScope();

                // ✅ 3. Candles aggregeren (voor 4h)
                var aggregator = scope.ServiceProvider.GetRequiredService<CandleAggregator>();
                await aggregator.AggregateAsync();

                // ✅ 4. Strategie uitvoeren
                var strategyService = scope.ServiceProvider.GetRequiredService<IStrategyService>();
                await strategyService.ExecuteStrategyAsync(stoppingToken);
                Console.WriteLine($"[STRATEGY] Uitgevoerd na 4h-sluiting ({DateTime.UtcNow:HH:mm} UTC)");

                // ✅ 5. Open trades syncen met Binance
                await _tradeManager.SyncWithExchangeAsync(stoppingToken);
                Console.WriteLine("[TRADESYNC] Open trades gesynchroniseerd met Binance");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BOTWORKER ERROR] {ex.Message}");
            }
        }
    }
    private TimeSpan GetDelayUntilNext4hCandle()
    {
        var now = DateTime.UtcNow;

        // 4h blokken: 0,4,8,12,16,20
        var nextBlockHour = ((now.Hour / 4) + 1) * 4;
        if (nextBlockHour >= 24)
            nextBlockHour -= 24;

        var nextBoundary = new DateTime(
            now.Year,
            now.Month,
            now.Day,
            nextBlockHour,
            0,
            0,
            DateTimeKind.Utc
        );

        if (nextBoundary <= now)
            nextBoundary = nextBoundary.AddHours(4);

        return nextBoundary - now;
    }


}
