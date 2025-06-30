using AionCoreBot.Application.Interfaces.IAnalyzers;
using AionCoreBot.Infrastructure.Interfaces;
using AionCoreBot.Domain.Enums;
using AionCoreBot.Domain.Models;
using Microsoft.Extensions.Configuration;

namespace AionCoreBot.Application.Analyzers
{
    public class EMAAnalyzer : IAnalyzer
    {
        private readonly IIndicatorService<EMAResult> _emaService;
        private readonly IConfiguration _configuration;
        private readonly ISignalEvaluationRepository _signalRepository;

        public EMAAnalyzer(IIndicatorService<EMAResult> emaService, IConfiguration configuration, ISignalEvaluationRepository signalRepository)
        {
            _emaService = emaService;
            _configuration = configuration;
            _signalRepository = signalRepository;
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

                result.ConfidenceScore = await CalculateConfidenceAsync(symbol, interval, evaluationtime, emaShort.Value, emaMedium.Value);
            }
            else
            {
                Console.WriteLine($"[WARN] EMAAnalyzer: insufficient EMA data for {symbol} - {interval} at {evaluationtime}");
            }

            return result;
        }

        private async Task<decimal> CalculateConfidenceAsync(string symbol, string interval, DateTime evaluationTime, decimal shortEma, decimal mediumEma)
        {
            if (mediumEma == 0) return 0.0m;

            var emaRatio = (shortEma - mediumEma) / mediumEma;
            var upperLimit = _configuration.GetValue<decimal>("IndicatorParameters:EMA:EMARatioUpperThreshold", 0.05m);
            var lowerThreshold = _configuration.GetValue<decimal>("IndicatorParameters:EMA:EMARatioLowerThreshold", 0.005m);

            if (emaRatio <= 0 || emaRatio <= lowerThreshold)
                return 0.0m;

            // Baseline score (exponentieel zoals vroeger)
            var normalized = (emaRatio - lowerThreshold) / (upperLimit - lowerThreshold);
            var score = (decimal)Math.Pow((double)normalized, 1.5);

            // ✅ Bonus 1: EMA7 > EMA50 → extra bevestiging
            int longPeriod = _configuration.GetValue<int>("IndicatorParameters:EMA:LongPeriod", 50);
            var emaLong = await _emaService.GetAsync(symbol, interval, evaluationTime, longPeriod);

            if (emaLong != null && shortEma > emaLong.Value)
                score += 0.2m; // boost confidence

            // ✅ Bonus 2: Vorige signalen ook positief?
            var recentSignals = await _signalRepository.GetBySymbolAndIntervalAsync(symbol, interval);
            var previousBuys = recentSignals
                .Where(s => s.EvaluationTime < evaluationTime)
                .OrderByDescending(s => s.EvaluationTime)
                .Take(3)
                .All(s => s.ProposedAction == TradeAction.Buy);

            if (previousBuys)
                score += 0.1m;

            // Clamp to max 1.0
            return Math.Min(score, 1.0m);
        }

    }
}
