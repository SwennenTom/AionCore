using AionCoreBot.Infrastructure.Interfaces;
using AionCoreBot.Domain.Models;
using AionCoreBot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

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

        public async Task<ATRResult?> GetLatestBySymbolIntervalPeriodAsync(string symbol, string interval, DateTime timestamp, int period)
        {
            return await _context.ATRResults
                .Where(r => r.Symbol == symbol && r.Interval == interval && r.Period == period && r.Timestamp == timestamp)
                .OrderByDescending(r => r.Timestamp)
                .FirstOrDefaultAsync();
        }

        public async Task<ATRResult?> GetLatestBeforeAsync(string symbol, string interval, int period, DateTime time)
        {
            return await _context.ATRResults
                .Where(r => r.Symbol == symbol && r.Interval == interval && r.Period == period && r.Timestamp <= time)
                .OrderByDescending(r => r.Timestamp)
                .FirstOrDefaultAsync();
        }

        public async Task<ATRResult> GetLatestBySymbolIntervalPeriodAsync(string symbol, string interval, int period)
        {
            return await _context.ATRResults
                .Where(r => r.Symbol == symbol && r.Interval == interval && r.Period == period)
                .OrderByDescending(r => r.Timestamp)
                .FirstAsync(); // Let it throw if nothing found — signals should exist
        }

        public async Task<ATRResult?> GetBySymbolIntervalTimestampPeriodAsync(string symbol, string interval, DateTime timestamp, int period)
        {
            return await _context.ATRResults
                .FirstOrDefaultAsync(r =>
                    r.Symbol == symbol &&
                    r.Interval == interval &&
                    r.Timestamp == timestamp &&
                    r.Period == period);
        }

        public async Task<IEnumerable<ATRResult>> GetByPeriodAndDateRangeAsync(string symbol, string interval, int period, DateTime from, DateTime to)
        {
            return await _context.ATRResults
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
