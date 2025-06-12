using AionCoreBot.Application.Interfaces;
using AionCoreBot.Domain.Enums;
using AionCoreBot.Domain.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AionCoreBot.Application.Services
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
        public Task<TradeDecision> ValidateTradeDecisionAsync(
            TradeDecision tradeDecision,
            decimal currentPositionSize,
            decimal entryPrice,
            CancellationToken cancellationToken = default)
        {
            if (tradeDecision == null)
                throw new ArgumentNullException(nameof(tradeDecision));

            if (entryPrice <= 0)
                throw new ArgumentException("Entry price moet groter dan 0 zijn.", nameof(entryPrice));

            // Bereken maximaal risico bedrag (bv. in USD)
            var maxRiskAmount = _riskManagementService.CalculateMaxRiskAmount(tradeDecision.Symbol, currentPositionSize);

            // Bepaal voorgestelde positie grootte volgens risk management (aantal eenheden)
            var suggestedQuantity = _riskManagementService.CalculatePositionSize(tradeDecision.Symbol, maxRiskAmount, entryPrice);

            // Check of de voorgestelde trade binnen risk limieten valt
            bool isWithinLimits = _riskManagementService.IsTradeWithinRiskLimits(tradeDecision.Symbol, tradeDecision.Action, suggestedQuantity);

            if (!isWithinLimits)
            {
                // Niet toegestaan, actie omzetten naar Hold
                tradeDecision.Action = TradeAction.Hold;

                // Reset quantity naar 0
                tradeDecision.Quantity = 0;

                // Update reden met toelichting
                tradeDecision.Reason = (tradeDecision.Reason ?? "") + " | Trade geannuleerd door Risk Management: positie te groot.";
            }
            else
            {
                // Trade is OK, update quantity en reden
                tradeDecision.Quantity = suggestedQuantity;
                tradeDecision.Reason = (tradeDecision.Reason ?? "") + $" | Goedgekeurd door Risk Management: max risico {maxRiskAmount:N2}, positie grootte {suggestedQuantity:N4} @ prijs {entryPrice:N2}.";
            }

            // Update beslissingsmoment
            tradeDecision.DecisionTime = DateTime.UtcNow;

            return Task.FromResult(tradeDecision);
        }
    }
}
