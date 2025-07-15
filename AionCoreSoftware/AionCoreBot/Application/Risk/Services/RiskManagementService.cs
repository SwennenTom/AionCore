using AionCoreBot.Application.Account.Interfaces;
using AionCoreBot.Application.Risk.Interfaces;
using AionCoreBot.Domain.Enums;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AionCoreBot.Application.Risk.Services
{
    public class RiskManagementService : IRiskManagementService
    {
        private const decimal RiskPct = 0.10m;
        private const decimal TestBalance = 10_000m;

        public Task<decimal> CalculateMaxRiskAmountAsync(string symbol, decimal currentPos, CancellationToken ct = default)
            => Task.FromResult(TestBalance * RiskPct);

        public decimal CalculatePositionSize(string symbol, decimal riskAmt, decimal entryPrice)
            => riskAmt / entryPrice;

        public bool IsTradeWithinRiskLimits(string symbol, TradeAction action, decimal qty) => true;

        public Task UpdateRiskParametersAsync(int tradeId, CancellationToken ct = default)
            => Task.CompletedTask;
    }

}
