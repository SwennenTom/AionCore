using AionCoreBot.Domain.Models;
using AionCoreBot.Domain.Enums;
using AionCoreBot.Application.Trades.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AionCoreBot.Application.Trades.Services
{
    public class TradeManager : ITradeManager
    {
        private readonly List<Trade> _openTrades = new();
        private int _tradeIdCounter = 0;
        private int NextId() => ++_tradeIdCounter;

        public Task<Trade> OpenTradeAsync(TradeDecision decision, decimal executionPrice, decimal quantity, CancellationToken ct = default)
        {
            var trade = new Trade
            {
                Id = NextId(),
                Symbol = decision.Symbol,
                Interval = decision.Interval,
                EntryAction = decision.Action,
                OpenTime = DateTime.UtcNow,
                OpenPrice = executionPrice,
                Quantity = quantity,
                Strategy = "Strategizer",     // evt. meegeven
                Reason = decision.Reason,
                Exchange = "Paper"            // of “Binance”
            };

            _openTrades.Add(trade);

            // TODO: echte order-uitvoering
            return Task.FromResult(trade);
        }

        public Task<Trade> CloseTradeAsync(
            Trade trade,
            TradeAction exitAction,
            decimal executionPrice,
            CancellationToken ct = default)
        {
            trade.ExitAction = exitAction;
            trade.CloseTime = DateTime.UtcNow;
            trade.ClosePrice = executionPrice;
            trade.ProfitLoss = (trade.ClosePrice.Value - trade.OpenPrice)
                               * trade.Quantity
                               * (trade.EntryAction is TradeAction.Buy or TradeAction.LimitBuy ? 1 : -1);

            _openTrades.Remove(trade);
            return Task.FromResult(trade);
        }

        public Task<IReadOnlyList<Trade>> GetOpenTradesAsync(CancellationToken ct = default)
            => Task.FromResult((IReadOnlyList<Trade>)_openTrades.AsReadOnly());

        public Task UpdateTradeAsync(Trade trade, CancellationToken ct = default)
            => Task.CompletedTask;
    }
}
