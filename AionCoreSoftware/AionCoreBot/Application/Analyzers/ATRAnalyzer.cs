using AionCoreBot.Application.Interfaces;
using AionCoreBot.Domain.Enums;
using AionCoreBot.Domain.Interfaces;
using AionCoreBot.Domain.Models;

namespace AionCoreBot.Application.Analyzers
{
    public class ATRAnalyzer : IAnalyzer
    {
        private readonly IIndicatorService<ATRResult> _atrService;

        public ATRAnalyzer(IIndicatorService<ATRResult> atrService)
        {
            _atrService = atrService;
        }

        public async Task<SignalEvaluationResult> AnalyzeAsync(string symbol, string interval)
        {
            var atr = await _atrService.GetLatestAsync(symbol, interval, 14);

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

            if (atr != null)
            {
                result.IndicatorValues["ATR14"] = atr.Value;

                if (atr.ClosePrice > 0)
                {
                    var percentage = atr.Value / atr.ClosePrice;

                    if (percentage > 0.03m)
                    {
                        result.SignalDescriptions.Add("ATR wijst op hoge volatiliteit");
                        result.Reason = "ATR hoger dan 3% van prijs — markt is volatiel.";
                    }
                }
            }

            return result;
        }
    }
}
