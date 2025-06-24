using AionCoreBot.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AionCoreBot.Application.Interfaces.IAnalyzers
{
    public interface IIndicatorService<TResult> where TResult : IIndicatorResult
    {
        Task<TResult> CalculateAsync(string symbol, string interval, int period, DateTime startTime, DateTime endTime);
        Task CalcAllAsync();

        Task<TResult?> GetAsync(string symbol, string interval, DateTime timestamp, int period);
        Task<TResult?> GetLatestAsync(string symbol, string interval, int period);
        Task<TResult?> GetLatestBeforeAsync(string symbol, string interval, int period, DateTime time);
        Task<IEnumerable<TResult>> GetHistoryAsync(string symbol, string interval, int period, DateTime from, DateTime to);
        Task SaveResultAsync(TResult result);
        Task ClearAllAsync();
    }
}
