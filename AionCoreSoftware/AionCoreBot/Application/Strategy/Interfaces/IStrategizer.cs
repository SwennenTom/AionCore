using AionCoreBot.Domain.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AionCoreBot.Application.Strategy.Interfaces
{
    public interface IStrategizer
    {
        Task<TradeDecision> DecideTradeAsync(List<SignalEvaluationResult> signals, CancellationToken cancellationToken = default);
    }
}
