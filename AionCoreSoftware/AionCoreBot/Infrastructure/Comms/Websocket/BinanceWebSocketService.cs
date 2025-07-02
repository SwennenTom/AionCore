using AionCoreBot.Domain.Models;
using AionCoreBot.Infrastructure.Data;
using AionCoreBot.Infrastructure.Interfaces;
using AionCoreBot.Worker.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AionCoreBot.Infrastructure.Comms.Websocket
{
    public class BinanceWebSocketService
    {
        private BinanceWebSocketClient _client;
        private readonly List<string> _symbols;
        private CancellationToken _externalCancellationToken;
        private BinanceTimeSynchronizer _timeSynchronizer;
        private readonly IServiceScopeFactory _scopeFactory;

        private int _reconnectAttempts = 0;
        private const int MaxReconnectAttempts = 1440;
        private const int BaseDelaySeconds = 5;
        private bool _isReconnecting = false;

        public BinanceWebSocketService(IServiceScopeFactory scopeFactory)
        {
            _symbols = new List<string>();
            _timeSynchronizer = new BinanceTimeSynchronizer();
            _scopeFactory = scopeFactory;
        }

        public async Task StartAsync(IEnumerable<string> symbols, CancellationToken cancellationToken)
        {
            _symbols.Clear();
            _symbols.AddRange(symbols.Select(s => s.ToLower()));
            _externalCancellationToken = cancellationToken;

            _timeSynchronizer.StartPeriodicSync(TimeSpan.FromMinutes(10), cancellationToken);
            _ = Task.Run(() => StartCountdownToNextMinute(cancellationToken));

            await ConnectAndSubscribeAsync(cancellationToken);
        }

        private async Task StartCountdownToNextMinute(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                var timeLeft = _timeSynchronizer.GetTimeUntilNextMinuteCandle();
                int secondsRemaining = (int)timeLeft.TotalSeconds;

                Console.Write($"\r[WS] Next 1m candle in: {secondsRemaining:00} sec   ");

                await Task.Delay(1000, token);
            }
        }

        private async Task ConnectAndSubscribeAsync(CancellationToken cancellationToken)
        {
            _client = new BinanceWebSocketClient();
            _client.OnMessageReceived += async (msg) => await HandleMessage(msg);
            _client.OnDisconnected += OnDisconnected;

            var streams = string.Join('/', _symbols.Select(s => $"{s}@kline_1m"));


            try
            {
                await _client.ConnectAsync(streams, cancellationToken);
                _reconnectAttempts = 0;
                Console.WriteLine("[WS] Connected to Binance stream.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WS] Initial connection failed: {ex.Message}");
                await AttemptReconnectAsync();
            }
        }

        private async void OnDisconnected()
        {
            Console.WriteLine("[WS] Disconnected from Binance. Attempting reconnect...");
            await AttemptReconnectAsync();
        }

        private async Task AttemptReconnectAsync()
        {
            if (_isReconnecting) return;
            _isReconnecting = true;

            while (_reconnectAttempts < MaxReconnectAttempts && !_externalCancellationToken.IsCancellationRequested)
            {
                try
                {
                    _reconnectAttempts++;
                    int delay = Math.Min(60, _reconnectAttempts * 5);
                    Console.WriteLine($"[WS] Waiting {delay} seconds before reconnect attempt #{_reconnectAttempts}...");
                    await Task.Delay(TimeSpan.FromSeconds(delay), _externalCancellationToken);

                    await ConnectAndSubscribeAsync(_externalCancellationToken);
                    Console.WriteLine("[WS] Reconnected successfully.");
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[WS] Reconnect attempt #{_reconnectAttempts} failed: {ex.Message}");
                }
            }

            if (_reconnectAttempts >= MaxReconnectAttempts)
            {
                Console.WriteLine("[WS] Max reconnect attempts reached. Giving up.");
            }

            _isReconnecting = false;
        }

        private async Task HandleMessage(string message)
        {
            try
            {
                var binanceKline = JsonSerializer.Deserialize<BinanceKlineMessage>(message);
                var c = binanceKline?.Data?.Kline;

                Console.WriteLine($"[WS] Message {binanceKline?.Stream} IsFinal={c?.IsFinal}");

                if (c?.IsFinal == true)
                {
                    using var scope = _scopeFactory.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                    var symbol = binanceKline.Data.Symbol;

                    var candle = new Candle(
                        symbol,
                        DateTimeOffset.FromUnixTimeMilliseconds(c.StartTime).UtcDateTime,
                        DateTimeOffset.FromUnixTimeMilliseconds(c.CloseTime).UtcDateTime,
                        c.OpenPrice,
                        c.HighPrice,
                        c.LowPrice,
                        c.ClosePrice,
                        c.Volume
                    )
                    {
                        Interval = c.Interval
                    };

                    await dbContext.AddAsync(candle);
                    await dbContext.SaveChangesAsync();

                    Console.WriteLine($"[WS] Final candle stored: {symbol} @ {DateTimeOffset.FromUnixTimeMilliseconds(c.CloseTime).UtcDateTime:yyyy-MM-dd HH:mm:ss}");

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WS] Error handling message: {ex.Message}\nPayload: {message}");
            }
        }

    }
}
