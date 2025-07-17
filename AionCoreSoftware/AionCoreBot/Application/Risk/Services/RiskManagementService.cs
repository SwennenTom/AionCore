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

        private readonly decimal _maxPortfolioRiskPerTrade;
        private readonly decimal _stopLossPct;
        private readonly decimal _takeProfitRatio;

        public RiskManagementService(
            IBalanceProvider balanceProvider,
            IConfiguration config)
        {
            _balanceProvider = balanceProvider ?? throw new ArgumentNullException(nameof(balanceProvider));

            _maxPortfolioRiskPerTrade = config.GetValue<decimal>("RiskManagement:MaxPortfolioRiskPerTrade", 0.02m);
            _stopLossPct = config.GetValue<decimal>("RiskManagement:StopLossPercentage", 0.02m);
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

        public Task<decimal> GetStopLossPriceAsync(string symbol, decimal entryPrice, CancellationToken ct = default)
        {
            decimal stopLossPrice = entryPrice * (1 - _stopLossPct);
            return Task.FromResult(Math.Round(stopLossPrice, 2));
        }

        public Task<decimal> GetTakeProfitPriceAsync(string symbol, decimal entryPrice, CancellationToken ct = default)
        {
            decimal stopLossDistance = entryPrice * _stopLossPct;
            decimal takeProfitPrice = entryPrice * (1 + (stopLossDistance * _takeProfitRatio));
            return Task.FromResult(Math.Round(takeProfitPrice, 2));
        }

        public Task<decimal> GetTrailingStopPercentAsync(string symbol, CancellationToken ct = default)
        {
            throw new NotImplementedException();
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
