using AionCoreBot.Domain.Models;
using AionCoreBot.Infrastructure.Interfaces;
using AionCoreBot.Worker;

public class Worker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;

    public Worker(IServiceProvider serviceProvider, IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await InitializeAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.UtcNow;
                var nextMinute = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0).AddMinutes(1);
                var delay = nextMinute - now;
                await Task.Delay(delay + TimeSpan.FromSeconds(2), stoppingToken);

                using var scope = _serviceProvider.CreateScope();

                var candleDownloadService = scope.ServiceProvider.GetRequiredService<ICandleDownloadService>();
                var candleRepository = scope.ServiceProvider.GetRequiredService<ICandleRepository>();
                var candleAggregator = scope.ServiceProvider.GetRequiredService<CandleAggregator>();

                var symbols = _configuration.GetSection("BinanceExchange:EURPairs").Get<List<string>>() ?? new();
                var intervals = _configuration.GetSection("TimeIntervals:AvailableIntervals").Get<List<string>>() ?? new();

                Console.WriteLine($"[LOOP] {DateTime.UtcNow:HH:mm:ss} Starting candle download...");

                foreach (var symbol in symbols)
                {
                    foreach (var interval in intervals)
                    {
                        DateTime from = await GetDownloadStartDateAsync(candleRepository, symbol, interval);
                        DateTime to = DateTime.UtcNow;

                        if (from >= to)
                            continue;

                        var candles = await candleDownloadService.GetHistoricalCandlesAsync(symbol, interval, from, to);

                        if (candles != null && candles.Any())
                        {
                            Console.WriteLine($"[LOOP] Downloaded {candles.Count} candles for {symbol} ({interval})");

                            foreach (var candle in candles)
                                await candleRepository.AddAsync(candle);

                            await candleRepository.SaveChangesAsync();
                        }
                        else
                        {
                            Console.WriteLine($"[LOOP] No new candles for {symbol} ({interval})");
                        }
                    }
                }

                await candleAggregator.AggregateAsync();
                Console.WriteLine($"[LOOP] Candle aggregation complete.\n");
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("[LOOP] Shutdown requested, stopping gracefully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR - Loop] {ex.Message}");
            }
        }
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
        return lastCandle != null
            ? lastCandle.CloseTime.AddMilliseconds(1)
            : DateTime.UtcNow.AddDays(-14); // Initieel 14 dagen terug
    }
}
