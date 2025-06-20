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
    public class SignalEvaluatorService : ISignalEvaluatorService
    {
        private readonly IEnumerable<IAnalyzer> _analyzers;

        public SignalEvaluatorService(IEnumerable<IAnalyzer> analyzers)
        {
            _analyzers = analyzers;
        }

        public async Task<List<SignalEvaluationResult>> EvaluateSignalsAsync(string symbol, string interval)
        {
            var results = new List<SignalEvaluationResult>();

            foreach (var analyzer in _analyzers)
            {
                var partial = await analyzer.AnalyzeAsync(symbol, interval);
                partial.Symbol = symbol;
                partial.Interval = interval;
                partial.EvaluationTime = DateTime.UtcNow;
                partial.AnalyzerName = analyzer.GetType().Name;

                results.Add(partial);
            }

            return results;
        }
    }


}
