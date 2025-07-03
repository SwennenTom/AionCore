using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AionCoreBot.Application.Signals.Interfaces;
using AionCoreBot.Infrastructure.Interfaces;

namespace AionCoreBot.Application.Signals.Services
{
    public class SignalInitializationService : ISignalInitializationService
    {
        private readonly ICandleRepository _candleRepository;
        private readonly ISignalEvaluatorService _signalEvaluator;
        private readonly List<string> _symbols;
        private readonly List<string> _intervals;

        public SignalInitializationService(
            ICandleRepository candleRepository,
            ISignalEvaluatorService signalEvaluator,
            IConfiguration configuration)
        {
            _candleRepository = candleRepository;
            _signalEvaluator = signalEvaluator;
            _symbols = configuration.GetSection("BinanceExchange:EURPairs").Get<List<string>>() ?? new();
            _intervals = configuration.GetSection("TimeIntervals:AvailableIntervals").Get<List<string>>() ?? new();
        }

        public async Task EvaluateHistoricalSignalsAsync(CancellationToken cancellationToken = default)
        {
            var allowedIntervals = new HashSet<string> { "1h", "4h", "1d" };

            foreach (var symbol in _symbols)
            {
                foreach (var interval in _intervals.Where(i => allowedIntervals.Contains(i)))
                {
                    Console.WriteLine($"[INIT] Evaluating historical signals for {symbol} ({interval})...");
                    var candles = await _candleRepository.GetBySymbolAndIntervalAsync(symbol, interval);
                    await _signalEvaluator.EvaluateAllAsync(symbol, interval, candles);
                    await _candleRepository.SaveChangesAsync();
                }
            }
        }
    }
}
