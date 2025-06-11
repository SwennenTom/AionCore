using AionCoreBot.Application.Interfaces;
using AionCoreBot.Domain.Enums;
using AionCoreBot.Domain.Interfaces;
using AionCoreBot.Domain.Models;
using AionCoreBot.Worker.Indicators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AionCoreBot.Application.Services
{
    public class SignalEvaluatorService : ISignalEvaluatorService
    {
        private readonly IEnumerable<IAnalyzer> _analyzers;

        public SignalEvaluatorService(IEnumerable<IAnalyzer> analyzers)
        {
            _analyzers = analyzers;
        }

        public async Task<SignalEvaluationResult> EvaluateSignalsAsync(string symbol, string interval)
        {
            var result = new SignalEvaluationResult
            {
                Symbol = symbol,
                Interval = interval,
                EvaluationTime = DateTime.UtcNow,
                IndicatorValues = new(),
                SignalDescriptions = new(),
                ProposedAction = TradeAction.Hold
            };

            foreach (var analyzer in _analyzers)
            {
                var partial = await analyzer.AnalyzeAsync(symbol, interval);

                // Voeg samen
                result.SignalDescriptions.AddRange(partial.SignalDescriptions);
                foreach (var kv in partial.IndicatorValues)
                    result.IndicatorValues[kv.Key] = kv.Value;

                // Scores, acties, confidence kun je hier later ook wegen
            }

            return result;
        }
    }

}
