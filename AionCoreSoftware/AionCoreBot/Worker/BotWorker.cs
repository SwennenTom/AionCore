using AionCoreBot.Domain.Models;
using AionCoreBot.Infrastructure.Comms.Websocket;
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

        Console.WriteLine("[BOOT] Clearing old candle and indicator data...");
        await ClearAllDataAsync();

        Console.WriteLine("[BOOT] Starting WebSocket...");
        var webSocketTask = _webSocketService.StartAsync(symbols, stoppingToken);

        Console.WriteLine("[BOOT] Starting historical initialization...");
        await InitializeAsync(stoppingToken);

        Console.WriteLine("[BOOT] Init complete. Starting analyzers...");
        using (var scope = _serviceProvider.CreateScope())
        {
            var analyzerOrchestrator = scope.ServiceProvider.GetRequiredService<IAnalyzerWorker>();
            await analyzerOrchestrator.RunAllAsync();
        }

        await webSocketTask;
    }

    private async Task InitializeAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();

            var candleDownloadService = scope.ServiceProvider.GetRequiredService<ICandleDownloadService>();
            var candleRepository = scope.ServiceProvider.GetRequiredService<ICandleRepository>();            

            var symbols = _configuration.GetSection("BinanceExchange:EURPairs").Get<List<string>>() ?? new();
            var intervals = _configuration.GetSection("TimeIntervals:AvailableIntervals").Get<List<string>>() ?? new();

            DateTime now = DateTime.UtcNow;
            DateTime? minFrom = null;
            DateTime? maxTo = null;

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

                        minFrom = minFrom == null ? from : (from < minFrom ? from : minFrom);
                        maxTo = maxTo == null ? to : (to > maxTo ? to : maxTo);
                    }
                }
            }

            Console.WriteLine("[INIT] Initialization complete.\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR - Init] {ex.Message}");
        }
    }

    private async Task ClearAllDataAsync()
    {
        using var scope = _serviceProvider.CreateScope();

        var candleRepository = scope.ServiceProvider.GetRequiredService<ICandleRepository>();
        var emaRepository = scope.ServiceProvider.GetRequiredService<IIndicatorRepository<EMAResult>>();
        var rsiRepository = scope.ServiceProvider.GetRequiredService<IIndicatorRepository<RSIResult>>();
        var atrRepository = scope.ServiceProvider.GetRequiredService<IIndicatorRepository<ATRResult>>();

        await candleRepository.ClearAllAsync();
        await emaRepository.ClearAllAsync();
        await rsiRepository.ClearAllAsync();
        await atrRepository.ClearAllAsync();
    }

}
