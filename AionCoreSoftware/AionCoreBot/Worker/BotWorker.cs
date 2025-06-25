using AionCoreBot.Application.Interfaces;
using AionCoreBot.Application.Services;
using AionCoreBot.Domain.Models;
using AionCoreBot.Infrastructure.Comms.Websocket;
using AionCoreBot.Infrastructure.Converters;
using AionCoreBot.Infrastructure.Interfaces;
using AionCoreBot.Worker;
using AionCoreBot.Worker.Interfaces;
using AionCoreBot.Worker.Services;
using System.Threading;

public class BotWorker
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly BinanceWebSocketService _webSocketService;

    public BotWorker(IServiceProvider serviceProvider, IConfiguration configuration, BinanceWebSocketService webSocketService)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _webSocketService = webSocketService;
    }
    public async Task RunAsync(CancellationToken stoppingToken)
    {
        var symbols = _configuration.GetSection("BinanceExchange:EURPairs").Get<List<string>>() ?? new();

        #region Initialisation
        Console.WriteLine("[BOOT] Clearing old candle and indicator data...");
        await ClearAllDataAsync();

        Console.WriteLine("[BOOT] Starting WebSocket...");
        var webSocketTask = _webSocketService.StartAsync(symbols, stoppingToken);

        Console.WriteLine("[BOOT] Starting historical initialization...");
        await DownloadHistoricalCandlesAsync(stoppingToken);

        Console.WriteLine("[BOOT] Calculating historical indicators...");
        using (var scope = _serviceProvider.CreateScope())
        {
            var analyzerOrchestrator = scope.ServiceProvider.GetRequiredService<IAnalyzerWorker>();
            await analyzerOrchestrator.RunAllAsync();
        }

        Console.WriteLine("[BOOT] Evaluating historical signals...");
        await EvaluateHistoricalSignalsAsync(stoppingToken);

        Console.WriteLine("[BOOT] Init complete. Switching to live mode.");
        #endregion

        // Periodieke aggregatie opstarten
        _ = Task.Run(async () =>
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
        }, stoppingToken);

        await webSocketTask;
    }


    private async Task ClearAllDataAsync()
    {
        using var scope = _serviceProvider.CreateScope();

        var candleRepository = scope.ServiceProvider.GetRequiredService<ICandleRepository>();
        var emaRepository = scope.ServiceProvider.GetRequiredService<IIndicatorRepository<EMAResult>>();
        var rsiRepository = scope.ServiceProvider.GetRequiredService<IIndicatorRepository<RSIResult>>();
        var atrRepository = scope.ServiceProvider.GetRequiredService<IIndicatorRepository<ATRResult>>();
        var signalRepository = scope.ServiceProvider.GetRequiredService<ISignalEvaluationRepository>();

        await candleRepository.ClearAllAsync();
        await emaRepository.ClearAllAsync();
        await rsiRepository.ClearAllAsync();
        await atrRepository.ClearAllAsync();
        await signalRepository.ClearAllAsync();
    }
    private async Task DownloadHistoricalCandlesAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();

        var candleDownloadService = scope.ServiceProvider.GetRequiredService<ICandleDownloadService>();
        var candleRepository = scope.ServiceProvider.GetRequiredService<ICandleRepository>();

        var symbols = _configuration.GetSection("BinanceExchange:EURPairs").Get<List<string>>() ?? new();
        var intervals = _configuration.GetSection("TimeIntervals:AvailableIntervals").Get<List<string>>() ?? new();

        DateTime now = DateTime.UtcNow;

        Console.WriteLine("[INIT] Downloading 14 days of historical candles...");

        foreach (var symbol in symbols)
        {
            foreach (var interval in intervals)
            {
                DateTime from = now.AddDays(-14);
                DateTime to = now;

                var candles = await candleDownloadService.GetHistoricalCandlesAsync(symbol, interval, from, to);

                if (candles != null && candles.Any())
                {
                    Console.WriteLine($"[INIT] {candles.Count} candles for {symbol} ({interval})");

                    foreach (var candle in candles)
                        await candleRepository.AddAsync(candle);

                    await candleRepository.SaveChangesAsync();
                }
            }
        }

        Console.WriteLine("[INIT] Candle download complete.");
    }
    private async Task EvaluateHistoricalSignalsAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();

        var candleRepository = scope.ServiceProvider.GetRequiredService<ICandleRepository>();
        var signalEvaluator = scope.ServiceProvider.GetRequiredService<ISignalEvaluatorService>();

        var symbols = _configuration.GetSection("BinanceExchange:EURPairs").Get<List<string>>() ?? new();
        var intervals = _configuration.GetSection("TimeIntervals:AvailableIntervals").Get<List<string>>() ?? new();

        foreach (var symbol in symbols)
        {
            var evaluationIntervals = new[] { "1h", "4h", "1d" };

            foreach (var interval in intervals.Where(i => evaluationIntervals.Contains(i)))
            {
                var candles = await candleRepository.GetBySymbolAndIntervalAsync(symbol, interval);

                if (candles != null && candles.Any())
                {
                    var evaluationPoints = candles  .Select(c => c.CloseTime.RoundUpToNextMinute())
                                                    .Distinct()
                                                    .OrderBy(t => t)
                                                    .ToList();



                    if (evaluationPoints.Count > 0)
                    {
                        Console.WriteLine($"[INIT] Evaluating signals for {symbol} ({interval})...");
                        await signalEvaluator.EvaluateHistoricalSignalsAsync(symbol, interval, evaluationPoints);
                    }
                }
            }
        }

        Console.WriteLine("[INIT] Historical signal evaluation complete.");
    }



}
