using AionCoreBot.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AionCoreBot.Application.Interfaces
{
    public interface IEMAService
    {
        Task<EMAResult> CalculateEMAAsync(string symbol, string interval, int period, DateTime? startTime = null, DateTime? endTime = null);
        Task SaveEMAResultAsync(EMAResult emaResult);
        Task<EMAResult?> GetEMAAsync(string symbol, string interval, DateTime timestamp, int period);
        Task<EMAResult?> GetLatestEMAAsync(string symbol, string interval, int period);
        Task<IEnumerable<EMAResult>> GetEMAHistoryAsync(string symbol, string interval, int period, DateTime from, DateTime to);
    }
}
