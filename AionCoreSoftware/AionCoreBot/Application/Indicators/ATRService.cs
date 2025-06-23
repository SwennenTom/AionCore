using AionCoreBot.Application.Interfaces.IIndicators;
using AionCoreBot.Domain.Models;
using AionCoreBot.Infrastructure.Interfaces;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AionCoreBot.Application.Indicators
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
            var candles = await _candleRepository.GetCandlesAsync(symbol, interval, startTime, endTime);

            if (candles == null || candles.Count() < period + 1)
                throw new InvalidOperationException("Niet genoeg candles beschikbaar voor ATR-berekening.");

            var ordered = candles.OrderBy(c => c.OpenTime).ToList();

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

            decimal firstAtr = trueRanges.Take(period).Average();
            decimal atr = firstAtr;

            for (int i = period; i < trueRanges.Count; i++)
            {
                atr = (atr * (period - 1) + trueRanges[i]) / period;
            }

            var result = new ATRResult
            {
                Symbol = symbol,
                Interval = interval,
                Period = period,
                Value = atr,
                Timestamp = ordered.Last().CloseTime,
                ClosePrice = ordered.Last().ClosePrice
            };

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
                    var candles = await _candleRepository.GetBySymbolAndIntervalAsync(symbol, interval);
                    var ordered = candles.OrderBy(c => c.OpenTime).ToList();

                    if (ordered.Count < 20) // minimaal aantal candles
                    {
                        Console.WriteLine($"[ATR] Skipping {symbol} - {interval}: onvoldoende candles.");
                        continue;
                    }

                    foreach (var period in periods)
                    {
                        Console.WriteLine($"[ATR] Calculating history for {symbol} - {interval} - {period}");

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

                        if (trueRanges.Count < period)
                        {
                            Console.WriteLine($"[ATR] Not enough data to initialize ATR for {symbol} - {interval} - {period}");
                            continue;
                        }

                        decimal atr = trueRanges.Take(period).Average(); // init
                        int atrStartIndex = period;

                        for (int i = atrStartIndex; i < trueRanges.Count; i++)
                        {
                            atr = (atr * (period - 1) + trueRanges[i]) / period;

                            var atrResult = new ATRResult
                            {
                                Symbol = symbol,
                                Interval = interval,
                                Period = period,
                                Value = atr,
                                Timestamp = ordered[i + 1].CloseTime, // i+1 want TR begint op index 1
                                ClosePrice = ordered[i + 1].ClosePrice
                            };

                            await _atrRepository.AddAsync(atrResult);

                            if (i % 50 == 0)
                                await _atrRepository.SaveChangesAsync();
                        }

                        await _atrRepository.SaveChangesAsync();
                        Console.WriteLine($"[ATR] ✅ Finished {symbol} - {interval} - {period}");
                    }
                }
            }

            Console.WriteLine("[ATR] ✅ ATR calculation finished for all symbols and intervals.");
        }


    }
}
