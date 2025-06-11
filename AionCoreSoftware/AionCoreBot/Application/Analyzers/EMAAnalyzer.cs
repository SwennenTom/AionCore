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

        public Task<EMAResult> AnalyzeAsync(IEnumerable<Candle> candles)
        {
            if (candles == null || !candles.Any())
                throw new ArgumentException("Geen candles aangeleverd voor EMA-berekening.");

            var orderedCandles = candles.OrderBy(c => c.Timestamp).ToList();
            var period = orderedCandles.Count;

            if (period < 2)
                throw new ArgumentException("Minstens twee candles vereist voor EMA-berekening.");

            decimal multiplier = 2m / (period + 1);
            decimal? previousEMA = null;

            foreach (var candle in orderedCandles)
            {
                if (previousEMA == null)
                    previousEMA = candle.ClosePrice; // init: eerste EMA = eerste sluitingsprijs
                else
                    previousEMA = ((candle.ClosePrice - previousEMA.Value) * multiplier) + previousEMA.Value;
            }

            var latestCandle = orderedCandles.Last();

            var result = new EMAResult
            {
                Symbol = latestCandle.Symbol,
                Interval = latestCandle.Interval,
                Period = period,
                Timestamp = latestCandle.Timestamp,
                EMAValue = previousEMA ?? 0m
            };

            return Task.FromResult(result);
        }
    }
}
