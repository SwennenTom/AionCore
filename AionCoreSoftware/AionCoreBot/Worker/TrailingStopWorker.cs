using AionCoreBot.Application.Trades.Interfaces;
using AionCoreBot.Domain.Enums;
using AionCoreBot.Domain.Models;
using AionCoreBot.Infrastructure.Comms.Websocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AionCoreBot.Worker
{
    public class TrailingStopWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        private readonly ConcurrentDictionary<string, List<Trade>> _tradesBySymbol = new();
        private readonly ConcurrentDictionary<int, decimal> _highestPriceSinceOpen = new();

        public TrailingStopWorker(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine("TrailingStopWorker started.");

            // 1) Maak scope en haal scoped services
            using var scope = _scopeFactory.CreateScope();

            var tradeManager = scope.ServiceProvider.GetRequiredService<ITradeManager>();
            var wsService = scope.ServiceProvider.GetRequiredService<BinanceWebSocketService>();

            // 2) Haal open trades uit DB of memory
            var openTrades = await tradeManager.GetOpenTradesAsync(stoppingToken);

            foreach (var trade in openTrades)
            {
                _tradesBySymbol.AddOrUpdate(
                    trade.Symbol,
                    new List<Trade> { trade },
                    (key, list) => { list.Add(trade); return list; }
                );

                _highestPriceSinceOpen[trade.Id] = trade.OpenPrice;
            }

            // 3) Subscriben op candles → binnen event ook scope maken
            wsService.OnFinalCandleReceived += async (symbol, candle) =>
            {
                using var innerScope = _scopeFactory.CreateScope();
                var scopedTradeManager = innerScope.ServiceProvider.GetRequiredService<ITradeManager>();

                await HandleCandleAsync(scopedTradeManager, symbol, candle);
            };

            // 4) Start websocket service
            var symbols = _tradesBySymbol.Keys;
            await wsService.StartAsync(symbols, stoppingToken);

            // 5) Blijven wachten tot cancel
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        private async Task HandleCandleAsync(ITradeManager tradeManager, string symbol, Candle candle)
        {
            if (!_tradesBySymbol.TryGetValue(symbol, out var trades)) return;

            var lastPrice = candle.ClosePrice;

            foreach (var trade in trades.ToArray()) // snapshot voor thread safety
            {
                UpdateHighestPrice(trade, lastPrice);

                if (ShouldTriggerTrailingStop(trade, lastPrice))
                {
                    Console.WriteLine($"Trailing stop triggered for trade {trade.Id} ({trade.Symbol}) at price {lastPrice}");

                    // ✅ Trade sluiten via scoped TradeManager
                    await tradeManager.CloseTradeAsync(
                        trade,
                        exitAction: DetermineExitAction(trade),
                        executionPrice: lastPrice
                    );

                    // ✅ Cleanup lokaal
                    trades.Remove(trade);
                    _highestPriceSinceOpen.TryRemove(trade.Id, out _);
                }
            }
        }

        private void UpdateHighestPrice(Trade trade, decimal currentPrice)
        {
            _highestPriceSinceOpen.AddOrUpdate(trade.Id,
                currentPrice,
                (id, oldPrice) => Math.Max(oldPrice, currentPrice));
        }

        private bool ShouldTriggerTrailingStop(Trade trade, decimal currentPrice)
        {
            if (!_highestPriceSinceOpen.TryGetValue(trade.Id, out var highestPrice))
                highestPrice = trade.OpenPrice;

            var trailingStopPrice = highestPrice * (1 - trade.TrailingStopPercent);

            if (trade.EntryAction is TradeAction.Buy or TradeAction.LimitBuy)
                return currentPrice <= trailingStopPrice;

            if (trade.EntryAction is TradeAction.Sell or TradeAction.LimitSell)
            {
                trailingStopPrice = highestPrice * (1 + trade.TrailingStopPercent);
                return currentPrice >= trailingStopPrice;
            }

            return false;
        }

        private TradeAction DetermineExitAction(Trade trade)
        {
            return trade.EntryAction switch
            {
                TradeAction.Buy or TradeAction.LimitBuy => TradeAction.Sell,
                TradeAction.Sell or TradeAction.LimitSell => TradeAction.Buy,
                _ => TradeAction.Sell
            };
        }
    }
}
