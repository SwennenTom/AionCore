using AionCoreBot.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AionCoreBot.Infrastructure.Interfaces
{
    public interface ISignalEvaluationRepository
    {
        Task AddAsync(SignalEvaluationResult result);
        Task AddRangeAsync(IEnumerable<SignalEvaluationResult> results);
        Task<IEnumerable<SignalEvaluationResult>> GetBySymbolAndIntervalAsync(string symbol, string interval);
        Task SaveChangesAsync();
        Task ClearAllAsync();
    }

}
