using AionCoreBot.Domain.Enums;
using AionCoreBot.Worker.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AionCoreBot.Worker
{
    public class TradeExecutionService : ITradeExecutionService
    {
        public Task<string> SendOrderAsync(string symbol, TradeAction action, decimal quantity, decimal? price = null)
        {
            // Simuleer order-ID en succesvolle uitvoering
            var fakeOrderId = Guid.NewGuid().ToString();
            Console.WriteLine($"Mock order geplaatst: {action} {quantity} {symbol} @ {price}");
            return Task.FromResult(fakeOrderId);
        }

        public Task<bool> CancelOrderAsync(string orderId)
        {
            Console.WriteLine($"Mock order geannuleerd: {orderId}");
            return Task.FromResult(true);
        }

        public Task<OrderStatus> GetOrderStatusAsync(string orderId)
        {
            // Altijd "Filled" teruggeven voor mock
            return Task.FromResult(OrderStatus.Filled);
        }
    }
}
