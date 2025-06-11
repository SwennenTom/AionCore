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

        public Task<RSIResult> AnalyzeAsync(IEnumerable<Candle> candles, int period)
        {
            if (candles == null || candles.Count() < period + 1)
                throw new ArgumentException($"Minstens {period + 1} candles vereist voor RSI-berekening.");

            var orderedCandles = candles.OrderBy(c => c.CloseTime).ToList();

            decimal gainSum = 0;
            decimal lossSum = 0;

            // Bereken winst/verlies over de eerste 'period' candles
            for (int i = 1; i <= period; i++)
            {
                decimal delta = orderedCandles[i].ClosePrice - orderedCandles[i - 1].ClosePrice;
                if (delta >= 0)
                    gainSum += delta;
                else
                    lossSum -= delta;
            }

            decimal avgGain = gainSum / period;
            decimal avgLoss = lossSum / period;

            decimal rs = avgLoss == 0 ? 100 : avgGain / avgLoss;
            decimal rsi = 100 - (100 / (1 + rs));

            var latestCandle = orderedCandles[period];

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
