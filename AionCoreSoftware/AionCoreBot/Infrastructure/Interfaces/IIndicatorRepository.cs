using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AionCoreBot.Infrastructure.Interfaces
{
    public interface IIndicatorRepository<T> where T : class
    {
        Task<T?> GetByIdAsync(int id);

        Task<IEnumerable<T>> GetAllAsync();

        Task<IEnumerable<T>> GetLatestNBySymbolAndIntervalAsync(string symbol, string interval, int count = 1);

        Task<T> GetLatestBySymbolIntervalPeriodAsync(string symbol, string interval, int period);

        Task<T?> GetLatestBeforeAsync(string symbol, string interval, int period, DateTime time);

        Task<T?> GetBySymbolIntervalTimestampPeriodAsync(string symbol, string interval, DateTime timestamp, int period);

        Task<IEnumerable<T>> GetByPeriodAndDateRangeAsync(string symbol, string interval, int period, DateTime from, DateTime to);

        Task AddAsync(T entity);

        Task ClearAllAsync();

        Task SaveChangesAsync();
    }
}
