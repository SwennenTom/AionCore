using AionCoreBot.Domain.Models;
using AionCoreBot.Infrastructure.Interfaces;
using AionCoreBot.Infrastructure.Websocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AionCoreBot.Infrastructure.Websocket
{
    public class BinanceWebSocketService
    {
        private readonly BinanceWebSocketClient _client;
        private readonly ICandleRepository _candleRepository;

        public BinanceWebSocketService(ICandleRepository candleRepository)
        {
            _client = new BinanceWebSocketClient();
            _candleRepository = candleRepository;
            _client.OnMessageReceived += HandleMessage;
            _client.OnDisconnected += OnDisconnected;
        }

        public async Task StartAsync(IEnumerable<string> symbols, CancellationToken cancellationToken)
        {
            // Binance streams format: e.g. "btceur@kline_1m"
            var streams = symbols
                .Select(s => s.ToLower() + "@kline_1m")
                .Aggregate((a, b) => a + "/" + b);

            await _client.ConnectAsync(streams, cancellationToken);
        }

        private async void HandleMessage(string message)
        {
            try
            {
                // Parse JSON message from Binance kline stream
                var binanceKline = JsonSerializer.Deserialize<BinanceKlineMessage>(message);

                if (binanceKline?.Data?.Kline?.IsFinal == true)
                {
                    var c = binanceKline.Data.Kline;
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
                    );

                    await _candleRepository.AddAsync(candle);
                    await _candleRepository.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing or saving candle: {ex.Message}");
            }
        }


        private void OnDisconnected()
        {
            Console.WriteLine("Binance websocket disconnected. Attempting reconnect...");
            // Optional: implement reconnect logic
        }
    }

}
