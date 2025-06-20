using AionCoreBot.Domain.Interfaces;
using AionCoreBot.Domain.Models;
using AionCoreBot.Infrastructure.Interfaces;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AionCoreBot.Worker.Indicators
{
    internal class RSIService : IRSIService
    {
        private readonly ICandleRepository _candleRepository;
        private readonly IIndicatorRepository<RSIResult> _rsiRepository;
        private readonly IConfiguration _configuration;

        public RSIService(ICandleRepository candleRepository, IIndicatorRepository<RSIResult> rsiRepository, IConfiguration configuration)
        {
            _candleRepository = candleRepository;
            _rsiRepository = rsiRepository;
            _configuration = configuration;
        }

        public async Task<RSIResult> CalculateAsync(string symbol, string interval, int period, DateTime startTime, DateTime endTime)
        {
            var candles = await _candleRepository.GetCandlesAsync(symbol, interval, startTime, endTime);

            if (candles == null || candles.Count() < period + 1)
                throw new InvalidOperationException("Niet genoeg candles voor RSI-berekening");

            var ordered = candles.OrderBy(c => c.OpenTime).ToList();

            var gains = new List<decimal>();
            var losses = new List<decimal>();

            for (int i = 1; i < period + 1; i++)
            {
                var change = ordered[i].ClosePrice - ordered[i - 1].ClosePrice;
                if (change > 0)
                    gains.Add(change);
                else
                    losses.Add(Math.Abs(change));
            }

            decimal avgGain = gains.DefaultIfEmpty(0).Average();
            decimal avgLoss = losses.DefaultIfEmpty(0).Average();

            decimal rs = avgLoss == 0 ? 100 : avgGain / avgLoss;
            decimal rsi = 100 - (100 / (1 + rs));

            // Wilder's smoothing toepassen voor resterende candles
            for (int i = period + 1; i < ordered.Count; i++)
            {
                var change = ordered[i].ClosePrice - ordered[i - 1].ClosePrice;
                decimal gain = change > 0 ? change : 0;
                decimal loss = change < 0 ? Math.Abs(change) : 0;

                avgGain = ((avgGain * (period - 1)) + gain) / period;
                avgLoss = ((avgLoss * (period - 1)) + loss) / period;

                rs = avgLoss == 0 ? 100 : avgGain / avgLoss;
                rsi = 100 - (100 / (1 + rs));
            }

            rsi = Math.Round(rsi, 2);

            var result = new RSIResult
            {
                Symbol = symbol,
                Interval = interval,
                Period = period,
                Value = rsi,
                Timestamp = ordered.Last().CloseTime
            };
            await _rsiRepository.AddAsync(result);
            await _rsiRepository.SaveChangesAsync();

            return result;
        }
        public async Task CalcAllAsync()
        {
            Console.WriteLine("[RSI] Starting RSI calculation for all symbols and intervals.");

            var symbols = _configuration.GetSection("BinanceExchange:EURPairs").Get<List<string>>() ?? new();
            var intervals = _configuration.GetSection("TimeIntervals:AvailableIntervals").Get<List<string>>() ?? new();
            var rsiPeriods = _configuration.GetSection("TimeIntervals:IndicatorPeriods:RSI").Get<List<int>>() ?? new List<int> { 14 };

            foreach (var symbol in symbols)
            {
                foreach (var interval in intervals)
                {
                    Console.WriteLine($"[RSI] Processing {symbol} - {interval}");

                    var candles = await _candleRepository.GetBySymbolAndIntervalAsync(symbol, interval);
                    var candlesList = candles.OrderBy(c => c.OpenTime).ToList();

                    if (candlesList.Count < 15) // 14 + 1
                    {
                        Console.WriteLine($"[RSI] Not enough candles for {symbol} - {interval}");
                        continue;
                    }

                    foreach (var rsiPeriod in rsiPeriods)
                    {


                        var lastRSI = await _rsiRepository.GetLatestBySymbolIntervalPeriodAsync(symbol, interval, rsiPeriod);

                        int startIndex = 0;
                        if (lastRSI != null)
                        {
                            startIndex = candlesList.FindIndex(c => c.OpenTime > lastRSI.Timestamp);
                            if (startIndex < 0) startIndex = 0;
                        }

                        decimal avgGain = 0, avgLoss = 0;
                        bool initialized = false;

                        for (int i = startIndex; i < candlesList.Count; i++)
                        {
                            if (i < rsiPeriod) continue;

                            if (!initialized)
                            {
                                var gains = new List<decimal>();
                                var losses = new List<decimal>();
                                for (int j = 1; j <= rsiPeriod; j++)
                                {
                                    var change = candlesList[j].ClosePrice - candlesList[j - 1].ClosePrice;
                                    if (change > 0) gains.Add(change);
                                    else losses.Add(Math.Abs(change));
                                }
                                avgGain = gains.DefaultIfEmpty(0).Average();
                                avgLoss = losses.DefaultIfEmpty(0).Average();
                                initialized = true;
                            }
                            else
                            {
                                var change = candlesList[i].ClosePrice - candlesList[i - 1].ClosePrice;
                                var gain = change > 0 ? change : 0;
                                var loss = change < 0 ? Math.Abs(change) : 0;

                                avgGain = ((avgGain * (rsiPeriod - 1)) + gain) / rsiPeriod;
                                avgLoss = ((avgLoss * (rsiPeriod - 1)) + loss) / rsiPeriod;
                            }

                            decimal rs = avgLoss == 0 ? 100 : avgGain / avgLoss;
                            decimal rsi = 100 - (100 / (1 + rs));

                            var rsiResult = new RSIResult
                            {
                                Symbol = symbol,
                                Interval = interval,
                                Period = rsiPeriod,
                                Timestamp = candlesList[i].OpenTime,
                                Value = rsi
                            };

                            await _rsiRepository.AddAsync(rsiResult);

                            if (i % 50 == 0)
                                await _rsiRepository.SaveChangesAsync();
                        }

                        await _rsiRepository.SaveChangesAsync();
                        Console.WriteLine($"[RSI] Finished processing {symbol} - {interval}");
                    }
                }
            }

            Console.WriteLine("[RSI] RSI calculation finished for all symbols and intervals.");
        }

    }
}
