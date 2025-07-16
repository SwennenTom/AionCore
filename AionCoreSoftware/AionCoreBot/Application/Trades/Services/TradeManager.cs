using AionCoreBot.Domain.Models;
using AionCoreBot.Domain.Enums;
using AionCoreBot.Application.Trades.Interfaces;
using AionCoreBot.Infrastructure.Comms.Interfaces;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AionCoreBot.Application.Trades.Services
{
    public class TradeManager : ITradeManager
    {
        private readonly List<Trade> _openTrades = new();
        private readonly IExchangeOrderService _exchangeOrderService;
        private readonly bool _paperTradingEnabled;

        private int _tradeIdCounter = 0;
        private int NextId() => ++_tradeIdCounter;

        public TradeManager(
            IExchangeOrderService exchangeOrderService,
            IConfiguration config)
        {
            _exchangeOrderService = exchangeOrderService
                ?? throw new ArgumentNullException(nameof(exchangeOrderService));

            // Switch uit appsettings.json -> true betekent paper mode
            _paperTradingEnabled = config.GetValue<bool>("Switches:PaperTrading");
        }

        public async Task<Trade> OpenTradeAsync(
            TradeDecision decision,
            decimal executionPrice,
            decimal quantity,
            CancellationToken ct = default)
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
                Strategy = "4h Swing",
                Reason = decision.Reason,
                Exchange = _paperTradingEnabled ? "Paper" : "Binance"
            };

            if (_paperTradingEnabled)
            {
                // ✅ Paper trade → enkel in geheugen opslaan
                _openTrades.Add(trade);
            }
            else
            {
                // ✅ Echte orderuitvoering via Binance
                var orderResult = await _exchangeOrderService.PlaceOrderAsync(
                    trade.Symbol,
                    trade.EntryAction,
                    quantity,
                    executionPrice,
                    ct
                );

                // eventueel order-id loggen
                trade.ExchangeOrderId = orderResult.OrderId;
                _openTrades.Add(trade);
            }

            return trade;
        }

        public async Task<Trade> CloseTradeAsync(
            Trade trade,
            TradeAction exitAction,
            decimal executionPrice,
            CancellationToken ct = default)
        {
            trade.ExitAction = exitAction;
            trade.CloseTime = DateTime.UtcNow;
            trade.ClosePrice = executionPrice;

            // Bereken PnL
            trade.ProfitLoss = (trade.ClosePrice.Value - trade.OpenPrice)
                               * trade.Quantity
                               * (trade.EntryAction is TradeAction.Buy or TradeAction.LimitBuy ? 1 : -1);

            if (!_paperTradingEnabled)
            {
                // ✅ Echte sluiting via Binance
                await _exchangeOrderService.ClosePositionAsync(
                    trade.Symbol,
                    exitAction,
                    trade.Quantity,
                    executionPrice,
                    ct
                );
            }

            _openTrades.Remove(trade);
            return trade;
        }

        public Task<IReadOnlyList<Trade>> GetOpenTradesAsync(CancellationToken ct = default)
            => Task.FromResult((IReadOnlyList<Trade>)_openTrades.AsReadOnly());

        public Task UpdateTradeAsync(Trade trade, CancellationToken ct = default)
            => Task.CompletedTask;
    }
}
