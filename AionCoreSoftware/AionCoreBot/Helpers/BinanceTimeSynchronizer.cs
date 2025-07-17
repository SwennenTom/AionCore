using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AionCoreBot.Helpers
{
    public class BinanceTimeSynchronizer : IDisposable
    {
        private readonly HttpClient _httpClient;
        private long _serverTimeDeltaMs = 0; // server time - local time in milliseconds
        private Timer? _syncTimer;

        public BinanceTimeSynchronizer()
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://api.binance.com")
            };
        }

        // Start periodic sync, bv. elke 10 minuten
        public void StartPeriodicSync(TimeSpan interval, CancellationToken cancellationToken)
        {
            _syncTimer = new Timer(async _ =>
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _syncTimer?.Dispose();
                    return;
                }

                await SyncServerTimeAsync();
            }, null, TimeSpan.Zero, interval);
        }

        // Sync server time via REST
        public async Task SyncServerTimeAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/v3/time");
                response.EnsureSuccessStatusCode();

                using var stream = await response.Content.ReadAsStreamAsync();
                var jsonDoc = await JsonDocument.ParseAsync(stream);

                if (jsonDoc.RootElement.TryGetProperty("serverTime", out var serverTimeElement))
                {
                    var serverTime = serverTimeElement.GetInt64();
                    var localTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    _serverTimeDeltaMs = serverTime - localTime;

                    Console.WriteLine($"[BinanceTimeSynchronizer] Server time delta updated: {_serverTimeDeltaMs} ms");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BinanceTimeSynchronizer] Error syncing server time: {ex.Message}");
            }
        }

        // Geef de countdown tot de volgende 1-minuut candle
        public TimeSpan GetTimeUntilNextMinuteCandle()
        {
            var correctedUtcNow = DateTimeOffset.UtcNow.AddMilliseconds(_serverTimeDeltaMs);

            var nextMinute = new DateTimeOffset(
                correctedUtcNow.Year,
                correctedUtcNow.Month,
                correctedUtcNow.Day,
                correctedUtcNow.Hour,
                correctedUtcNow.Minute,
                0,
                TimeSpan.Zero).AddMinutes(1);

            return nextMinute - correctedUtcNow;
        }

        public TimeSpan GetTimeUntilNextFullHourCandle()
        {
            var now = DateTime.UtcNow;
            var nextHour = now.AddHours(1).Date.AddHours(now.Hour + 1);
            return nextHour - now;
        }


        public void Dispose()
        {
            _syncTimer?.Dispose();
            _httpClient.Dispose();
        }
    }
}
