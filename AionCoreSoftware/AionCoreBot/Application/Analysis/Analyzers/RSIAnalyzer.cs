using AionCoreBot.Application.Analysis.Interfaces;
using AionCoreBot.Domain.Enums;
using AionCoreBot.Domain.Models;
using AionCoreBot.Infrastructure.Interfaces;
using Microsoft.Extensions.Configuration;

namespace AionCoreBot.Application.Analysis.Analyzers
{
    public class RSIAnalyzer : IAnalyzer
    {
        private readonly IIndicatorService<RSIResult> _rsiService;
        private readonly ICandleRepository _candleRepo;
        private readonly IConfiguration _cfg;

        /* ─ Config values (loaded once in ctor) ─ */
        private readonly int _period;
        private readonly int _overbought;
        private readonly int _oversold;

        private readonly decimal _edgeW;
        private readonly decimal _neutralW;
        private readonly decimal _momentumW;
        private readonly decimal _volumeW;
        private readonly decimal _volBoostStart;
        private readonly decimal _volBoostSpan;

        public RSIAnalyzer(
            IIndicatorService<RSIResult> rsiService,
            IConfiguration configuration,
            ICandleRepository candleRepository)
        {
            _rsiService = rsiService;
            _candleRepo = candleRepository;
            _cfg = configuration;

            /* ─ Load general RSI params ─ */
            _period = _cfg.GetValue<int>("IndicatorParameters:RSI:Period", 14);
            _overbought = _cfg.GetValue<int>("IndicatorParameters:RSI:OverboughtThreshold", 70);
            _oversold = _cfg.GetValue<int>("IndicatorParameters:RSI:OversoldThreshold", 30);

            /* ─ Load confidence weights ─ */
            var path = "IndicatorParameters:RSI:ConfidenceWeights";
            _edgeW = _cfg.GetValue<decimal>($"{path}:EdgeWeight", 0.85m);
            _neutralW = _cfg.GetValue<decimal>($"{path}:NeutralWeight", 0.25m);
            _momentumW = _cfg.GetValue<decimal>($"{path}:MomentumWeight", 0.10m);
            _volumeW = _cfg.GetValue<decimal>($"{path}:VolumeWeight", 0.10m);
            _volBoostStart = _cfg.GetValue<decimal>($"{path}:VolumeBoostStart", 1.20m);
            _volBoostSpan = _cfg.GetValue<decimal>($"{path}:VolumeBoostSpan", 0.80m);
        }

        public Task<SignalEvaluationResult> AnalyzeAsync(string symbol, string interval)
            => AnalyzeAsync(symbol, interval, DateTime.UtcNow);

        public async Task<SignalEvaluationResult?> AnalyzeAsync(
            string symbol, string interval, DateTime evalTimeUtc)
        {
            evalTimeUtc = evalTimeUtc.AddSeconds(-evalTimeUtc.Second)
                                     .AddMilliseconds(-evalTimeUtc.Millisecond);

            /* ─ fetch RSI current & previous ─ */
            var rsi = await _rsiService.GetAsync(symbol, interval, evalTimeUtc, _period);
            var prevRsi = await _rsiService.GetAsync(symbol, interval, evalTimeUtc.AddMinutes(-1), _period);

            if (rsi == null) return null;              // skip: no data

            /* ─ volume info ─ */
            var candles = (await _candleRepo.GetBySymbolAndIntervalAsync(symbol, interval)).ToList();
            var curCandle = candles.FirstOrDefault(c => c.CloseTime == evalTimeUtc);
            var last20Vol = candles.Where(c => c.CloseTime < evalTimeUtc)
                                     .OrderByDescending(c => c.CloseTime)
                                     .Take(20);
            decimal? curVol = curCandle?.Volume;
            decimal? avgVol = last20Vol.Any() ? last20Vol.Average(c => c.Volume) : null;

            var result = new SignalEvaluationResult
            {
                Symbol = symbol,
                Interval = interval,
                EvaluationTime = evalTimeUtc,
                AnalyzerName = GetType().Name,
                IndicatorValues = new() { [$"RSI{_period}"] = rsi.Value },
                SignalDescriptions = new(),
                ProposedAction = DetermineAction(rsi.Value, out var reason),
                Reason = reason
            };

            result.ConfidenceScore = CalcConfidence(
                rsiValue: rsi.Value,
                previousRsi: prevRsi?.Value,
                currentVol: curVol,
                averageVol: avgVol);

            return result;
        }

        /*──────────────── helpers ────────────────*/

        private TradeAction DetermineAction(decimal value, out string reason)
        {
            if (value < _oversold)
            {
                reason = $"RSI below {_oversold}";
                return TradeAction.Buy;
            }
            if (value > _overbought)
            {
                reason = $"RSI above {_overbought}";
                return TradeAction.Sell;
            }

            reason = "RSI neutral";
            return TradeAction.Hold;
        }

        private decimal CalcConfidence(decimal rsiValue,
                                       decimal? previousRsi,
                                       decimal? currentVol,
                                       decimal? averageVol)
        {
            decimal conf = 0m;

            /* 1️⃣  RSI-afstand */
            if (rsiValue < _oversold)
            {
                var ratio = (_oversold - rsiValue) / _oversold;
                conf += _edgeW * ratio;
            }
            else if (rsiValue > _overbought)
            {
                var ratio = (rsiValue - _overbought) / (100m - _overbought);
                conf += _edgeW * ratio;
            }
            else
            {
                var neutral = Math.Abs(rsiValue - 50m) / 20m;
                conf += _neutralW * neutral;
            }

            /* 2️⃣  Momentum (alleen positief) */
            if (previousRsi.HasValue)
            {
                var momentum = Math.Max(0m, (rsiValue - previousRsi.Value) / 15m);
                conf += _momentumW * momentum;
            }

            /* 3️⃣  Volume-boost */
            if (currentVol.HasValue && averageVol.HasValue && averageVol.Value > 0m)
            {
                var ratio = currentVol.Value / averageVol.Value;
                if (ratio >= _volBoostStart)
                {
                    var boost = Math.Clamp((ratio - _volBoostStart) / _volBoostSpan, 0m, 1m);
                    conf += _volumeW * boost;
                }
            }

            return Math.Clamp(conf, 0m, 1m);
        }
    }
}
