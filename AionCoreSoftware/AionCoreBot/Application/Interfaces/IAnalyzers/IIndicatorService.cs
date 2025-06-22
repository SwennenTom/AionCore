using AionCoreBot.Domain.Interfaces;
using AionCoreBot.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AionCoreBot.Application.Interfaces.IAnalyzers
{
    public interface IIndicatorService<TResult> where TResult : IIndicatorResult
    {
        Task<TResult> CalculateAsync(string symbol, string interval, int period, DateTime? startTime = null, DateTime? endTime = null);
        Task SaveResultAsync(TResult result);
        Task<TResult?> GetAsync(string symbol, string interval, DateTime timestamp, int period);
        Task<TResult?> GetLatestAsync(string symbol, string interval, int period);
        Task<TResult?> GetLatestBeforeAsync(string symbol, string interval, int period, DateTime time);
        Task<IEnumerable<TResult>> GetHistoryAsync(string symbol, string interval, int period, DateTime from, DateTime to);
        Task ClearAllAsync();

    }
}
