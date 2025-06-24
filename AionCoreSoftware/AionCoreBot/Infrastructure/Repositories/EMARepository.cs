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
    public class EMARepository : IIndicatorRepository<EMAResult>
    {
        private readonly ApplicationDbContext _context;

        public EMARepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(EMAResult entity)
        {
            await _context.EMAResults.AddAsync(entity);
        }

        public async Task ClearAllAsync()
        {
            _context.EMAResults.RemoveRange(_context.EMAResults);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<EMAResult>> GetAllAsync()
        {
            return await _context.EMAResults.ToListAsync();
        }

        public async Task<EMAResult?> GetByIdAsync(int id)
        {
            return await _context.EMAResults.FindAsync(id);
        }

        public async Task<EMAResult?> GetLatestBySymbolIntervalPeriodAsync(string symbol, string interval, int period)
        {
            return await _context.EMAResults
                .Where(e => e.Symbol == symbol && e.Interval == interval && e.Period == period)
                .OrderByDescending(e => e.Timestamp)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<EMAResult>> GetLatestNBySymbolAndIntervalAsync(string symbol, string interval, int count = 1)
        {
            return await _context.EMAResults
                .Where(e => e.Symbol == symbol && e.Interval == interval)
                .OrderByDescending(e => e.Timestamp)
                .Take(count)
                .ToListAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public void Update(EMAResult entity)
        {
            _context.EMAResults.Update(entity);
        }

        public async Task<EMAResult?> GetLatestBeforeAsync(string symbol, string interval, int period, DateTime time)
        {
            return await _context.EMAResults
                .Where(r => r.Symbol == symbol && r.Interval == interval && r.Period == period && r.Timestamp <= time)
                .OrderByDescending(r => r.Timestamp)
                .FirstOrDefaultAsync();
        }

        public async Task<EMAResult?> GetBySymbolIntervalTimestampPeriodAsync(string symbol, string interval, DateTime timestamp, int period)
        {
            return await _context.EMAResults
                .FirstOrDefaultAsync(r =>
                    r.Symbol == symbol &&
                    r.Interval == interval &&
                    r.Timestamp == timestamp &&
                    r.Period == period);
        }

        public async Task<IEnumerable<EMAResult>> GetByPeriodAndDateRangeAsync(string symbol, string interval, int period, DateTime from, DateTime to)
        {
            return await _context.EMAResults
                .Where(r => r.Symbol == symbol &&
                            r.Interval == interval &&
                            r.Period == period &&
                            r.Timestamp >= from &&
                            r.Timestamp <= to)
                .OrderBy(r => r.Timestamp)
                .ToListAsync();
        }

    }
}
