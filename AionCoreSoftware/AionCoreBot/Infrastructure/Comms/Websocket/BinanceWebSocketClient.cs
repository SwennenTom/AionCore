using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace AionCoreBot.Infrastructure.Comms.Websocket
{
    public class BinanceWebSocketClient : IDisposable
    {
        private ClientWebSocket _ws;
        private readonly Uri _baseUri = new("wss://stream.binance.com:9443/stream?streams=");
        private CancellationTokenSource _cts;

        public event Action<string>? OnMessageReceived;
        public event Action? OnConnected;
        public event Action? OnDisconnected;

        public async Task ConnectAsync(string stream, CancellationToken cancellationToken = default)
        {
            _ws = new ClientWebSocket();
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            var uri = new Uri($"{_baseUri}{stream}");
            await _ws.ConnectAsync(uri, _cts.Token);
            OnConnected?.Invoke();

            _ = ReceiveLoopAsync(_cts.Token);
        }

        public async Task DisconnectAsync()
        {
            _cts?.Cancel();

            if (_ws?.State == WebSocketState.Open)
            {
                await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client closing", CancellationToken.None);
                OnDisconnected?.Invoke();
            }
        }


        private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
        {
            var buffer = new byte[8192];

            while (!cancellationToken.IsCancellationRequested && _ws.State == WebSocketState.Open)
            {
                var result = await _ws.ReceiveAsync(buffer, cancellationToken);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await DisconnectAsync();
                    break;
                }

                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                OnMessageReceived?.Invoke(message);
            }
        }

        public void Dispose()
        {
            _ws?.Dispose();
            _cts?.Cancel();
            _cts?.Dispose();
        }
    }

}
