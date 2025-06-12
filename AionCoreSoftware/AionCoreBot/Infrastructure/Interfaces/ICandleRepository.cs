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
        Task<Candle?> GetByIdAsync(int id);
        Task<IEnumerable<Candle>> GetAllAsync();
        Task<IEnumerable<Candle>> GetBySymbolAsync(string symbol);
        Task AddAsync(Candle entity);
        //void Update(Candle entity);
        void Delete(Candle entity);
        Task SaveChangesAsync();
    }
}
