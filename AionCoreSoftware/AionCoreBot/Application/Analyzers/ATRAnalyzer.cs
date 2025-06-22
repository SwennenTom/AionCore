using AionCoreBot.Application.Interfaces.IAnalyzers;
using AionCoreBot.Domain.Enums;
using AionCoreBot.Domain.Models;
using Microsoft.Extensions.Configuration;

namespace AionCoreBot.Application.Analyzers
{
    public class ATRAnalyzer : IAnalyzer
    {
        private readonly IIndicatorService<ATRResult> _atrService;
        private readonly IConfiguration _configuration;

        public ATRAnalyzer(IIndicatorService<ATRResult> atrService, IConfiguration configuration)
        {
            _atrService = atrService;
            _configuration = configuration;
        }

        public async Task<SignalEvaluationResult> AnalyzeAsync(string symbol, string interval)
        {
            int period = _configuration.GetValue<int>("IndicatorParameters:ATR:Period", 14);
            decimal thresholdPercent = _configuration.GetValue<decimal>("IndicatorParameters:ATR:Threshold", 3.0m) / 100m;
            decimal lowerBound = thresholdPercent / _configuration.GetValue<int>("IndicatorParameters:ATR:LowerBoundFactor", 10);

            var atr = await _atrService.GetLatestAsync(symbol, interval, period);

            var result = new SignalEvaluationResult
            {
                Symbol = symbol,
                Interval = interval,
                EvaluationTime = DateTime.UtcNow,
                IndicatorValues = new Dictionary<string, decimal>(),
                SignalDescriptions = new List<string>(),
                ProposedAction = TradeAction.Hold, // Default
            };

            if (atr != null && atr.ClosePrice > 0)
            {
                result.IndicatorValues[$"ATR{period}"] = atr.Value;

                var percentage = atr.Value / atr.ClosePrice;

                decimal maxDistance = thresholdPercent - lowerBound;

                if (percentage > thresholdPercent)
                {
                    result.ProposedAction = TradeAction.NoBuy;
                    result.SignalDescriptions.Add($"ATR: high volatility ({percentage:P1})");
                    result.Reason = $"ATR > {thresholdPercent:P0} of price — avoid buying";

                    var overshoot = percentage - thresholdPercent;
                    var normalized = 1m - Math.Min(overshoot / thresholdPercent, 1m);
                    result.ConfidenceScore = 0.4m * normalized;
                }
                else if (percentage < lowerBound)
                {
                    result.ProposedAction = TradeAction.NoBuy;
                    result.SignalDescriptions.Add($"ATR: too low volatility ({percentage:P1})");
                    result.Reason = $"ATR < {lowerBound:P0} of price — no market movement";

                    var overshoot = lowerBound - percentage;
                    var normalized = 1m - Math.Min(overshoot / lowerBound, 1m);
                    result.ConfidenceScore = 0.4m * normalized;
                }
                else
                {
                    result.ProposedAction = TradeAction.OkToBuy;
                    result.SignalDescriptions.Add($"ATR: normal volatility ({percentage:P1})");
                    result.Reason = $"ATR within acceptable range";

                    var distanceFromEdges = Math.Min(percentage - lowerBound, thresholdPercent - percentage);
                    var normalized = distanceFromEdges / (maxDistance / 2m);
                    result.ConfidenceScore = 0.6m + 0.4m * normalized;
                }

            }
            return result;
        }

    }
}