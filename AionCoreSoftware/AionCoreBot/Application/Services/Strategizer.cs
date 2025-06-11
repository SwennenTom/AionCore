using AionCoreBot.Application.Interfaces;
using AionCoreBot.Domain.Enums;
using AionCoreBot.Domain.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AionCoreBot.Application.Services
{
    public class Strategizer : IStrategizer
    {
        public Task<TradeDecision> DecideTradeAsync(SignalEvaluationResult signals, CancellationToken cancellationToken = default)
        {
            if (signals == null)
                throw new ArgumentNullException(nameof(signals));

            // Default naar Hold
            TradeAction finalAction = TradeAction.Hold;
            string? reason = "Geen duidelijke signalen";

            // Simpele prioriteit: Sell > Buy > Hold
            if (signals.ProposedAction == TradeAction.Sell)
            {
                finalAction = TradeAction.Sell;
                reason = "SignalEvaluator gaf Sell signaal";
            }
            else if (signals.ProposedAction == TradeAction.Buy)
            {
                finalAction = TradeAction.Buy;
                reason = "SignalEvaluator gaf Buy signaal";
            }

            var decision = new TradeDecision
            {
                Symbol = signals.Symbol,
                Interval = signals.Interval,
                Action = finalAction,
                Reason = reason,
                DecisionTime = DateTime.UtcNow,
                // Optioneel kun je hier SuggestedPrice en Quantity invullen, bijvoorbeeld uit signals.IndicatorValues of elders
            };

            return Task.FromResult(decision);
        }
    }
}
