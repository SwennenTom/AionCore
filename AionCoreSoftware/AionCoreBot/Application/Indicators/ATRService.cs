using AionCoreBot.Application.Interfaces.IAnalyzers;
using AionCoreBot.Domain.Models;
using AionCoreBot.Infrastructure.Interfaces;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AionCoreBot.Application.Indicators
{
    internal class ATRService : IIndicatorService<ATRResult>
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
            var ordered = candles.OrderBy(c => c.OpenTime).ToList();

            var results = ComputeATRSeries(symbol, interval, ordered, period);

            var latest = results.LastOrDefault();
            if (latest != null)
            {
                await _atrRepository.AddAsync(latest);
                await _atrRepository.SaveChangesAsync();
            }

            return latest!;
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

                    if (ordered.Count < 20)
                    {
                        Console.WriteLine($"[ATR] Skipping {symbol} - {interval}: insufficient candles.");
                        continue;
                    }

                    foreach (var period in periods)
                    {
                        Console.WriteLine($"[ATR] Calculating history for {symbol} - {interval} - {period}");

                        var results = ComputeATRSeries(symbol, interval, ordered, period);
                        if (results.Count == 0)
                        {
                            Console.WriteLine($"[ATR] Not enough data to initialize ATR for {symbol} - {interval} - {period}");
                            continue;
                        }

                        foreach (var atrResult in results)
                        {
                            await _atrRepository.AddAsync(atrResult);
                        }

                        await _atrRepository.SaveChangesAsync();
                        Console.WriteLine($"[ATR] Finished {symbol} - {interval} - {period}");
                    }
                }
            }

            Console.WriteLine("[ATR] ATR calculation finished for all symbols and intervals.");
        }

        private List<ATRResult> ComputeATRSeries(string symbol, string interval, List<Candle> ordered, int period)
        {
            var results = new List<ATRResult>();
            if (ordered.Count < period + 1) return results;

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

            if (trueRanges.Count < period) return results;

            decimal atr = trueRanges.Take(period).Average();

            for (int i = period; i < trueRanges.Count; i++)
            {
                atr = (atr * (period - 1) + trueRanges[i]) / period;

                results.Add(new ATRResult
                {
                    Symbol = symbol,
                    Interval = interval,
                    Period = period,
                    Value = atr,
                    ValuePct = atr / ordered[i + 1].ClosePrice,
                    Timestamp = ordered[i + 1].CloseTime,
                    ClosePrice = ordered[i + 1].ClosePrice
                });
            }

            return results;
        }

        #region PassThrough Methods
        public async Task SaveResultAsync(ATRResult result)
        {
            await _atrRepository.AddAsync(result);
            await _atrRepository.SaveChangesAsync();
        }

        public async Task<ATRResult?> GetAsync(string symbol, string interval, DateTime timestamp, int period)
        {
            return await _atrRepository.GetBySymbolIntervalTimestampPeriodAsync(symbol, interval, timestamp, period);
        }

        public async Task<ATRResult?> GetLatestAsync(string symbol, string interval, int period)
        {
            return await _atrRepository.GetLatestBySymbolIntervalPeriodAsync(symbol, interval, period);
        }

        public async Task<ATRResult?> GetLatestBeforeAsync(string symbol, string interval, int period, DateTime time)
        {
            return await _atrRepository.GetLatestBeforeAsync(symbol, interval, period, time);
        }

        public async Task<IEnumerable<ATRResult>> GetHistoryAsync(string symbol, string interval, int period, DateTime from, DateTime to)
        {
            return await _atrRepository.GetByPeriodAndDateRangeAsync(symbol, interval, period, from, to);
        }

        public async Task ClearAllAsync()
        {
            await _atrRepository.ClearAllAsync();
        }
        #endregion
    }
}
