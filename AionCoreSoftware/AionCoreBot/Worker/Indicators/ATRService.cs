using AionCoreBot.Domain.Interfaces;
using AionCoreBot.Domain.Models;
using AionCoreBot.Infrastructure.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AionCoreBot.Worker.Indicators
{
    internal class ATRService : IATRService
    {
        private readonly ICandleRepository _candleRepository;
        private readonly IIndicatorRepository<ATRResult> _atrRepository;

        public ATRService(ICandleRepository candleRepository, IIndicatorRepository<ATRResult> atrRepository)
        {
            _candleRepository = candleRepository;
            _atrRepository = atrRepository;
        }

        public async Task<ATRResult> CalculateAsync(string symbol, string interval, int period, DateTime startTime, DateTime endTime)
        {
            // Candles ophalen
            var candles = await _candleRepository.GetCandlesAsync(symbol, interval, startTime, endTime);

            if (candles == null || candles.Count() < period + 1)
                throw new InvalidOperationException("Niet genoeg candles beschikbaar voor ATR-berekening.");

            var ordered = candles.OrderBy(c => c.OpenTime).ToList();

            // True Ranges berekenen
            var trueRanges = new List<decimal>();

            for (int i = 1; i < ordered.Count; i++)
            {
                var current = ordered[i];
                var previous = ordered[i - 1];

                decimal highLow = current.HighPrice - current.LowPrice;
                decimal highClose = Math.Abs(current.HighPrice - previous.ClosePrice);
                decimal lowClose = Math.Abs(current.LowPrice - previous.ClosePrice);

                decimal tr = Math.Max(highLow, Math.Max(highClose, lowClose));
                trueRanges.Add(tr);
            }

            // Eerste ATR is SMA van de eerste `period` true ranges
            decimal firstAtr = trueRanges.Take(period).Average();
            decimal atr = firstAtr;

            // Daarna Wilder’s smoothing
            for (int i = period; i < trueRanges.Count; i++)
            {
                atr = ((atr * (period - 1)) + trueRanges[i]) / period;
            }

            var result = new ATRResult
            {
                Symbol = symbol,
                Interval = interval,
                Period = period,
                Value = atr,
                Timestamp = ordered.Last().CloseTime
            };

            // Opslaan
            await _atrRepository.AddAsync(result);
            await _atrRepository.SaveChangesAsync();

            return result;
        }
    }
}
