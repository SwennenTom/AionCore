using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AionCoreBot.Infrastructure.Interfaces
{
    public interface IIndicatorRepository<T> where T : class
    {
        Task<T?> GetByIdAsync(int id);

        // Get all indicator records
        Task<IEnumerable<T>> GetAllAsync();

        // Get the latest indicator record for a symbol and interval
        Task<IEnumerable<T>> GetLatestBySymbolAndIntervalAsync(string symbol, string interval, int count = 1);

        Task AddAsync(T entity);
        //void Update(T entity);
        Task ClearAllAsync();
        Task SaveChangesAsync();
    }
}
