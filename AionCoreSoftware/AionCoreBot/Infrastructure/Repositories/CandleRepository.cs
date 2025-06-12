using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AionCoreBot.Domain.Models;
using AionCoreBot.Infrastructure.Data;
using AionCoreBot.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AionCoreBot.Infrastructure.Repositories
{
    public class CandleRepository : ICandleRepository
    {
        private readonly ApplicationDbContext _context;

        public CandleRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        // Note: Candle doesn't have a single int Id, so this method returns null or throws
        public Task<Candle?> GetByIdAsync(int id)
        {
            // Because Candle has a composite key, this doesn't really make sense.
            // Returning null or you could throw a NotSupportedException here.
            return Task.FromResult<Candle?>(null);
        }

        public async Task<IEnumerable<Candle>> GetAllAsync()
        {
            return await _context.Candles.ToListAsync();
        }

        public async Task<IEnumerable<Candle>> GetBySymbolAsync(string symbol)
        {
            return await _context.Candles
                .Where(c => c.Symbol == symbol)
                .OrderBy(c => c.OpenTime)
                .ToListAsync();
        }

        public async Task AddAsync(Candle entity)
        {
            await _context.Candles.AddAsync(entity);
        }

        public void Delete(Candle entity)
        {
            _context.Candles.Remove(entity);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
