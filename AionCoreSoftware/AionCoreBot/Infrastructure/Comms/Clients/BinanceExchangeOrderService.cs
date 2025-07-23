using AionCoreBot.Application.Risk.Interfaces;
using AionCoreBot.Domain.Enums;
using AionCoreBot.Infrastructure.Comms.Interfaces;
using Binance.Net.Clients;
using Binance.Net.Enums;

public class BinanceExchangeOrderService : IExchangeOrderService
{
    private readonly BinanceRestClient _client;
    private readonly IRiskManagementService _risk;

    public BinanceExchangeOrderService(BinanceRestClient client, IRiskManagementService riskManagement)
    {
        _client = client;
        _risk = riskManagement;
    }

    public async Task<OrderResult> PlaceOrderAsync(
        string symbol,
        TradeAction side,
        decimal quantity,
        decimal? manualPrice,
        CancellationToken ct = default)
    {
        var orderSide = side is TradeAction.Buy or TradeAction.LimitBuy
            ? OrderSide.Buy
            : OrderSide.Sell;

        var orderType = manualPrice.HasValue
            ? SpotOrderType.Limit
            : SpotOrderType.Market;

        // ✅ Step 1: Place ENTRY order
        var entryResult = await _client.SpotApi.Trading.PlaceOrderAsync(
            symbol,
            orderSide,
            orderType,
            quantity,
            price: manualPrice,
            timeInForce: TimeInForce.GoodTillCanceled,
            ct: ct
        );

        if (!entryResult.Success || entryResult.Data == null)
            throw new Exception($"Order failed: {entryResult.Error?.Message}");

        var entryData = entryResult.Data;

        var avgFillPrice = entryData.AverageFillPrice
            ?? (decimal?)entryData.Price
            ?? (decimal?)manualPrice
            ?? 0m;

        // ✅ Step 2: Compute SL/TP
        var stopLossPrice = await _risk.GetStopLossPriceAsync(symbol, avgFillPrice, ct);
        var takeProfitPrice = await _risk.GetTakeProfitPriceAsync(symbol, avgFillPrice, ct);

        // ✅ Step 3: OCO Protection
        if (side == TradeAction.Buy || side == TradeAction.LimitBuy)
        {
            // Entry BUY → SELL OCO
            var ocoResult = await _client.SpotApi.Trading.PlaceOcoOrderAsync(
                symbol,
                OrderSide.Sell,
                quantity,
                takeProfitPrice,
                stopPrice: stopLossPrice,
                stopLimitPrice: stopLossPrice * 0.999m,
                ct: ct
            );

            if (!ocoResult.Success)
                throw new Exception($"Failed to place OCO (SL/TP): {ocoResult.Error?.Message}");
        }
        else
        {
            // Entry SELL → BUY OCO
            var ocoResult = await _client.SpotApi.Trading.PlaceOcoOrderAsync(
                symbol,
                OrderSide.Buy,
                quantity,
                takeProfitPrice,
                stopPrice: stopLossPrice,
                stopLimitPrice: stopLossPrice * 1.001m,
                ct: ct
            );

            if (!ocoResult.Success)
                throw new Exception($"Failed to place OCO (SL/TP): {ocoResult.Error?.Message}");
        }

        return new OrderResult(
            entryData.Id.ToString(),
            avgFillPrice,
            entryData.QuantityFilled
        );
    }

    public async Task<bool> ClosePositionAsync(
        string symbol,
        TradeAction side,
        decimal quantity,
        decimal? price,
        CancellationToken ct = default)
    {
        var exitSide = side is TradeAction.Buy or TradeAction.LimitBuy
            ? TradeAction.Sell
            : TradeAction.Buy;

        var closeOrder = await PlaceOrderAsync(symbol, exitSide, quantity, price, ct);
        return closeOrder.FilledQuantity > 0;
    }

    public async Task<IEnumerable<OrderResult>> GetOrderHistoryAsync(string symbol, CancellationToken ct = default)
    {
        var result = await _client.SpotApi.Trading.GetOrdersAsync(symbol, ct: ct);

        if (!result.Success)
            throw new Exception($"Failed to get order history for {symbol}: {result.Error?.Message}");

        return result.Data.Select(o => new OrderResult(
            o.Id.ToString(),
            o.AverageFillPrice ?? (decimal?)o.Price ?? 0m,
            o.QuantityFilled
        ));
    }
}

