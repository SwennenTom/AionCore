using AionCoreBot.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AionCoreBot.Infrastructure.Comms.Interfaces
{
    public interface IExchangeOrderService
    {
        Task<OrderResult> PlaceOrderAsync(
            string symbol,
            TradeAction side,
            decimal quantity,
            decimal? price,
            CancellationToken ct = default);

        Task<bool> ClosePositionAsync(
            string symbol,
            TradeAction side,
            decimal quantity,
            decimal? price,
            CancellationToken ct = default);

        Task<IEnumerable<OrderResult>> GetOrderHistoryAsync();

    }

    public record OrderResult(string OrderId, decimal FilledPrice, decimal FilledQuantity);


}
