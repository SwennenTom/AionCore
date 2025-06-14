using AionCoreBot.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AionCoreBot.Infrastructure.Interfaces
{
    public interface ICandleRepository
    {
        Task<IEnumerable<Candle>> GetAllAsync();
        Task<IEnumerable<Candle>> GetBySymbolAndIntervalAsync(string symbol, string interval);
        Task<IEnumerable<Candle>> GetCandlesAsync(string symbol, string interval, DateTime startTime, DateTime endTime);
        Task AddAsync(Candle entity);
        void Delete(Candle entity);
        Task SaveChangesAsync();
    }
}
