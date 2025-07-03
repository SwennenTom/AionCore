using AionCoreBot.Domain.Models;
using AionCoreBot.Infrastructure.Interfaces;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Threading.Tasks;

namespace AionCoreBot.Infrastructure.Comms.Clients
{
    internal class BinanceCandleDownloadService : ICandleDownloadService
    {
        private readonly IBinanceRestClient _restClient;

        public BinanceCandleDownloadService(IBinanceRestClient restClient)
        {
            _restClient = restClient;
        }

        public async Task<List<Candle>> GetHistoricalCandlesAsync(string symbol, string interval, DateTime from, DateTime to)
        {
            const int limit = 1000;
            var candles = new List<Candle>();
            long fromMs = ToUnixMilliseconds(from);

            while (true)
            {
                var url = $"/api/v3/klines?symbol={symbol.ToUpperInvariant()}&interval={interval}&limit={limit}&startTime={fromMs}";
                var response = await _restClient.GetRawAsync(url);

                var parsed = JsonSerializer.Deserialize<List<JsonElement[]>>(response);
                if (parsed == null || parsed.Count == 0)
                    break;

                foreach (var item in parsed)
                {
                    var openTimeMs = item[0].GetInt64();
                    var closeTimeMs = item[6].GetInt64();
                    var openTime = FromUnixMilliseconds(openTimeMs);
                    var closeTime = FromUnixMilliseconds(closeTimeMs);

                    if (closeTime > to)
                        return candles;

                    var candle = new Candle
                    {
                        Symbol = symbol,
                        Interval = interval,
                        OpenTime = openTime,
                        CloseTime = closeTime,
                        OpenPrice = ParseDecimal(item[1]),
                        HighPrice = ParseDecimal(item[2]),
                        LowPrice = ParseDecimal(item[3]),
                        ClosePrice = ParseDecimal(item[4]),
                        Volume = ParseDecimal(item[5]),
                        QuoteVolume = ParseDecimal(item[7])
                    };

                    candles.Add(candle);
                }

                if (parsed.Count < limit)
                    break;

                fromMs = parsed[^1][0].GetInt64() + 1; // start net na de laatste openTime van vorige batch
            }

            candles = candles
                            .GroupBy(c => new { c.Symbol, c.Interval, c.OpenTime })
                            .Select(g => g.First())
                            .ToList();


            return candles;
        }

        public async Task<List<Candle>> DownloadCandlesAsync(string symbol, string interval, int days)
        {
            var to = DateTime.UtcNow;
            var from = to.AddDays(-days);

            return await GetHistoricalCandlesAsync(symbol, interval, from, to);
        }


        private static decimal ParseDecimal(JsonElement element) =>
            decimal.Parse(element.GetString() ?? "0", CultureInfo.InvariantCulture);



        private static long ToUnixMilliseconds(DateTime dt) =>
            new DateTimeOffset(dt).ToUnixTimeMilliseconds();

        private static DateTime FromUnixMilliseconds(long ms) =>
            DateTimeOffset.FromUnixTimeMilliseconds(ms).UtcDateTime;

        private static decimal ParseDecimal(object value) =>
            Convert.ToDecimal(value.ToString(), CultureInfo.InvariantCulture);
    }
}
