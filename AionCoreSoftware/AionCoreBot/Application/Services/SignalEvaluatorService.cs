using AionCoreBot.Application.Interfaces;
using AionCoreBot.Application.Interfaces.IAnalyzers;
using AionCoreBot.Domain.Enums;
using AionCoreBot.Domain.Models;
using AionCoreBot.Infrastructure.Interfaces;
using AionCoreBot.Application.Indicators;
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
        private readonly ISignalEvaluationRepository _signalRepo;

        public SignalEvaluatorService(IEnumerable<IAnalyzer> analyzers, ISignalEvaluationRepository signalrepo)
        {
            _analyzers = analyzers;
            _signalRepo = signalrepo;
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
                partial.ConfidenceScore ??= 0.0m;

                results.Add(partial);
            }
            await _signalRepo.AddRangeAsync(results);
            await _signalRepo.SaveChangesAsync();

            return results;
        }
        public async Task<List<SignalEvaluationResult>> EvaluateHistoricalSignalsAsync(string symbol, string interval, List<DateTime> evaluationPoints)
        {
            var results = new List<SignalEvaluationResult>();

            foreach (var timestamp in evaluationPoints)
            {
                foreach (var analyzer in _analyzers)
                {
                    //Console.WriteLine($"[DEBUG] Evaluating {analyzer.GetType().Name} for {symbol} ({interval}) at {timestamp}");
                    var result = await analyzer.AnalyzeAsync(symbol, interval, timestamp);

                    if (result == null)
                        continue;

                    result.Symbol = symbol;
                    result.Interval = interval;
                    result.EvaluationTime = timestamp;
                    result.AnalyzerName = analyzer.GetType().Name;
                    result.ConfidenceScore ??= 0.0m;

                    results.Add(result);
                    
                    //Console.WriteLine($"[DEBUG] Result toegevoegd voor {symbol} ({interval}) op {timestamp} door {analyzer.GetType().Name}");
                }
            }
            Console.WriteLine($"[INIT] {results.Count} signalen geëvalueerd voor {symbol} ({interval})");

            await _signalRepo.AddRangeAsync(results);
            await _signalRepo.SaveChangesAsync();

            //Console.WriteLine("[DEBUG] SignalEvaluationResults saved!");


            return results;
        }

        public async Task<List<SignalEvaluationResult>> EvaluateAllAsync(string symbol, string interval, IEnumerable<Candle> candles)
        {
            var evaluationPoints = candles
                .Select(c => c.OpenTime)
                .Distinct()
                .OrderBy(t => t)
                .ToList();
            Console.WriteLine($"[INIT] {evaluationPoints.Count} unieke evaluatiepunten gevonden voor {symbol} ({interval})");
            return await EvaluateHistoricalSignalsAsync(symbol, interval, evaluationPoints);
        }


    }


}
