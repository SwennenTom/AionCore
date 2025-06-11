using AionCoreBot.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AionCoreBot.Application.Interfaces
{
    public interface IRSIService
    {
        Task<RSIResult> CalculateRSIAsync(string symbol, string interval, int period, DateTime? startTime = null, DateTime? endTime = null);
        Task SaveRSIResultAsync(RSIResult rsiResult);
        Task<RSIResult?> GetRSIAsync(string symbol, string interval, DateTime timestamp, int period);
        Task<RSIResult?> GetLatestRSIAsync(string symbol, string interval, int period);
        Task<IEnumerable<RSIResult>> GetRSIHistoryAsync(string symbol, string interval, int period, DateTime from, DateTime to);
    }
}
