using AionCoreBot.Domain.Enums;

namespace AionCoreBot.Application.Risk.Interfaces
{
    public interface IRiskManagementService
    {
        Task<decimal> CalculateMaxRiskAmountAsync(string symbol, decimal currentPositionSize, CancellationToken ct = default);
        decimal CalculatePositionSize(string symbol, decimal riskAmount, decimal entryPrice);
        bool IsTradeWithinRiskLimits(string symbol, TradeAction proposedAction, decimal positionSize);
        Task UpdateRiskParametersAsync(int tradeId, CancellationToken ct = default);
        Task<decimal> GetStopLossPriceAsync(string symbol, decimal entryPrice, CancellationToken ct = default);
        Task<decimal> GetTakeProfitPriceAsync(string symbol, decimal entryPrice, CancellationToken ct = default);
        Task<decimal> GetTrailingStopPercentAsync(string symbol, CancellationToken ct = default);
    }
}