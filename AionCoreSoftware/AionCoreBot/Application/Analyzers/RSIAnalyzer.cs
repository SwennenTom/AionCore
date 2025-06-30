using AionCoreBot.Application.Interfaces.IAnalyzers;
using AionCoreBot.Domain.Enums;
using AionCoreBot.Domain.Models;
using AionCoreBot.Infrastructure.Interfaces;
using Microsoft.Extensions.Configuration;

namespace AionCoreBot.Application.Analyzers
{
    public class RSIAnalyzer : IAnalyzer
    {
        private readonly IIndicatorService<RSIResult> _rsiService;
        private readonly IConfiguration _configuration;
        private readonly ICandleRepository _candleRepository;

        public RSIAnalyzer(IIndicatorService<RSIResult> rsiService, IConfiguration configuration, ICandleRepository candleRepository)
        {
            _rsiService = rsiService;
            _configuration = configuration;
            _candleRepository = candleRepository;
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
            evaluationtime = evaluationtime.AddSeconds(-evaluationtime.Second).AddMilliseconds(-evaluationtime.Millisecond);

            var rsi = await _rsiService.GetAsync(symbol, interval, evaluationtime, period);
            var previousRsi = await _rsiService.GetAsync(symbol, interval, evaluationtime.AddMinutes(-1), period);

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

                // Volume uit candles ophalen
                var candles = (await _candleRepository.GetBySymbolAndIntervalAsync(symbol, interval)).ToList();

                var currentCandle = candles.FirstOrDefault(c => c.CloseTime == evaluationtime);
                var previousVolumes = candles
                    .Where(c => c.CloseTime <= evaluationtime)
                    .OrderByDescending(c => c.CloseTime)
                    .Take(20)
                    .ToList();

                decimal? currentVolume = currentCandle?.Volume;
                decimal? averageVolume = previousVolumes.Count > 0 ? previousVolumes.Average(c => c.Volume) : null;

                result.ProposedAction = DetermineAction(rsi.Value, overbought, oversold, result.SignalDescriptions, out string reason);
                result.Reason = reason;

                result.ConfidenceScore = CalculateConfidenceScore(
                    rsiValue: rsi.Value,
                    overbought: overbought,
                    oversold: oversold,
                    previousRsi: previousRsi?.Value,
                    currentVolume: currentVolume,
                    averageVolume: averageVolume
                );
            }

            return result;
        }

        private TradeAction DetermineAction(decimal rsiValue, int overbought, int oversold, List<string> descriptions, out string reason)
        {
            if (rsiValue < oversold)
            {
                descriptions.Add("RSI oversold");
                reason = $"RSI under {oversold}";
                return TradeAction.Buy;
            }
            else if (rsiValue > overbought)
            {
                descriptions.Add("RSI overbought");
                reason = $"RSI over {overbought}";
                return TradeAction.Sell;
            }
            else
            {
                descriptions.Add("RSI neutral");
                reason = "RSI in neutral zone";
                return TradeAction.Hold;
            }
        }

        private decimal CalculateConfidenceScore(
            decimal rsiValue,
            int overbought,
            int oversold,
            decimal? previousRsi,
            decimal? currentVolume,
            decimal? averageVolume)
        {
            decimal confidence = 0m;

            // 1. RSI afstand
            if (rsiValue < oversold)
            {
                var dist = oversold - rsiValue;
                var norm = Math.Min((decimal)Math.Pow((double)(dist / oversold), 1.5), 1m);
                confidence += 0.5m * norm;
            }
            else if (rsiValue > overbought)
            {
                var dist = rsiValue - overbought;
                var norm = Math.Min(dist / (100 - overbought), 1m);
                confidence += 0.5m * norm;
            }
            else
            {
                var distTo50 = Math.Abs(rsiValue - 50);
                var norm = distTo50 / 20m;
                confidence += 0.3m * norm;
            }

            // 2. Momentum (RSI stijging of daling)
            if (previousRsi.HasValue)
            {
                var momentum = rsiValue - previousRsi.Value;
                confidence += 0.2m * Math.Clamp(momentum / 10m, -1m, 1m);
            }

            // 3. Volume
            if (currentVolume.HasValue && averageVolume.HasValue && averageVolume.Value > 0)
            {
                var ratio = currentVolume.Value / averageVolume.Value;
                var volumeBoost = Math.Clamp(ratio - 1, 0, 1); // enkel boost bij hoger dan gemiddeld
                confidence += 0.3m * volumeBoost;
            }

            return Math.Clamp(confidence, 0m, 1m);
        }


    }
}
