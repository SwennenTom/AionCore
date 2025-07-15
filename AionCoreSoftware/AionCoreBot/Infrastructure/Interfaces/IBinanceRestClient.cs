using System.Threading;
using System.Threading.Tasks;

namespace AionCoreBot.Infrastructure.Interfaces
{
    public interface IBinanceRestClient
    {
        Task<string> GetAccountInfoAsync(CancellationToken ct = default);

        Task<string> PlaceOrderAsync(
            string symbol,
            string side,
            string type,
            decimal quantity,
            decimal? price = null,
            CancellationToken ct = default);

        Task<string> GetOrderStatusAsync(string symbol, long orderId, CancellationToken ct = default);

        Task<string> CancelOrderAsync(string symbol, long orderId, CancellationToken ct = default);

        Task<string> GetRawAsync(string url, CancellationToken ct = default);
    }
}


//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace AionCoreBot.Infrastructure.Interfaces
//{
//    public interface IBinanceRestClient
//    {
//        Task<string> GetAccountInfoAsync(CancellationToken ct = default);
//        Task<string> PlaceOrderAsync(string symbol, string side, string type, decimal quantity, decimal? price = null);
//        Task<string> GetOrderStatusAsync(string symbol, long orderId);
//        Task<string> CancelOrderAsync(string symbol, long orderId);
//        Task<string> GetRawAsync(string url, CancellationToken ct = default);
//    }
//}
