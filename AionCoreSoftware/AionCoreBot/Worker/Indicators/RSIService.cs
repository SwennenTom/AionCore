using AionCoreBot.Domain.Interfaces;
using AionCoreBot.Domain.Models;
using AionCoreBot.Infrastructure.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AionCoreBot.Worker.Indicators
{
    internal class RSIService : IRSIService
    {
        private readonly ICandleRepository _candleRepository;
        private readonly IIndicatorRepository<RSIResult> _rsiRepository;

        public RSIService(ICandleRepository candleRepository, IIndicatorRepository<RSIResult> rsiRepository)
        {
            _candleRepository = candleRepository;
            _rsiRepository = rsiRepository;
        }

        public async Task<RSIResult> CalculateAsync(string symbol, string interval, int period, DateTime startTime, DateTime endTime)
        {
            var candles = await _candleRepository.GetCandlesAsync(symbol, interval, startTime, endTime);

            if (candles == null || candles.Count() < period + 1)
                throw new InvalidOperationException("Niet genoeg candles voor RSI-berekening");

            var ordered = candles.OrderBy(c => c.OpenTime).ToList();

            var gains = new List<decimal>();
            var losses = new List<decimal>();

            for (int i = 1; i < period + 1; i++)
            {
                var change = ordered[i].ClosePrice - ordered[i - 1].ClosePrice;
                if (change > 0)
                    gains.Add(change);
                else
                    losses.Add(Math.Abs(change));
            }

            decimal avgGain = gains.DefaultIfEmpty(0).Average();
            decimal avgLoss = losses.DefaultIfEmpty(0).Average();

            decimal rs = avgLoss == 0 ? 100 : avgGain / avgLoss;
            decimal rsi = 100 - (100 / (1 + rs));

            // Wilder's smoothing toepassen voor resterende candles
            for (int i = period + 1; i < ordered.Count; i++)
            {
                var change = ordered[i].ClosePrice - ordered[i - 1].ClosePrice;
                decimal gain = change > 0 ? change : 0;
                decimal loss = change < 0 ? Math.Abs(change) : 0;

                avgGain = ((avgGain * (period - 1)) + gain) / period;
                avgLoss = ((avgLoss * (period - 1)) + loss) / period;

                rs = avgLoss == 0 ? 100 : avgGain / avgLoss;
                rsi = 100 - (100 / (1 + rs));
            }

            var result = new RSIResult
            {
                Symbol = symbol,
                Interval = interval,
                Period = period,
                Value = rsi,
                Timestamp = ordered.Last().CloseTime
            };
            await _rsiRepository.AddAsync(result);
            await _rsiRepository.SaveChangesAsync();

            return result;
        }
    }
}
