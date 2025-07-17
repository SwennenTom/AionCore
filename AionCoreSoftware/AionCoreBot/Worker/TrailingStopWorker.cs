using AionCoreBot.Application.Trades.Interfaces;
using AionCoreBot.Domain.Enums;
using AionCoreBot.Domain.Models;
using AionCoreBot.Infrastructure.Comms.Websocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
        private readonly ITradeManager _tradeManager;
        private readonly BinanceWebSocketService _binanceWebSocketService;

        // Houdt per symbool open trades bij
        private readonly ConcurrentDictionary<string, List<Trade>> _tradesBySymbol = new();

        // Track highest price sinds open voor trailing stops
        private readonly ConcurrentDictionary<int, decimal> _highestPriceSinceOpen = new();

        public TrailingStopWorker(
            IServiceScopeFactory scopeFactory,
            ITradeManager tradeManager,
            BinanceWebSocketService binanceWebSocketService)
        {
            _scopeFactory = scopeFactory;
            _tradeManager = tradeManager;
            _binanceWebSocketService = binanceWebSocketService;
            _binanceWebSocketService.OnFinalCandleReceived += HandleCandleAsync;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine("TrailingStopWorker started.");

            // 1) Open trades ophalen
            var openTrades = await _tradeManager.GetOpenTradesAsync(stoppingToken);

            // Groepeer per symbool in concurrent dictionary
            foreach (var trade in openTrades)
            {
                _tradesBySymbol.AddOrUpdate(
                    trade.Symbol,
                    new List<Trade> { trade },
                    (key, list) => { list.Add(trade); return list; }
                );

                // Init highest price met openprice
                _highestPriceSinceOpen[trade.Id] = trade.OpenPrice;
            }

            // 2) Start websocket service met deze symbolen (1m kline streams)
            var symbols = _tradesBySymbol.Keys;
            await _binanceWebSocketService.StartAsync(symbols, stoppingToken);

            // 3) Wacht tot cancellation
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        // Event handler voor nieuwe 1m candle per symbol
        private async Task HandleCandleAsync(string symbol, Domain.Models.Candle candle)
        {
            if (!_tradesBySymbol.TryGetValue(symbol, out var trades)) return;

            var lastPrice = candle.ClosePrice;

            foreach (var trade in trades.ToArray()) // copy om thread safe te zijn
            {
                UpdateHighestPrice(trade, lastPrice);

                if (ShouldTriggerTrailingStop(trade, lastPrice))
                {
                    Console.WriteLine($"Trailing stop triggered for trade {trade.Id} ({trade.Symbol}) at price {lastPrice}");

                    // Sluit trade via TradeManager
                    await _tradeManager.CloseTradeAsync(trade, exitAction: DetermineExitAction(trade), executionPrice: lastPrice);

                    // Trade verwijderen uit lokale lijst
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

            // Trigger als de prijs onder trailing stop valt (voor long trades)
            if (trade.EntryAction is TradeAction.Buy or TradeAction.LimitBuy)
            {
                return currentPrice <= trailingStopPrice;
            }

            // Voor short trades kun je de logica omdraaien
            if (trade.EntryAction is TradeAction.Sell or TradeAction.LimitSell)
            {
                trailingStopPrice = highestPrice * (1 + trade.TrailingStopPercent);
                return currentPrice >= trailingStopPrice;
            }

            return false;
        }

        private TradeAction DetermineExitAction(Trade trade)
        {
            // Sluit het tegenovergestelde van de entry actie
            return trade.EntryAction switch
            {
                TradeAction.Buy or TradeAction.LimitBuy => TradeAction.Sell,
                TradeAction.Sell or TradeAction.LimitSell => TradeAction.Buy,
                _ => TradeAction.Sell
            };
        }
    }
}
