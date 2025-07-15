using AionCoreBot.Application.Strategy.Interfaces;
using AionCoreBot.Domain.Enums;
using AionCoreBot.Domain.Models;
using Microsoft.Extensions.Configuration;
using System.Text;

namespace AionCoreBot.Application.Strategy.Services
{
    public class Strategizer : IStrategizer
    {
        private readonly decimal _minConfidence;
        private readonly decimal _minSeparation;

        public Strategizer(IConfiguration cfg)
        {
            _minConfidence = cfg.GetValue("Strategy:DecisionThreshold", 2.0m);
            _minSeparation = cfg.GetValue("Strategy:MinSeparation", 0.4m);
        }

        public Task<TradeDecision> DecideTradeAsync(
            List<SignalEvaluationResult> signals,
            CancellationToken ct = default)
        {
            if (signals == null || !signals.Any())
                throw new ArgumentNullException(nameof(signals));

            // 1. Weging per TradeAction
            var weighted = new Dictionary<TradeAction, decimal>();
            foreach (var s in signals)
            {
                if (!weighted.ContainsKey(s.ProposedAction))
                    weighted[s.ProposedAction] = 0m;

                weighted[s.ProposedAction] += s.ConfidenceScore ?? 0m;
            }

            var ordered = weighted.OrderByDescending(kv => kv.Value).ToList();
            var best = ordered[0];
            var second = ordered.Count > 1 ? ordered[1] : new(best.Key, 0m);

            bool above = best.Value >= _minConfidence;
            bool better = best.Value - second.Value >= _minSeparation;

            var finalAction = (above && better) ? best.Key : TradeAction.Hold;

            // 2. Motivatie opbouwen
            var sb = new StringBuilder();
            sb.AppendLine("Beslissing op basis van gewogen confidence:");
            foreach (var kv in weighted)
                sb.AppendLine($"- {kv.Key}: {kv.Value:N2}");

            if (!above) sb.AppendLine($"Te lage confidence ({best.Value:N2} < {_minConfidence:N2})");
            else if (!better)
                sb.AppendLine($"Acties liggen te dicht bij elkaar ({best.Value:N2} vs {second.Value:N2})");

            return Task.FromResult(new TradeDecision
            {
                Symbol = signals[0].Symbol,
                Interval = signals[0].Interval,
                Action = finalAction,
                Reason = sb.ToString().Trim(),
                DecisionTime = DateTime.UtcNow
            });
        }
    }
}
