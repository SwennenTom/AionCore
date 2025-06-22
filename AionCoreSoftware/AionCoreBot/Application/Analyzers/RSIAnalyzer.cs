using AionCoreBot.Application.Interfaces.IAnalyzers;
using AionCoreBot.Domain.Enums;
using AionCoreBot.Domain.Models;
using Microsoft.Extensions.Configuration;

namespace AionCoreBot.Application.Analyzers
{
    public class RSIAnalyzer : IAnalyzer
    {
        private readonly IIndicatorService<RSIResult> _rsiService;
        private readonly IConfiguration _configuration;

        public RSIAnalyzer(IIndicatorService<RSIResult> rsiService, IConfiguration configuration)
        {
            _rsiService = rsiService;
            _configuration = configuration;
        }

        public async Task<SignalEvaluationResult> AnalyzeAsync(string symbol, string interval)
        {
            return await AnalyzeAsync(symbol, interval, DateTime.UtcNow);
        }

        public async Task<SignalEvaluationResult> AnalyzeAsync(string symbol, string interval, DateTime evaluationtime)
        {
            int period = _configuration.GetValue<int>("IndicatorParameters:RSI:Period", 14);
            int overbought = _configuration.GetValue<int>("IndicatorParameters:RSI:OverboughtThreshold", 70);
            int oversold = _configuration.GetValue<int>("IndicatorParameters:RSI:OversoldThreshold", 30);

            var rsi = await _rsiService.GetLatestAsync(symbol, interval, period);

            var result = new SignalEvaluationResult
            {
                Symbol = symbol,
                Interval = interval,
                EvaluationTime = evaluationtime,
                IndicatorValues = new Dictionary<string, decimal>(),
                SignalDescriptions = new List<string>(),
                ProposedAction = TradeAction.Hold,
                AnalyzerName = GetType().Name
            };

            if (rsi != null)
            {
                result.IndicatorValues[$"RSI{period}"] = rsi.Value;

                if (rsi.Value < oversold)
                {
                    result.ProposedAction = TradeAction.Buy;
                    result.SignalDescriptions.Add("RSI oversold");
                    result.Reason = $"RSI under {oversold}";

                    // Hoe lager de RSI, hoe sterker het signaal
                    var distance = oversold - rsi.Value;
                    var normalized = Math.Min(distance / oversold, 1m);
                    result.ConfidenceScore = 0.6m + 0.4m * normalized;
                }
                else if (rsi.Value > overbought)
                {
                    result.ProposedAction = TradeAction.Sell;
                    result.SignalDescriptions.Add("RSI overbought");
                    result.Reason = $"RSI over {overbought}";

                    // Hoe hoger de RSI, hoe sterker het signaal
                    var distance = rsi.Value - overbought;
                    var normalized = Math.Min(distance / (100 - overbought), 1m);
                    result.ConfidenceScore = 0.6m + 0.4m * normalized;
                }
                else
                {
                    // Neutrale RSI → confidence daalt naarmate dichter bij 50
                    result.Reason = "RSI in neutral zone";
                    result.SignalDescriptions.Add("RSI neutral");

                    var distanceToCenter = Math.Abs(rsi.Value - 50);
                    var normalized = distanceToCenter / 20m; // max afstand = 20 (van 50 naar 70/30)
                    result.ConfidenceScore = 0.4m * normalized;
                }
            }

            return result;
        }
    }
}
