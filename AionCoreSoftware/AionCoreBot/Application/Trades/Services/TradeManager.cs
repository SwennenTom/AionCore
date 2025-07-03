using AionCoreBot.Domain.Models;
using AionCoreBot.Domain.Enums;
using AionCoreBot.Application.Trades.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AionCoreBot.Application.Risk.Services;

namespace AionCoreBot.Application.Trades.Services
{
    public class TradeManager : ITradeManager
    {
        private readonly List<Trade> _openTrades = new();

        public Task<Trade> OpenTradeAsync(TradeDecision decision, decimal executionPrice)
        {
            var trade = new Trade
            {
                Id = GenerateTradeId(),
                Symbol = decision.Symbol,
                Interval = decision.Interval,
                EntryAction = decision.Action,
                OpenTime = DateTime.UtcNow,
                OpenPrice = executionPrice,
                Quantity = decision.Quantity ?? 0,
                Strategy = "StrategizerName", // eventueel dynamisch meegeven
                Reason = decision.Reason,
                Exchange = "DefaultExchange"
            };

            _openTrades.Add(trade);

            // Hier kan je later order executie aanroepen via TradeExecutionService

            return Task.FromResult(trade);
        }

        public Task<Trade> CloseTradeAsync(Trade trade, TradeAction exitAction, decimal executionPrice)
        {
            trade.ExitAction = exitAction;
            trade.CloseTime = DateTime.UtcNow;
            trade.ClosePrice = executionPrice;
            trade.ProfitLoss = CalculatePnL(trade);
            // Mogelijk fees bijwerken

            // Trade sluiten uit open lijst
            _openTrades.Remove(trade);

            // Hier ook update order uitvoeren via TradeExecutionService

            return Task.FromResult(trade);
        }

        public Task<IReadOnlyList<Trade>> GetOpenTradesAsync()
        {
            return Task.FromResult((IReadOnlyList<Trade>)_openTrades.AsReadOnly());
        }

        public Task UpdateTradeAsync(Trade trade)
        {
            // Placeholder om trade updates door te voeren, bvb na orderstatus updates
            return Task.CompletedTask;
        }

        private decimal CalculatePnL(Trade trade)
        {
            if (!trade.ClosePrice.HasValue) return 0m;

            var priceDifference = trade.ClosePrice.Value - trade.OpenPrice;
            var direction = trade.EntryAction == TradeAction.Buy || trade.EntryAction == TradeAction.LimitBuy ? 1 : -1;
            return priceDifference * trade.Quantity * direction;
        }

        private int _tradeIdCounter = 0;
        private int GenerateTradeId() => ++_tradeIdCounter;
    }
}
