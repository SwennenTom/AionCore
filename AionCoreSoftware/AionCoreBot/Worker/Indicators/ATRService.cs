using AionCoreBot.Application.Interfaces;
using AionCoreBot.Domain.Models;
using AionCoreBot.Infrastructure.Interfaces;
using AionCoreBot.Worker.Interfaces;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AionCoreBot.Worker.Indicators
{
    internal class ATRService : IBaseIndicatorService<ATRResult>
    {
        private readonly ICandleRepository _candleRepository;
        private readonly IIndicatorRepository<ATRResult> _atrRepository;
        private readonly IConfiguration _configuration;

        public ATRService(ICandleRepository candleRepository, IIndicatorRepository<ATRResult> atrRepository, IConfiguration configuration)
        {
            _candleRepository = candleRepository;
            _atrRepository = atrRepository;
            _configuration = configuration;
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

        public async Task CalcAllAsync()
        {
            Console.WriteLine("[ATR] Starting ATR calculation for all symbols and intervals.");

            var symbols = _configuration.GetSection("BinanceExchange:EURPairs").Get<List<string>>() ?? new();
            var intervals = _configuration.GetSection("TimeIntervals:AvailableIntervals").Get<List<string>>() ?? new();
            var periods = _configuration.GetSection("TimeIntervals:IndicatorPeriods:ATR").Get<List<int>>() ?? new() { 14 };

            foreach (var symbol in symbols)
            {
                foreach (var interval in intervals)
                {
                    foreach (var period in periods)
                    {
                        DateTime from = DateTime.UtcNow.AddDays(-14);
                        DateTime to = DateTime.UtcNow;

                        try
                        {
                            await CalculateAsync(symbol, interval, period, from, to);
                            Console.WriteLine($"[ATR] ✅ Calculated {symbol} - {interval} - {period}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[ATR] ❌ Failed {symbol} - {interval} - {period}: {ex.Message}");
                        }
                    }
                }
            }

            Console.WriteLine("[ATR] ATR calculation finished for all symbols and intervals.");
        }



        private decimal CalculateTrueRange(IList<Candle> candles, int i)
        {
            var current = candles[i];
            if (i == 0) return current.HighPrice - current.LowPrice;

            var previousClose = candles[i - 1].ClosePrice;

            var highLow = current.HighPrice - current.LowPrice;
            var highPrevClose = Math.Abs(current.HighPrice - previousClose);
            var lowPrevClose = Math.Abs(current.LowPrice - previousClose);

            return Math.Max(highLow, Math.Max(highPrevClose, lowPrevClose));
        }

    }
}
