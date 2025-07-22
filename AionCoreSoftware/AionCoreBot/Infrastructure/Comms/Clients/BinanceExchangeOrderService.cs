using AionCoreBot.Application.Trades.Interfaces;
using AionCoreBot.Domain.Enums;
using AionCoreBot.Infrastructure.Comms.Interfaces;
using Binance.Net.Clients;
using Binance.Net.Enums;
using Binance.Net.Objects.Models.Spot;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AionCoreBot.Infrastructure.Comms.Clients
{
    public class BinanceExchangeOrderService : IExchangeOrderService
    {
        private readonly BinanceRestClient _client;

        public BinanceExchangeOrderService(BinanceRestClient client)
        {
            _client = client;
        }

        public async Task<OrderResult> PlaceOrderAsync(
            string symbol,
            TradeAction side,
            decimal quantity,
            decimal? price,
            CancellationToken ct = default)
        {
            var orderSide = side is TradeAction.Buy or TradeAction.LimitBuy
                ? OrderSide.Buy
                : OrderSide.Sell;

            var orderType = price.HasValue
                ? SpotOrderType.Limit
                : SpotOrderType.Market;

            var result = await _client.SpotApi.Trading.PlaceOrderAsync(
                symbol,
                orderSide,
                orderType,
                quantity,
                price: price,
                timeInForce: TimeInForce.GoodTillCanceled,
                ct: ct
            );

            if (!result.Success || result.Data == null)
                throw new Exception($"Order failed: {result.Error?.Message}");

            var data = result.Data;

            // Soms is filledPrice = 0 bij Market → bereken gemiddelde
            var avgFillPrice = data.AverageFillPrice ?? (decimal?)data.Price ?? 0m;

            return new OrderResult(
                data.Id.ToString(),
                avgFillPrice,
                data.QuantityFilled
            );
        }

        public async Task<bool> ClosePositionAsync(
            string symbol,
            TradeAction side,
            decimal quantity,
            decimal? price,
            CancellationToken ct = default)
        {
            // Tegenovergestelde actie om te sluiten
            var exitSide = side is TradeAction.Buy or TradeAction.LimitBuy
                ? TradeAction.Sell
                : TradeAction.Buy;

            var closeOrder = await PlaceOrderAsync(symbol, exitSide, quantity, price, ct);
            return closeOrder.FilledQuantity > 0;
        }

        public async Task<IEnumerable<OrderResult>> GetOrderHistoryAsync()
        {
            var result = await _client.SpotApi.Trading.GetOrdersAsync();

            if (!result.Success)
                throw new Exception($"Failed to get order history: {result.Error?.Message}");

            return result.Data.Select(o => new OrderResult(
                o.Id.ToString(),
                o.AverageFillPrice ?? o.Price ?? 0m,
                o.QuantityFilled
            ));
        }
    }
}
