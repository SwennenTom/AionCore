using AionCoreBot.Domain.Interfaces;
using AionCoreBot.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AionCoreBot.Application.Analyzers
{
    public class ATRAnalyzer : IAnalyzer<IEnumerable<Candle>, ATRResult>
    {
        public string Name => "ATR";

        public void ResetState()
        {
            // Optionele resetlogica
        }

        public Task<ATRResult> AnalyzeAsync(IEnumerable<Candle> candles, int period)
        {
            if (candles == null || candles.Count() < period + 1)
                throw new ArgumentException($"Minstens {period + 1} candles vereist voor ATR-berekening.");

            var orderedCandles = candles.OrderBy(c => c.CloseTime).ToList();

            List<decimal> trueRanges = new();

            // Bereken True Range voor elke candle binnen de periode
            for (int i = 1; i <= period; i++)
            {
                var current = orderedCandles[i];
                var previous = orderedCandles[i - 1];

                decimal highLow = current.HighPrice - current.LowPrice;
                decimal highClose = Math.Abs(current.HighPrice - previous.ClosePrice);
                decimal lowClose = Math.Abs(current.LowPrice - previous.ClosePrice);

                decimal trueRange = Math.Max(highLow, Math.Max(highClose, lowClose));
                trueRanges.Add(trueRange);
            }

            decimal atr = trueRanges.Average();

            var latestCandle = orderedCandles[period];

            return Task.FromResult(new ATRResult
            {
                Symbol = latestCandle.Symbol,
                Interval = latestCandle.Interval,
                Period = period,
                Timestamp = latestCandle.CloseTime,
                ATRValue = atr
            });
        }

    }
}
