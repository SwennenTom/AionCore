using AionCoreBot.Application.Interfaces;
using AionCoreBot.Application.Services;
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

public class BotWorker
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;

    public BotWorker(IServiceProvider serviceProvider, IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
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
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var aggregator = scope.ServiceProvider.GetRequiredService<CandleAggregator>();
                await aggregator.AggregateAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AGGREGATOR ERROR] {ex.Message}");
            }

            await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
        }
    }
}
