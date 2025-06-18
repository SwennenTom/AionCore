using System.Collections.Generic;
using System.ComponentModel;
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

        public async Task<IEnumerable<Candle>> GetAllAsync()
        {
            return await _context.Candles.ToListAsync();
        }

        public async Task<IEnumerable<Candle>> GetBySymbolAndIntervalAsync(string symbol, string interval)
        {
            return await _context.Candles
                .Where(c => c.Symbol == symbol)
                .Where(c => c.Interval == interval)
                .OrderBy(c => c.OpenTime)
                .ToListAsync();
        }
        public async Task<Candle?> GetLastCandleAsync(string symbol, string interval)
        {
            return await _context.Candles
                .Where(c => c.Symbol == symbol && c.Interval == interval)
                .OrderByDescending(c => c.OpenTime)
                .FirstOrDefaultAsync();
        }
        public async Task AddAsync(Candle entity)
        {
            await _context.Candles.AddAsync(entity);
        }
        public async Task AddRangeAsync(IEnumerable<Candle> entities)
        {
            await _context.Candles.AddRangeAsync(entities);
        }
        public async Task ClearAllAsync()
        {
            _context.Candles.RemoveRange(_context.Candles);
            await _context.SaveChangesAsync();
        }
        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
        public async Task<IEnumerable<Candle>> GetCandlesAsync(string symbol, string interval, DateTime startTime, DateTime endTime)
        {
            var candles = await _context.Candles
                .Where(c => c.Symbol == symbol
                         && c.Interval == interval
                         && c.OpenTime >= startTime
                         && c.CloseTime <= endTime)
                .OrderBy(c => c.OpenTime)
                .ToListAsync();

            return candles;
        }
        public async Task<bool> ExistsAsync(string symbol, string interval, DateTime openTime)
        {
            return await _context.Candles.AnyAsync(c =>
                c.Symbol == symbol &&
                c.Interval == interval &&
                c.OpenTime == openTime);
        }

    }
}
