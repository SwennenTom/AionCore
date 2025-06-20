using System;
using System.Threading.Tasks;

namespace AionCoreBot.Worker.Interfaces
{
    public interface IBaseIndicatorService<TResult>
    {
        Task<TResult> CalculateAsync(string symbol, string interval, int period, DateTime startTime, DateTime endTime);
        Task CalcAllAsync();
    }
}
