using AionCoreBot.Domain.Models;
using AionCoreBot.Infrastructure.Interfaces;
using AionCoreBot.Infrastructure.Websocket;
using AionCoreBot.Worker;
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
        await InitializeAsync(stoppingToken);

        var symbols = _configuration.GetSection("BinanceExchange:EURPairs").Get<List<string>>() ?? new();
        await _webSocketService.StartAsync(symbols, stoppingToken);        
    }

    private async Task InitializeAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();

            var candleDownloadService = scope.ServiceProvider.GetRequiredService<ICandleDownloadService>();
            var candleRepository = scope.ServiceProvider.GetRequiredService<ICandleRepository>();
            var candleAggregator = scope.ServiceProvider.GetRequiredService<CandleAggregator>();

            var emaRepository = scope.ServiceProvider.GetRequiredService<IIndicatorRepository<EMAResult>>();
            var rsiRepository = scope.ServiceProvider.GetRequiredService<IIndicatorRepository<RSIResult>>();
            var atrRepository = scope.ServiceProvider.GetRequiredService<IIndicatorRepository<ATRResult>>();

            Console.WriteLine("[INIT] Clearing old candle and indicator data...");

            await candleRepository.ClearAllAsync();
            await emaRepository.ClearAllAsync();
            await rsiRepository.ClearAllAsync();
            await atrRepository.ClearAllAsync();

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

    private async Task<DateTime> GetDownloadStartDateAsync(ICandleRepository candleRepository, string symbol, string interval)
    {
        var lastCandle = await candleRepository.GetLastCandleAsync(symbol, interval);
        if (lastCandle != null)
        {
            return AlignToIntervalStart(lastCandle.CloseTime, interval);
        }

        return AlignToIntervalStart(DateTime.UtcNow.AddDays(-14), interval);
    }
        private static DateTime AlignToIntervalStart(DateTime time, string interval)
    {
        return interval switch
        {
            "1m" => new DateTime(time.Year, time.Month, time.Day, time.Hour, time.Minute, 0, DateTimeKind.Utc).AddMinutes(1),
            "5m" => new DateTime(time.Year, time.Month, time.Day, time.Hour, time.Minute / 5 * 5, 0, DateTimeKind.Utc).AddMinutes(5),
            "15m" => new DateTime(time.Year, time.Month, time.Day, time.Hour, time.Minute / 15 * 15, 0, DateTimeKind.Utc).AddMinutes(15),
            "1h" => new DateTime(time.Year, time.Month, time.Day, time.Hour, 0, 0, DateTimeKind.Utc).AddHours(1),
            "4h" => new DateTime(time.Year, time.Month, time.Day, time.Hour / 4 * 4, 0, 0, DateTimeKind.Utc).AddHours(4),
            "1d" => new DateTime(time.Year, time.Month, time.Day, 0, 0, 0, DateTimeKind.Utc).AddDays(1),
            _ => throw new ArgumentException($"Unsupported interval: {interval}")
        };
    }


}
