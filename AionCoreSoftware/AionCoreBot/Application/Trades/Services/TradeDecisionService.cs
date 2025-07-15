using AionCoreBot.Application.Risk.Interfaces;
using AionCoreBot.Domain.Enums;
using AionCoreBot.Domain.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AionCoreBot.Application.Trades.Services
{
    public class TradeDecisionService
    {
        private readonly IRiskManagementService _riskManagementService;

        public TradeDecisionService(IRiskManagementService riskManagementService)
        {
            _riskManagementService = riskManagementService ?? throw new ArgumentNullException(nameof(riskManagementService));
        }

        /// <summary>
        /// Valideert en past de trade beslissing aan op basis van risk management regels.
        /// </summary>
        /// <param name="tradeDecision">De voorgestelde trade beslissing van de strategizer.</param>
        /// <param name="currentPositionSize">De huidige positie grootte voor het symbool.</param>
        /// <param name="entryPrice">De prijs waartegen de trade geopend zou worden.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>De gevalideerde en mogelijk aangepaste trade beslissing.</returns>
        public async Task<TradeDecision> ValidateTradeDecisionAsync(
    TradeDecision tradeDecision,
    decimal currentPositionSize,
    decimal entryPrice,
    CancellationToken ct = default)
        {
            if (tradeDecision is null)
                throw new ArgumentNullException(nameof(tradeDecision));
            if (entryPrice <= 0)
                throw new ArgumentException("Entry price moet > 0 zijn.", nameof(entryPrice));

            /* 1️⃣  Maximaal risicobedrag asynchroon ophalen */
            decimal maxRiskAmount =
                await _riskManagementService.CalculateMaxRiskAmountAsync(
                        tradeDecision.Symbol,
                        currentPositionSize,
                        ct);

            /* 2️⃣  Positiegrootte berekenen met het decimale resultaat */
            decimal suggestedQuantity =
                _riskManagementService.CalculatePositionSize(
                        tradeDecision.Symbol,
                        maxRiskAmount,
                        entryPrice);

            /* 3️⃣  Risk-check */
            bool within =
                _riskManagementService.IsTradeWithinRiskLimits(
                        tradeDecision.Symbol,
                        tradeDecision.Action,
                        suggestedQuantity);

            if (!within)
            {
                tradeDecision.Action = TradeAction.Hold;
                tradeDecision.Quantity = 0;
                tradeDecision.Reason += " | Trade geannuleerd door Risk Management: positie te groot.";
            }
            else
            {
                tradeDecision.Quantity = suggestedQuantity;
                tradeDecision.Reason +=
                    $" | Goedgekeurd: max risico {maxRiskAmount:N2}, qty {suggestedQuantity:N4} @ {entryPrice:N2}.";
            }

            tradeDecision.DecisionTime = DateTime.UtcNow;
            return tradeDecision;
        }

    }
}
