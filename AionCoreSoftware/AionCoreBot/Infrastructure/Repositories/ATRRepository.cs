using AionCoreBot.Infrastructure.Interfaces;
using AionCoreBot.Domain.Models;
using AionCoreBot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AionCoreBot.Infrastructure.Repositories
{
    public class ATRRepository : IIndicatorRepository<ATRResult>
    {
        private readonly ApplicationDbContext _context;

        public ATRRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ATRResult?> GetByIdAsync(int id)
        {
            return await _context.ATRResults.FindAsync(id);
        }

        public async Task<IEnumerable<ATRResult>> GetAllAsync()
        {
            return await _context.ATRResults.ToListAsync();
        }

        public async Task AddAsync(ATRResult entity)
        {
            await _context.ATRResults.AddAsync(entity);
        }

        public void Update(ATRResult entity)
        {
            _context.ATRResults.Update(entity);
        }

        public async Task ClearAllAsync()
        {
            _context.ATRResults.RemoveRange(_context.ATRResults);
            await _context.SaveChangesAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<ATRResult>> GetLatestNBySymbolAndIntervalAsync(string symbol, string interval, int count = 1)
        {
            return await _context.ATRResults
                .Where(r => r.Symbol == symbol && r.Interval == interval)
                .OrderByDescending(r => r.Timestamp)
                .Take(count)
                .ToListAsync();
        }

        public async Task<ATRResult?> GetLatestBySymbolIntervalPeriodAsync(string symbol, string interval, int period)
        {
            return await _context.ATRResults
                .Where(e => e.Symbol == symbol && e.Interval == interval && e.Period == period)
                .OrderByDescending(e => e.Timestamp)
                .FirstOrDefaultAsync();
        }

        public async Task<ATRResult?> GetLatestBeforeAsync(string symbol, string interval, int period, DateTime time)
        {
            return await _context.ATRResults
                .Where(r => r.Symbol == symbol && r.Interval == interval && r.Period == period && r.Timestamp <= time)
                .OrderByDescending(r => r.Timestamp)
                .FirstOrDefaultAsync();
        }


    }
}
