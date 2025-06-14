using AionCoreBot.Domain.Models;
using AionCoreBot.Infrastructure.Interfaces;
using AionCoreBot.Domain.Interfaces;

namespace AionCoreBot.Worker.Indicators
{
    internal class EMAService : IEMAService
    {
        private readonly ICandleRepository _candleRepository;
        private readonly IIndicatorRepository<EMAResult> _emaRepository;

        public EMAService(ICandleRepository candleRepository, IIndicatorRepository<EMAResult> emaRepository)
        {
            _candleRepository = candleRepository;
            _emaRepository = emaRepository;
        }

        public async Task<EMAResult> CalculateAsync(string symbol, string interval, int period, DateTime startTime, DateTime endTime)
        {
            // 1. Candles ophalen
            var candles = await _candleRepository.GetCandlesAsync(symbol, interval, startTime, endTime);

            if (candles == null || !candles.Any())
                throw new InvalidOperationException("Geen candles beschikbaar voor EMA-berekening");

            // 2. Sorteren op tijd
            var ordered = candles.OrderBy(c => c.OpenTime).ToList();

            // 3. EMA berekenen
            double multiplier = 2.0 / (period + 1);
            double? previousEma = null;

            foreach (var candle in ordered)
            {
                if (previousEma == null)
                {
                    previousEma = (double)candle.ClosePrice; // startwaarde is eerste close
                }
                else
                {
                    previousEma = ((double)candle.ClosePrice - previousEma) * multiplier + previousEma;
                }
            }

            if (previousEma == null)
                throw new InvalidOperationException("EMA kon niet worden berekend");

            // 4. Resultaat aanmaken
            var result = new EMAResult
            {
                Symbol = symbol,
                Interval = interval,
                Period = period,
                Value = (decimal)previousEma,
                Timestamp = ordered.Last().CloseTime
            };

            await _emaRepository.AddAsync(result);
            await _emaRepository.SaveChangesAsync();

            return result;
        }
    }
}
