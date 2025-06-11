using AionCoreBot.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AionCoreBot.Application.Interfaces
{
    public interface IATRService
    {
        Task<ATRResult> CalculateATRAsync(string symbol, string interval, int period, DateTime? startTime = null, DateTime? endTime = null);
        Task SaveATRResultAsync(ATRResult atrResult);
        Task<ATRResult?> GetATRAsync(string symbol, string interval, DateTime timestamp, int period);
        Task<ATRResult?> GetLatestATRAsync(string symbol, string interval, int period);
        Task<IEnumerable<ATRResult>> GetATRHistoryAsync(string symbol, string interval, int period, DateTime from, DateTime to);
    }
}
