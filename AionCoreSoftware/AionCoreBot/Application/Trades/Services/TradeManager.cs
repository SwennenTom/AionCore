using AionCoreBot.Application.Risk.Interfaces;
using AionCoreBot.Application.Trades.Interfaces;
using AionCoreBot.Domain.Enums;
using AionCoreBot.Domain.Models;
using AionCoreBot.Infrastructure.Comms.Interfaces;
using AionCoreBot.Infrastructure.Interfaces;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AionCoreBot.Application.Trades.Services
{
    public class TradeManager : ITradeManager
    {
        private readonly List<Trade> _openTrades = new();
        private readonly IExchangeOrderService _exchangeOrderService;
        private readonly IRiskManagementService _riskManagementService;
        private readonly ITradeRepository _tradeRepository;
        private readonly bool _paperTradingEnabled;

        private int _tradeIdCounter = 0;
        private int NextId() => ++_tradeIdCounter;

        public TradeManager(
            IExchangeOrderService exchangeOrderService,
            IConfiguration config,
            IRiskManagementService risk, ITradeRepository tradeRepository)
        {
            _exchangeOrderService = exchangeOrderService
                ?? throw new ArgumentNullException(nameof(exchangeOrderService));
            _riskManagementService = risk
                ?? throw new ArgumentNullException(nameof(risk));
            _tradeRepository = tradeRepository
                ?? throw new ArgumentNullException(nameof(tradeRepository));
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
                StopLossPrice = await _riskManagementService.GetStopLossPriceAsync(decision.Symbol, executionPrice, ct),
                TakeProfitPrice = await _riskManagementService.GetTakeProfitPriceAsync(decision.Symbol, executionPrice, ct),
                Strategy = "4h Swing",
                Reason = decision.Reason,
                Exchange = _paperTradingEnabled ? "Paper" : "Binance"
            };

            if (_paperTradingEnabled)
            {
                _openTrades.Add(trade);
                await _tradeRepository.AddAsync(trade);
            }
            else
            {
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
                await _tradeRepository.AddAsync(trade);
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
            await _tradeRepository.Update(trade);
            return trade;
        }

        public Task<IReadOnlyList<Trade>> GetOpenTradesAsync(CancellationToken ct = default)
            => Task.FromResult((IReadOnlyList<Trade>)_openTrades.AsReadOnly());

        public Task UpdateTradeAsync(Trade trade, CancellationToken ct = default)
            => Task.CompletedTask;

        private TradeAction DetermineExitAction(Trade trade)
        {
            return trade.EntryAction switch
            {
                TradeAction.Buy or TradeAction.LimitBuy => TradeAction.Sell,
                TradeAction.Sell or TradeAction.LimitSell => TradeAction.Buy,
                _ => TradeAction.Sell
            };
        }

        public async Task SyncWithExchangeAsync(CancellationToken ct = default)
        {
            // ✅ 1. Verzamel unieke symbolen van open trades
            var symbols = _openTrades
                .Select(t => t.Symbol)
                .Distinct()
                .ToList();

            foreach (var symbol in symbols)
            {
                // ✅ 2. Haal orderhistoriek per symbool op via Binance
                var orders = await _exchangeOrderService.GetOrderHistoryAsync(symbol, ct);

                // ✅ 3. Bouw een lookup map op OrderId
                var orderMap = orders.ToDictionary(o => o.OrderId);

                // ✅ 4. Check alleen trades voor dit symbool
                var tradesForSymbol = _openTrades
                    .Where(t => t.Symbol == symbol)
                    .ToList();

                foreach (var openTrade in tradesForSymbol)
                {
                    if (openTrade.ExchangeOrderId == null)
                        continue; // skip trades zonder exchange-id (bv. paper)

                    if (orderMap.TryGetValue(openTrade.ExchangeOrderId, out var matchingOrder))
                    {
                        // ✅ Als filledQuantity >= geaggregeerde hoeveelheid → trade sluiten
                        if (matchingOrder.FilledQuantity >= openTrade.Quantity)
                        {
                            var exitAction = DetermineExitAction(openTrade);

                            // ❗ matchingOrder.FilledPrice is al veilig nullable -> fallback naar 0m
                            var fillPrice = (decimal?)matchingOrder.FilledPrice ?? 0m;

                            await CloseTradeAsync(openTrade, exitAction, fillPrice, ct);
                        }
                    }
                }
            }
        }

    }
}
