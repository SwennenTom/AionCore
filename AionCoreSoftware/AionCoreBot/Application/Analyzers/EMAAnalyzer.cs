using AionCoreBot.Domain.Interfaces;
using AionCoreBot.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AionCoreBot.Application.Analyzers
{
    public class EMAAnalyzer:IAnalyzer<IEnumerable<Candle>, EMAResult>
    {
        public string Name => "EMA";

        public void ResetState()
        {
            // Reset logica indien nodig
        }

        public Task<EMAResult> AnalyzeAsync(IEnumerable<Candle> candles, int period)
        {
            if (candles == null || !candles.Any())
                throw new ArgumentException("Geen candles aangeleverd voor EMA-berekening.");

            var orderedCandles = candles.OrderBy(c => c.Timestamp).ToList();

            if (orderedCandles.Count < period)
                throw new ArgumentException($"Minstens {period} candles vereist voor EMA-berekening.");

            decimal multiplier = 2m / (period + 1);
            decimal? previousEMA = null;

            for (int i = 0; i < orderedCandles.Count; i++)
            {
                var closePrice = orderedCandles[i].ClosePrice;
                if (i == 0)
                    previousEMA = closePrice; // init
                else
                    previousEMA = ((closePrice - previousEMA.Value) * multiplier) + previousEMA.Value;
            }

            var latestCandle = orderedCandles.Last();

            var result = new EMAResult
            {
                Symbol = latestCandle.Symbol,
                Interval = latestCandle.Interval,
                Period = period,
                Timestamp = latestCandle.CloseTime,
                EMAValue = previousEMA ?? 0m
            };

            return Task.FromResult(result);
        }
    }
}
