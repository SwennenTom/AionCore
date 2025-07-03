using AionCoreBot.Domain.Models;
using AionCoreBot.Infrastructure.Interfaces;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AionCoreBot.Application.Services
{
    internal class CandleAggregator
    {
        private readonly ICandleRepository _candleRepository;
        private readonly IConfiguration _configuration;

        private readonly List<string> _intervals;
        private readonly List<string> _symbols;

        public CandleAggregator(ICandleRepository candleRepository, IConfiguration configuration)
        {
            _candleRepository = candleRepository;
            _configuration = configuration;

            _intervals = _configuration
                .GetSection("TimeIntervals:AvailableIntervals")
                .Get<List<string>>() ?? new();

            _symbols = _configuration
                .GetSection("BinanceExchange:EURPairs")
                .Get<List<string>>() ?? new();
        }

        public async Task AggregateAsync()
        {
            var now = DateTime.UtcNow;

            foreach (var interval in _intervals)
            {
                if (!IsCorrectTimeToAggregate(interval, now))
                    continue;

                var (startTime, endTime) = GetAggregationWindow(interval, now);

                foreach (var symbol in _symbols)
                {
                    var candles = await _candleRepository.GetCandlesAsync(symbol, "1m", startTime, endTime);

                    if (candles == null || !candles.Any())
                        continue;

                    var ordered = candles.OrderBy(c => c.OpenTime).ToList();

                    var aggregatedCandle = new Candle
                    {
                        Symbol = symbol,
                        Interval = interval,
                        OpenTime = startTime,
                        CloseTime = endTime,
                        OpenPrice = ordered.First().OpenPrice,
                        ClosePrice = ordered.Last().ClosePrice,
                        HighPrice = ordered.Max(c => c.HighPrice),
                        LowPrice = ordered.Min(c => c.LowPrice),
                        Volume = ordered.Sum(c => c.Volume)
                    };

                    await _candleRepository.AddAsync(aggregatedCandle);
                    await _candleRepository.SaveChangesAsync();
                }
            }
        }

        private bool IsCorrectTimeToAggregate(string interval, DateTime now)
        {
            return interval switch
            {
                "1m" => true,
                "5m" => now.Minute % 5 == 0,
                "15m" => now.Minute % 15 == 0,
                "1h" => now.Minute == 0,
                "4h" => now.Minute == 0 && now.Hour % 4 == 0,
                "1d" => now.Hour == 0 && now.Minute == 0,
                _ => false
            };
        }

        private (DateTime Start, DateTime End) GetAggregationWindow(string interval, DateTime now)
        {
            now = now.AddSeconds(-now.Second).AddMilliseconds(-now.Millisecond); // truncate to minute

            if (interval.EndsWith("m"))
            {
                int minutes = int.Parse(interval.Replace("m", ""));
                var end = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute / minutes * minutes, 0).AddMinutes(minutes);
                return (end.AddMinutes(-minutes), end);
            }

            if (interval.EndsWith("h"))
            {
                int hours = int.Parse(interval.Replace("h", ""));
                var endHour = now.Hour / hours * hours;
                var end = new DateTime(now.Year, now.Month, now.Day, endHour, 0, 0).AddHours(hours);
                return (end.AddHours(-hours), end);
            }

            if (interval == "1d")
            {
                var end = new DateTime(now.Year, now.Month, now.Day).AddDays(1);
                return (end.AddDays(-1), end);
            }

            throw new ArgumentException($"Unsupported interval: {interval}");
        }
    }
}
