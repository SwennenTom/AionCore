using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AionCoreBot.Application.Candles.Interfaces;
using AionCoreBot.Infrastructure.Interfaces;

namespace AionCoreBot.Application.Candles.Services
{
    public class CandleInitializationService : ICandleInitializationService
    {
        private readonly ICandleRepository _candleRepository;
        private readonly ICandleDownloadService _downloadService;
        private readonly List<string> _symbols;
        private readonly List<string> _intervals;

        public CandleInitializationService(
            ICandleRepository candleRepository,
            ICandleDownloadService downloadService,
            IConfiguration configuration)
        {
            _candleRepository = candleRepository;
            _downloadService = downloadService;
            _symbols = configuration.GetSection("BinanceExchange:EURPairs").Get<List<string>>() ?? new();
            _intervals = configuration.GetSection("TimeIntervals:AvailableIntervals").Get<List<string>>() ?? new();
        }

        public async Task DownloadHistoricalCandlesAsync(CancellationToken cancellationToken = default)
        {
            foreach (var symbol in _symbols)
            {
                foreach (var interval in _intervals)
                {
                    var candles = await _downloadService.DownloadCandlesAsync(symbol, interval, days: 14);
                    await _candleRepository.AddRangeAsync(candles);
                    await _candleRepository.SaveChangesAsync();
                }
            }
        }
    }
}
