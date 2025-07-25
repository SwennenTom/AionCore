﻿using AionCoreBot.Application.Analysis.Interfaces;
using AionCoreBot.Domain.Models;
using AionCoreBot.Infrastructure.Interfaces;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AionCoreBot.Application.Analysis.Indicators
{
    internal class EMAService : IIndicatorService<EMAResult>
    {
        private readonly ICandleRepository _candleRepository;
        private readonly IIndicatorRepository<EMAResult> _emaRepository;
        private readonly IConfiguration _configuration;

        public EMAService(ICandleRepository candleRepository, IIndicatorRepository<EMAResult> emaRepository, IConfiguration configuration)
        {
            _candleRepository = candleRepository;
            _emaRepository = emaRepository;
            _configuration = configuration;
        }

        public async Task<EMAResult> CalculateAsync(string symbol, string interval, int period, DateTime startTime, DateTime endTime)
        {
            var candles = await _candleRepository.GetCandlesAsync(symbol, interval, startTime, endTime);

            if (candles == null || !candles.Any())
                throw new InvalidOperationException("Geen candles beschikbaar voor EMA-berekening");

            var ordered = candles.OrderBy(c => c.OpenTime).ToList();

            double multiplier = 2.0 / (period + 1);
            double? previousEma = null;

            foreach (var candle in ordered)
            {
                if (previousEma == null)
                {
                    previousEma = (double)candle.ClosePrice;
                }
                else
                {
                    previousEma = ((double)candle.ClosePrice - previousEma) * multiplier + previousEma;
                }
            }

            if (previousEma == null)
                throw new InvalidOperationException("EMA kon niet worden berekend");

            var result = new EMAResult
            {
                Symbol = symbol,
                Interval = interval,
                Period = period,
                Value = (decimal)previousEma,
                Timestamp = ordered.Last().CloseTime
            };

            await _emaRepository.AddAsync(result);
            await _emaRepository.SaveChangesAsync();

            return result;
        }

        public async Task CalcAllAsync()
        {
            Console.WriteLine("[EMA] Starting EMA calculation for all symbols and intervals.");

            var symbols = _configuration.GetSection("BinanceExchange:EURPairs").Get<List<string>>() ?? new();
            var intervals = _configuration.GetSection("TimeIntervals:AvailableIntervals").Get<List<string>>() ?? new();
            var emaPeriods = _configuration.GetSection("TimeIntervals:IndicatorPeriods:EMA").Get<List<int>>() ?? new List<int> { 14 };

            foreach (var symbol in symbols)
            {
                foreach (var interval in intervals)
                {
                    var candles = await _candleRepository.GetBySymbolAndIntervalAsync(symbol, interval);
                    var candlesList = candles.OrderBy(c => c.OpenTime).ToList();

                    if (!candlesList.Any())
                    {
                        Console.WriteLine($"[EMA] No candles found for {symbol} - {interval}");
                        continue;
                    }

                    foreach (var emaPeriod in emaPeriods)
                    {
                        //Console.WriteLine($"[EMA] Processing {symbol} - {interval} for period {emaPeriod}");

                        var lastEMA = await _emaRepository.GetLatestBySymbolIntervalPeriodAsync(symbol, interval, emaPeriod);

                        int startIndex = 0;
                        if (lastEMA != null)
                        {
                            startIndex = candlesList.FindIndex(c => c.OpenTime > lastEMA.Timestamp);
                            if (startIndex < 0) startIndex = 0;
                        }

                        double? previousEMA = lastEMA?.Value != null ? Convert.ToDouble(lastEMA.Value) : null;
                        double multiplier = 2.0 / (emaPeriod + 1);

                        for (int i = startIndex; i < candlesList.Count; i++)
                        {
                            var candle = candlesList[i];

                            if (previousEMA == null)
                                previousEMA = (double)candle.ClosePrice;
                            else
                                previousEMA = ((double)candle.ClosePrice - previousEMA) * multiplier + previousEMA;

                            var emaResult = new EMAResult
                            {
                                Symbol = symbol,
                                Interval = interval,
                                Period = emaPeriod,
                                Timestamp = candle.OpenTime,
                                Value = (decimal)previousEMA.Value
                            };

                            await _emaRepository.AddAsync(emaResult);

                            if (i % 50 == 0)
                                await _emaRepository.SaveChangesAsync();
                        }
                        await _emaRepository.SaveChangesAsync();

                        //Console.WriteLine($"[EMA] Finished processing {symbol} - {interval} for period {emaPeriod}");
                    }
                }
            }

            Console.WriteLine("[EMA] EMA calculation finished for all symbols and intervals.");
        }

        #region Pass Through Methods
        public async Task SaveResultAsync(EMAResult result)
        {
            await _emaRepository.AddAsync(result);
            await _emaRepository.SaveChangesAsync();
        }

        public async Task<EMAResult?> GetAsync(string symbol, string interval, DateTime timestamp, int period)
        {
            return await _emaRepository.GetBySymbolIntervalTimestampPeriodAsync(symbol, interval, timestamp, period);
        }

        public async Task<EMAResult?> GetLatestAsync(string symbol, string interval, int period)
        {
            return await _emaRepository.GetLatestBySymbolIntervalPeriodAsync(symbol, interval, period);
        }

        public async Task<EMAResult?> GetLatestBeforeAsync(string symbol, string interval, int period, DateTime time)
        {
            return await _emaRepository.GetLatestBeforeAsync(symbol, interval, period, time);
        }

        public async Task<IEnumerable<EMAResult>> GetHistoryAsync(string symbol, string interval, int period, DateTime from, DateTime to)
        {
            return await _emaRepository.GetByPeriodAndDateRangeAsync(symbol, interval, period, from, to);
        }

        public async Task ClearAllAsync()
        {
            await _emaRepository.ClearAllAsync();
        }
        #endregion
    }
}