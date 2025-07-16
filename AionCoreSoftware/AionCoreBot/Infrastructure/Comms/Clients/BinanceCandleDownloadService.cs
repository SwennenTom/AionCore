using AionCoreBot.Domain.Models;
using AionCoreBot.Infrastructure.Comms.Interfaces;
using Binance.Net.Clients;
using Binance.Net.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AionCoreBot.Infrastructure.Comms.Clients
{
    internal class BinanceCandleDownloadService : ICandleDownloadService
    {
        private readonly BinanceRestClient _restClient;

        public BinanceCandleDownloadService(BinanceRestClient restClient)
        {
            _restClient = restClient;
        }

        public async Task<List<Candle>> GetHistoricalCandlesAsync(string symbol, string interval, DateTime from, DateTime to)
        {
            var candles = new List<Candle>();

            // ✅ Map interval string → Binance.Net enum
            if (!TryMapInterval(interval, out var klineInterval))
                throw new ArgumentException($"Unsupported interval: {interval}");

            DateTime currentFrom = from;

            while (currentFrom < to)
            {
                var result = await _restClient.SpotApi.ExchangeData.GetKlinesAsync(
                    symbol,
                    klineInterval,
                    startTime: currentFrom,
                    endTime: to,
                    limit: 1000
                );

                if (!result.Success || result.Data == null || !result.Data.Any())
                    break;

                foreach (var k in result.Data)
                {
                    if (k.CloseTime > to)
                        break;

                    candles.Add(new Candle
                    {
                        Symbol = symbol,
                        Interval = interval,
                        OpenTime = k.OpenTime,
                        CloseTime = k.CloseTime,
                        OpenPrice = k.OpenPrice,
                        HighPrice = k.HighPrice,
                        LowPrice = k.LowPrice,
                        ClosePrice = k.ClosePrice,
                        Volume = k.Volume,
                        QuoteVolume = k.QuoteVolume
                    });
                }

                // ✅ Pagination: vanaf laatste candle + 1 ms
                currentFrom = result.Data.Last().CloseTime.AddMilliseconds(1);

                // Binance levert max ~1000 → break als minder dan limit
                if (result.Data.Count() < 1000)
                    break;
            }

            // ✅ Deduplicatie
            return candles
                .GroupBy(c => new { c.Symbol, c.Interval, c.OpenTime })
                .Select(g => g.First())
                .OrderBy(c => c.OpenTime)
                .ToList();
        }

        public Task<List<Candle>> DownloadCandlesAsync(string symbol, string interval, int days)
        {
            var to = DateTime.UtcNow;
            var from = to.AddDays(-days);
            return GetHistoricalCandlesAsync(symbol, interval, from, to);
        }

        private static bool TryMapInterval(string interval, out KlineInterval mapped)
        {
            mapped = interval switch
            {
                "1m" => KlineInterval.OneMinute,
                "3m" => KlineInterval.ThreeMinutes,
                "5m" => KlineInterval.FiveMinutes,
                "15m" => KlineInterval.FifteenMinutes,
                "30m" => KlineInterval.ThirtyMinutes,
                "1h" => KlineInterval.OneHour,
                "2h" => KlineInterval.TwoHour,
                "4h" => KlineInterval.FourHour,
                "6h" => KlineInterval.SixHour,
                "8h" => KlineInterval.EightHour,
                "12h" => KlineInterval.TwelveHour,
                "1d" => KlineInterval.OneDay,
                "3d" => KlineInterval.ThreeDay,
                "1w" => KlineInterval.OneWeek,
                _ => KlineInterval.OneMinute
            };
            return interval switch
            {
                "1m" or "3m" or "5m" or "15m" or "30m" or
                "1h" or "2h" or "4h" or "6h" or "8h" or "12h" or
                "1d" or "3d" or "1w" => true,
                _ => false
            };
        }
    }
}
