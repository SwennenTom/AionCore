using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AionCoreBot.Domain.Models;
using AionCoreBot.Infrastructure.Data;
using AionCoreBot.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AionCoreBot.Infrastructure.Repositories
{
    internal class RSIRepository : IIndicatorRepository<RSIResult>
    {
        private readonly ApplicationDbContext _context;

        public RSIRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(RSIResult entity)
        {
            await _context.RSIResults.AddAsync(entity);
        }

        public async Task ClearAllAsync()
        {
            _context.RSIResults.RemoveRange(_context.RSIResults);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<RSIResult>> GetAllAsync()
        {
            return await _context.RSIResults.ToListAsync();
        }

        public async Task<RSIResult?> GetByIdAsync(int id)
        {
            return await _context.RSIResults.FindAsync(id);
        }

        public async Task<RSIResult?> GetLatestBySymbolIntervalPeriodAsync(string symbol, string interval, int period)
        {
            return await _context.RSIResults
                .Where(e => e.Symbol == symbol && e.Interval == interval && e.Period == period)
                .OrderByDescending(e => e.Timestamp)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<RSIResult>> GetLatestNBySymbolAndIntervalAsync(string symbol, string interval, int count = 1)
        {
            return await _context.RSIResults
                .Where(r => r.Symbol == symbol && r.Interval == interval)
                .OrderByDescending(r => r.Timestamp)
                .Take(count)
                .ToListAsync();
        }


        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public void Update(RSIResult entity)
        {
            _context.RSIResults.Update(entity);
        }
        public async Task<RSIResult?> GetLatestBeforeAsync(string symbol, string interval, int period, DateTime time)
        {
            return await _context.RSIResults
                .Where(r => r.Symbol == symbol && r.Interval == interval && r.Period == period && r.Timestamp <= time)
                .OrderByDescending(r => r.Timestamp)
                .FirstOrDefaultAsync();
        }
    }
}
