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
                using var scope = _serviceProvider.CreateScope();

                // Aggregator elke minuut
                var aggregator = scope.ServiceProvider.GetRequiredService<CandleAggregator>();
                await aggregator.AggregateAsync();

                // Strategie check
                var now = DateTime.UtcNow;
                var currentHour = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0);

                if (currentHour > lastStrategyExecutionHour)
                {
                    var strategyService = scope.ServiceProvider.GetRequiredService<IStrategyService>();
                    await strategyService.ExecuteStrategyAsync(stoppingToken);
                    lastStrategyExecutionHour = currentHour;

                    Console.WriteLine($"[STRATEGY] Uitgevoerd om {currentHour:HH:mm} UTC");
                }

                //TradeSync
                await _tradeManager.SyncWithExchangeAsync(stoppingToken);


            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BOTWORKER ERROR] {ex.Message}");
            }

            await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
        }
    }

}
