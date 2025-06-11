using AionCoreBot.Application.Interfaces;
using AionCoreBot.Domain.Enums;
using AionCoreBot.Domain.Interfaces;
using AionCoreBot.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AionCoreBot.Application.Analyzers
{
    public class EMAAnalyzer : IAnalyzer
    {
        private readonly IIndicatorService<EMAResult> _emaService;

        public EMAAnalyzer(IIndicatorService<EMAResult> emaService)
        {
            _emaService = emaService;
        }

        public async Task<SignalEvaluationResult> AnalyzeAsync(string symbol, string interval)
        {
            var result = new SignalEvaluationResult
            {
                Symbol = symbol,
                Interval = interval,
                EvaluationTime = DateTime.UtcNow,
                ProposedAction = TradeAction.Hold,
                IndicatorValues = new(),
                SignalDescriptions = new()
            };

            var ema7 = await _emaService.GetLatestAsync(symbol, interval, 7);
            var ema21 = await _emaService.GetLatestAsync(symbol, interval, 21);

            if (ema7 != null && ema21 != null)
            {
                result.IndicatorValues["EMA7"] = ema7.Value;
                result.IndicatorValues["EMA21"] = ema21.Value;

                if (ema7.Value > ema21.Value)
                {
                    result.ProposedAction = TradeAction.Buy;
                    result.SignalDescriptions.Add("EMA7 boven EMA21");
                }
                else if (ema7.Value < ema21.Value)
                {
                    result.ProposedAction = TradeAction.Sell;
                    result.SignalDescriptions.Add("EMA7 onder EMA21");
                }
            }

            return result;
        }
    }

}
