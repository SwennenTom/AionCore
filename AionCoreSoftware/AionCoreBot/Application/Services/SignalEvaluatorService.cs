using AionCoreBot.Application.Interfaces;
using AionCoreBot.Domain.Enums;
using AionCoreBot.Domain.Models;
using AionCoreBot.Worker.Indicators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AionCoreBot.Application.Services
{
    public class SignalEvaluatorService:ISignalEvaluatorService
    {
        private readonly IIndicatorService<EMAResult> _emaService;
        private readonly IIndicatorService<RSIResult> _rsiService;
        private readonly IIndicatorService<ATRResult> _atrService;

        public SignalEvaluatorService(
            IIndicatorService<EMAResult> emaService,
            IIndicatorService<RSIResult> rsiService,
            IIndicatorService<ATRResult> atrService)
        {
            _emaService = emaService ?? throw new ArgumentNullException(nameof(emaService));
            _rsiService = rsiService ?? throw new ArgumentNullException(nameof(rsiService));
            _atrService = atrService ?? throw new ArgumentNullException(nameof(atrService));
        }
        public async Task<SignalEvaluationResult> EvaluateSignalsAsync(string symbol, string interval)
        {
            var result = new SignalEvaluationResult
            {
                Symbol = symbol,
                Interval = interval,
                EvaluationTime = DateTime.UtcNow,
                ProposedAction = TradeAction.Hold,
                IndicatorValues = new Dictionary<string, decimal>(),
                SignalDescriptions = new List<string>()
            };

            // EMA's ophalen
            var ema7 = await _emaService.GetLatestAsync(symbol, interval, 7);
            var ema21 = await _emaService.GetLatestAsync(symbol, interval, 21);
            var ema50 = await _emaService.GetLatestAsync(symbol, interval, 50);

            if (ema7 != null) result.IndicatorValues["EMA7"] = ema7.Value;
            if (ema21 != null) result.IndicatorValues["EMA21"] = ema21.Value;
            if (ema50 != null) result.IndicatorValues["EMA50"] = ema50.Value;

            // RSI ophalen
            var rsi14 = await _rsiService.GetLatestAsync(symbol, interval, 14);
            if (rsi14 != null) result.IndicatorValues["RSI14"] = rsi14.Value;

            // ATR ophalen
            var atr14 = await _atrService.GetLatestAsync(symbol, interval, 14);
            if (atr14 != null) result.IndicatorValues["ATR14"] = atr14.Value;

            // Eenvoudige evaluatie voorbeelden
            if (ema7 != null && ema21 != null)
            {
                if (ema7.Value > ema21.Value)
                {
                    result.ProposedAction = TradeAction.Buy;
                    result.SignalDescriptions.Add("EMA7 above EMA21");
                    result.Reason = "Korte termijn trend is positief.";
                }
                else if (ema7.Value < ema21.Value)
                {
                    result.ProposedAction = TradeAction.Sell;
                    result.SignalDescriptions.Add("EMA7 below EMA21");
                    result.Reason = "Korte termijn trend is negatief.";
                }
            }

            // RSI check
            if (rsi14 != null)
            {
                if (rsi14.Value < 30)
                {
                    result.SignalDescriptions.Add("RSI oversold");
                    result.ConfidenceScore = 0.8m;
                    if (result.ProposedAction == TradeAction.Hold)
                    {
                        result.ProposedAction = TradeAction.Buy;
                        result.Reason = "RSI wijst op oversold conditie.";
                    }
                }
                else if (rsi14.Value > 70)
                {
                    result.SignalDescriptions.Add("RSI overbought");
                    result.ConfidenceScore = 0.8m;
                    if (result.ProposedAction == TradeAction.Hold)
                    {
                        result.ProposedAction = TradeAction.Sell;
                        result.Reason = "RSI wijst op overbought conditie.";
                    }
                }
            }

            return result;
        }

    }
}
