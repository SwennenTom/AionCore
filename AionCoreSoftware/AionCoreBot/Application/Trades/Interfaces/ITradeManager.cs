using AionCoreBot.Domain.Models;
using AionCoreBot.Domain.Enums;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace AionCoreBot.Application.Trades.Interfaces
{
    public interface ITradeManager
    {
        Task<Trade> OpenTradeAsync(
            TradeDecision decision,
            decimal executionPrice,
            decimal quantity,
            CancellationToken ct = default);

        Task<Trade> CloseTradeAsync(
            Trade trade,
            TradeAction exitAction,
            decimal executionPrice,
            CancellationToken ct = default);

        Task<IReadOnlyList<Trade>> GetOpenTradesAsync(CancellationToken ct = default);
        Task UpdateTradeAsync(Trade trade, CancellationToken ct = default);
        Task SyncWithExchangeAsync(CancellationToken ct = default);
    }
}
