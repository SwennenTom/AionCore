using AionCoreBot.Application.Interfaces.IAnalyzers;
using AionCoreBot.Domain.Enums;
using AionCoreBot.Domain.Models;
using Microsoft.Extensions.Configuration;
using System.Runtime;

namespace AionCoreBot.Application.Analyzers
{
    public class EMAAnalyzer : IAnalyzer
    {
        private readonly IIndicatorService<EMAResult> _emaService;
        private readonly IConfiguration _configuration;

        public EMAAnalyzer(IIndicatorService<EMAResult> emaService, IConfiguration configuration)
        {
            _emaService = emaService;
            _configuration = configuration;
        }                
        public async Task<SignalEvaluationResult> AnalyzeAsync(string symbol, string interval)
        {
            return await AnalyzeAsync(symbol, interval, DateTime.UtcNow);
        }

        public async Task<SignalEvaluationResult> AnalyzeAsync(string symbol, string interval, DateTime evaluationtime)
        {
            int shortPeriod = _configuration.GetValue<int>("IndicatorParameters:EMA:ShortPeriod", 7);
            int mediumPeriod = _configuration.GetValue<int>("IndicatorParameters:EMA:MediumPeriod", 21);
            evaluationtime = evaluationtime.AddSeconds(-evaluationtime.Second).AddMilliseconds(-evaluationtime.Millisecond);

            var result = new SignalEvaluationResult
            {
                Symbol = symbol,
                Interval = interval,
                EvaluationTime = evaluationtime,
                ProposedAction = TradeAction.Hold,
                IndicatorValues = new(),
                SignalDescriptions = new(),
                AnalyzerName = GetType().Name
            };

            var emaShort = await _emaService.GetAsync(symbol, interval, evaluationtime, shortPeriod);
            var emaMedium = await _emaService.GetAsync(symbol, interval, evaluationtime, mediumPeriod);

            if (emaShort != null && emaMedium != null && emaMedium.Value != 0)
            {
                result.IndicatorValues[$"EMA{shortPeriod}"] = emaShort.Value;
                result.IndicatorValues[$"EMA{mediumPeriod}"] = emaMedium.Value;

                if (emaShort.Value > emaMedium.Value)
                {
                    result.ProposedAction = TradeAction.Buy;
                    result.SignalDescriptions.Add($"EMA{shortPeriod} over EMA{mediumPeriod}");
                }
                else if (emaShort.Value < emaMedium.Value)
                {
                    result.ProposedAction = TradeAction.Sell;
                    result.SignalDescriptions.Add($"EMA{shortPeriod} under EMA{mediumPeriod}");
                }

                var EMARatio = ((emaShort.Value - emaMedium.Value) / emaMedium.Value) * 100m;
                var EMARatioUpperLimit = _configuration.GetValue<decimal>("IndicatorParameters:EMA:EMARatioUpperLimit", 5);

                if (EMARatio <= 0)
                {
                    result.ConfidenceScore = 0.0m;
                }
                else if (EMARatio >= EMARatioUpperLimit)
                {
                    result.ConfidenceScore = 1.0m;
                }
                else
                {
                    result.ConfidenceScore = EMARatio / EMARatioUpperLimit;
                }

            }
            else
            {
                // Optioneel: logging of waarschuwing
                Console.WriteLine($"[WARN] EMAAnalyzer: insufficient EMA data for {symbol} - {interval} at {evaluationtime}");
            }

            return result;
        }

    }
}