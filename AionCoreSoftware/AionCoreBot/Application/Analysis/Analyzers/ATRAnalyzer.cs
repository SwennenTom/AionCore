using AionCoreBot.Application.Analysis.Interfaces;
using AionCoreBot.Domain.Enums;
using AionCoreBot.Domain.Models;
using AionCoreBot.Helpers.Converters;
using AionCoreBot.Infrastructure.Interfaces;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AionCoreBot.Application.Analysis.Analyzers
{
    public class ATRAnalyzer : IAnalyzer
    {
        private readonly IIndicatorService<ATRResult> _atrService;
        private readonly ICandleRepository _candleRepository;  // voor volume data
        private readonly IConfiguration _configuration;

        private readonly decimal _changeFactorUpperThreshold;
        private readonly decimal _changeFactorIncrease;
        private readonly decimal _changeFactorDecrease;

        private readonly decimal _volumeRatioHigh;
        private readonly decimal _volumeFactorHigh;
        private readonly decimal _volumeRatioMedium;
        private readonly decimal _volumeFactorMedium;
        private readonly decimal _volumeRatioLow;
        private readonly decimal _volumeFactorLow;
        private readonly decimal _volumeFactorVeryLow;

        public ATRAnalyzer(
            IIndicatorService<ATRResult> atrService,
            ICandleRepository candleRepository,
            IConfiguration configuration)
        {
            _atrService = atrService;
            _candleRepository = candleRepository;
            _configuration = configuration;

            _changeFactorUpperThreshold = _configuration.GetValue("IndicatorParameters:ATR:ChangeFactorUpperThreshold", 0.2m);
            _changeFactorIncrease = _configuration.GetValue("IndicatorParameters:ATR:ChangeFactorIncrease", 1.1m);
            _changeFactorDecrease = _configuration.GetValue("IndicatorParameters:ATR:ChangeFactorDecrease", 0.9m);

            _volumeRatioHigh = _configuration.GetValue("IndicatorParameters:ATR:VolumeRatioHigh", 1.5m);
            _volumeFactorHigh = _configuration.GetValue("IndicatorParameters:ATR:VolumeFactorHigh", 1.15m);
            _volumeRatioMedium = _configuration.GetValue("IndicatorParameters:ATR:VolumeRatioMedium", 1.0m);
            _volumeFactorMedium = _configuration.GetValue("IndicatorParameters:ATR:VolumeFactorMedium", 1.0m);
            _volumeRatioLow = _configuration.GetValue("IndicatorParameters:ATR:VolumeRatioLow", 0.7m);
            _volumeFactorLow = _configuration.GetValue("IndicatorParameters:ATR:VolumeFactorLow", 0.85m);
            _volumeFactorVeryLow = _configuration.GetValue("IndicatorParameters:ATR:VolumeFactorVeryLow", 0.7m);
        }

        public async Task<SignalEvaluationResult> AnalyzeAsync(string symbol, string interval)
        {
            return await AnalyzeAsync(symbol, interval, DateTime.UtcNow);
        }

        public async Task<SignalEvaluationResult> AnalyzeAsync(string symbol, string interval, DateTime evaluationTime)
        {
            int period = _configuration.GetValue("IndicatorParameters:ATR:Period", 14);
            decimal thresholdPercent = _configuration.GetValue("IndicatorParameters:ATR:Threshold", 3.0m) / 100m;
            decimal lowerBound = thresholdPercent / _configuration.GetValue("IndicatorParameters:ATR:LowerBoundFactor", 10);
            evaluationTime = evaluationTime.AlignToInterval(interval);

            // Huidige ATR
            var atr = await _atrService.GetAsync(symbol, interval, evaluationTime, period);
            if (atr == null || atr.ClosePrice <= 0)
            {
                // Onvoldoende data
                if (atr == null || atr.ClosePrice <= 0)
                {
                    return null; // Geen geldige data, dus skip
                }

            }

            // Vorige ATR (van vorige interval)
            var previousEvaluationTime = evaluationTime.SubtractInterval(interval);
            var previousAtr = await _atrService.GetAsync(symbol, interval, previousEvaluationTime, period);

            decimal atrChangePercent = 0m;
            if (previousAtr != null && previousAtr.Value > 0)
            {
                atrChangePercent = (atr.Value - previousAtr.Value) / previousAtr.Value;
            }

            // Volume data ophalen uit candles
            // Huidige candle volume:
            var currentCandle = (await _candleRepository.GetBySymbolAndIntervalAsync(symbol, interval))
                .Where(c => c.CloseTime == evaluationTime)
                .FirstOrDefault();

            decimal currentVolume = currentCandle?.Volume ?? 0m;

            // Gemiddelde volume over de periode (period candles vóór evaluationTime)
            var candlesForVolume = (await _candleRepository.GetBySymbolAndIntervalAsync(symbol, interval))
                .Where(c => c.CloseTime < evaluationTime)
                .OrderByDescending(c => c.CloseTime)
                .Take(period)
                .ToList();

            decimal averageVolume = candlesForVolume.Any()
                ? candlesForVolume.Average(c => c.Volume)
                : 0m;

            var percentage = atr.Value / atr.ClosePrice;

            var result = new SignalEvaluationResult
            {
                Symbol = symbol,
                Interval = interval,
                EvaluationTime = evaluationTime,
                IndicatorValues = new Dictionary<string, decimal> { [$"ATR{period}"] = atr.Value },
                SignalDescriptions = new List<string>(),
                ProposedAction = TradeAction.Hold
            };

            result.ConfidenceScore = CalculateConfidenceScore(
                percentage: percentage,
                thresholdPercent: thresholdPercent,
                lowerBound: lowerBound,
                atrChangePercent: atrChangePercent,
                currentVolume: currentVolume,
                averageVolume: averageVolume,
                out TradeAction action,
                out string reason,
                out string description);

            result.ProposedAction = action;
            result.Reason = reason;
            result.SignalDescriptions.Add(description);

            return result;
        }

        private decimal CalculateConfidenceScore(
    decimal percentage,
    decimal thresholdPercent,
    decimal lowerBound,
    decimal atrChangePercent,
    decimal currentVolume,
    decimal averageVolume,
    out TradeAction proposedAction,
    out string reason,
    out string signalDescription)
        {
            decimal maxDistance = thresholdPercent - lowerBound;

            decimal baseConfidence;
            if (percentage > thresholdPercent)
            {
                proposedAction = TradeAction.Hold;
                signalDescription = $"ATR: high volatility ({percentage:P1})";
                reason = $"ATR > {thresholdPercent:P0} of price — avoid buying";

                var overshoot = percentage - thresholdPercent;
                var normalized = 1m - Math.Min(overshoot / thresholdPercent, 1m);
                baseConfidence = 0.4m * normalized;
            }
            else if (percentage < lowerBound)
            {
                proposedAction = TradeAction.Hold;
                signalDescription = $"ATR: too low volatility ({percentage:P1})";
                reason = $"ATR < {lowerBound:P0} of price — no market movement";

                var overshoot = lowerBound - percentage;
                var normalized = 1m - Math.Min(overshoot / lowerBound, 1m);
                baseConfidence = 0.4m * normalized;
            }
            else
            {
                proposedAction = TradeAction.Buy;
                signalDescription = $"ATR: normal volatility ({percentage:P1})";
                reason = $"ATR within acceptable range";

                var distanceFromEdges = Math.Min(percentage - lowerBound, thresholdPercent - percentage);
                var normalized = distanceFromEdges / (maxDistance / 2m);
                baseConfidence = 0.6m + 0.4m * normalized;
            }

            // ATR verandering factor - uit configuratie
            decimal changeFactor = 1.0m;
            if (atrChangePercent > _changeFactorUpperThreshold)
                changeFactor = _changeFactorIncrease;
            else if (atrChangePercent < -_changeFactorUpperThreshold)
                changeFactor = _changeFactorDecrease;

            // Volume factor - uit configuratie
            decimal volumeRatio = averageVolume > 0 ? currentVolume / averageVolume : 1m;
            decimal volumeFactor;

            if (volumeRatio >= _volumeRatioHigh)
                volumeFactor = _volumeFactorHigh;
            else if (volumeRatio >= _volumeRatioMedium)
                volumeFactor = _volumeFactorMedium;
            else if (volumeRatio >= _volumeRatioLow)
                volumeFactor = _volumeFactorLow;
            else
                volumeFactor = _volumeFactorVeryLow;

            decimal combinedConfidence = baseConfidence * changeFactor * volumeFactor;

            return Math.Min(combinedConfidence, 1.0m);
        }

    }
}
