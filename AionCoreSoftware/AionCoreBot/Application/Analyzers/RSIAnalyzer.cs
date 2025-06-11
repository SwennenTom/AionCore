using AionCoreBot.Domain.Interfaces;
using AionCoreBot.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AionCoreBot.Application.Analyzers
{
    public class RSIAnalyzer : IAnalyzer<IEnumerable<Candle>, RSIResult>
    {
        public string Name => "RSI";

        public void ResetState()
        {
            // Optionele resetlogica
        }

        public Task<RSIResult> AnalyzeAsync(IEnumerable<Candle> candles)
        {
            if (candles == null || candles.Count() < 2)
                throw new ArgumentException("Minstens twee candles vereist voor RSI-berekening.");

            var orderedCandles = candles.OrderBy(c => c.CloseTime).ToList();
            var period = orderedCandles.Count;

            decimal gainSum = 0;
            decimal lossSum = 0;

            for (int i = 1; i < period; i++)
            {
                decimal delta = orderedCandles[i].ClosePrice - orderedCandles[i - 1].ClosePrice;
                if (delta >= 0)
                    gainSum += delta;
                else
                    lossSum -= delta;
            }

            decimal avgGain = gainSum / (period - 1);
            decimal avgLoss = lossSum / (period - 1);
            decimal rs = avgLoss == 0 ? 100 : avgGain / avgLoss;
            decimal rsi = 100 - (100 / (1 + rs));

            var latestCandle = orderedCandles.Last();

            return Task.FromResult(new RSIResult
            {
                Symbol = latestCandle.Symbol,
                Interval = latestCandle.Interval,
                Period = period,
                Timestamp = latestCandle.CloseTime,
                RSIValue = rsi
            });
        }
    }
}
