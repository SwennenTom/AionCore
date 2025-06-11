using AionCoreBot.Application.Interfaces;
using AionCoreBot.Domain.Enums;
using AionCoreBot.Domain.Interfaces;
using AionCoreBot.Domain.Models;

namespace AionCoreBot.Application.Analyzers
{
    public class RSIAnalyzer : IAnalyzer
    {
        private readonly IIndicatorService<RSIResult> _rsiService;

        public RSIAnalyzer(IIndicatorService<RSIResult> rsiService)
        {
            _rsiService = rsiService;
        }

        public async Task<SignalEvaluationResult> AnalyzeAsync(string symbol, string interval)
        {
            var rsi = await _rsiService.GetLatestAsync(symbol, interval, 14);

            var result = new SignalEvaluationResult
            {
                Symbol = symbol,
                Interval = interval,
                EvaluationTime = DateTime.UtcNow,
                IndicatorValues = new Dictionary<string, decimal>(),
                SignalDescriptions = new List<string>(),
                ProposedAction = TradeAction.Hold,
                ConfidenceScore = 0.5m
            };

            if (rsi != null)
            {
                result.IndicatorValues["RSI14"] = rsi.Value;

                if (rsi.Value < 30)
                {
                    result.ProposedAction = TradeAction.Buy;
                    result.SignalDescriptions.Add("RSI oversold");
                    result.Reason = "RSI onder 30 — mogelijk koopmoment.";
                    result.ConfidenceScore = 0.8m;
                }
                else if (rsi.Value > 70)
                {
                    result.ProposedAction = TradeAction.Sell;
                    result.SignalDescriptions.Add("RSI overbought");
                    result.Reason = "RSI boven 70 — mogelijk verkoopmoment.";
                    result.ConfidenceScore = 0.8m;
                }
            }

            return result;
        }
    }
}
