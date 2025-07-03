using AionCoreBot.Application.Strategy.Interfaces;
using AionCoreBot.Infrastructure.Interfaces;
using AionCoreBot.Infrastructure.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AionCoreBot.Application.Strategy.Services
{
    public class StrategyService : IStrategyService
    {
        private readonly ISignalEvaluationRepository _signalRepository;
        private readonly IStrategizer _strategizer;

        public StrategyService(ISignalEvaluationRepository signalRepository, IStrategizer strategizer)
        {
            _signalRepository = signalRepository;
            _strategizer = strategizer;
        }

        public async Task ExecuteStrategyAsync(CancellationToken cancellationToken = default)
        {
            var signals = await _signalRepository.GetLatestSignalsAsync(); // dit moet gegroepeerd kunnen worden

            var grouped = signals
                .GroupBy(s => (s.Symbol, s.Interval))
                .ToList();

            foreach (var group in grouped)
            {
                var decision = await _strategizer.DecideTradeAsync(group.ToList(), cancellationToken);

                Console.WriteLine($"[STRATEGY] Beslissing voor {group.Key.Symbol} ({group.Key.Interval}): {decision.Action}");
                Console.WriteLine(decision.Reason);

                // TODO: opslaan, versturen of verwerken van de beslissing
            }
        }

    }

}
