using AionCoreBot.Application.Interfaces;
using AionCoreBot.Domain.Enums;
using AionCoreBot.Domain.Models;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AionCoreBot.Application.Services
{
    public class Strategizer : IStrategizer
    {
        private readonly decimal _minimumConfidenceThreshold;
        private readonly decimal _minimumActionSeparation;

        public Strategizer(decimal minimumConfidenceThreshold = 2.0m, decimal minimumActionSeparation = 0.4m)
        {
            _minimumConfidenceThreshold = minimumConfidenceThreshold;
            _minimumActionSeparation = minimumActionSeparation;
        }

        public Task<TradeDecision> DecideTradeAsync(List<SignalEvaluationResult> signals, CancellationToken cancellationToken = default)
        {
            if (signals == null || !signals.Any())
                throw new ArgumentNullException(nameof(signals));

            var weightedScores = new Dictionary<TradeAction, decimal>
        {
            { TradeAction.Buy, 0 },
            { TradeAction.Sell, 0 },
            { TradeAction.Hold, 0 }
        };

            foreach (var signal in signals)
            {
                var weight = signal.ConfidenceScore ?? 1m;
                weightedScores[signal.ProposedAction] += weight;
            }

            var ordered = weightedScores.OrderByDescending(kv => kv.Value).ToList();
            var best = ordered[0];
            var secondBest = ordered[1];

            bool aboveThreshold = best.Value >= _minimumConfidenceThreshold;
            bool clearlyBetter = (best.Value - secondBest.Value) >= _minimumActionSeparation;

            TradeAction finalAction = (aboveThreshold && clearlyBetter)
                ? best.Key
                : TradeAction.Hold;

            var reasonBuilder = new StringBuilder();
            reasonBuilder.AppendLine("Beslissing op basis van gewogen confidence:");
            foreach (var kv in weightedScores)
                reasonBuilder.AppendLine($"- {kv.Key}: {kv.Value:N2}");

            if (!aboveThreshold)
                reasonBuilder.AppendLine($"Confidence te laag voor actie ({best.Value:N2} < drempel {_minimumConfidenceThreshold:N2})");
            else if (!clearlyBetter)
                reasonBuilder.AppendLine($"Beste actie lag te dicht bij volgende ({best.Value:N2} - {secondBest.Value:N2} < drempel {_minimumActionSeparation:N2})");

            var decision = new TradeDecision
            {
                Symbol = signals.First().Symbol,
                Interval = signals.First().Interval,
                Action = finalAction,
                Reason = reasonBuilder.ToString().Trim(),
                DecisionTime = DateTime.UtcNow
            };

            return Task.FromResult(decision);
        }
    }
}
