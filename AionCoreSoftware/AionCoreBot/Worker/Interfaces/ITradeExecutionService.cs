using AionCoreBot.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AionCoreBot.Worker.Interfaces
{
    public interface ITradeExecutionService
    {
        Task<string> SendOrderAsync(string symbol, TradeAction action, decimal quantity, decimal? price = null);
        Task<bool> CancelOrderAsync(string orderId);
        Task<OrderStatus> GetOrderStatusAsync(string orderId);
    }
}
