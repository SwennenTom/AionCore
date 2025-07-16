using AionCoreBot.Application.Account.Interfaces;
using AionCoreBot.Application.Risk.Interfaces;
using AionCoreBot.Domain.Enums;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AionCoreBot.Application.Risk.Services
{
    public class RiskManagementService : IRiskManagementService
    {
        private readonly IBalanceProvider _balanceProvider;

        private readonly decimal _maxPortfolioRiskPerTrade;   // bv. 0.02 (2%)
        private readonly decimal _stopLossPct;                // bv. 0.05 (5%)
        private readonly decimal _takeProfitRatio;            // bv. 2.0 (RR=1:2)

        public RiskManagementService(
            IBalanceProvider balanceProvider,
            IConfiguration config)
        {
            _balanceProvider = balanceProvider ?? throw new ArgumentNullException(nameof(balanceProvider));

            _maxPortfolioRiskPerTrade = config.GetValue<decimal>("RiskManagement:MaxPortfolioRiskPerTrade", 0.02m);
            _stopLossPct = config.GetValue<decimal>("RiskManagement:StopLossPercentage", 0.05m);
            _takeProfitRatio = config.GetValue<decimal>("RiskManagement:TakeProfitRatio", 2.0m);
        }

        public async Task<decimal> CalculateMaxRiskAmountAsync(string symbol, decimal currentPos, CancellationToken ct = default)
        {
            var balances = await _balanceProvider.GetBalancesAsync(ct);

            var quoteAsset = GetQuoteAsset(symbol);

            if (!balances.TryGetValue(quoteAsset, out var availableBalance))
                throw new Exception($"Geen beschikbare balans voor {quoteAsset}");

            Console.WriteLine($"[RISK] Beschikbare balans voor {quoteAsset}: {availableBalance}");

            return availableBalance * _maxPortfolioRiskPerTrade;
        }

        public decimal CalculatePositionSize(string symbol, decimal riskAmount, decimal entryPrice)
        {
            // hoeveel base asset kan je kopen met dit risico-bedrag?
            var qty = riskAmount / (entryPrice * _stopLossPct);
            return Math.Round(qty, 6); // afronden op 6 decimals voor crypto
        }

        public bool IsTradeWithinRiskLimits(string symbol, TradeAction action, decimal qty)
        {
            // Hier kun je later bv. max exposure per symbool toevoegen
            return true;
        }

        /// <summary>
        /// Placeholder: later kun je hier SL/TP dynamic aanpassen na uitvoering.
        /// </summary>
        public Task UpdateRiskParametersAsync(int tradeId, CancellationToken ct = default)
            => Task.CompletedTask;

        private string GetQuoteAsset(string symbol)
        {
            if (symbol.EndsWith("EUR", StringComparison.InvariantCultureIgnoreCase)) return "EUR";
            if (symbol.EndsWith("USDT", StringComparison.InvariantCultureIgnoreCase)) return "USDT";
            return "EUR"; // fallback
        }
    }
}
