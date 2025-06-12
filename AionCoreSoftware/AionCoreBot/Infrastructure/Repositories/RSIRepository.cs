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

        public void Delete(RSIResult entity)
        {
            _context.RSIResults.Remove(entity);
        }

        public async Task<IEnumerable<RSIResult>> GetAllAsync()
        {
            return await _context.RSIResults.ToListAsync();
        }

        public async Task<RSIResult?> GetByIdAsync(int id)
        {
            return await _context.RSIResults.FindAsync(id);
        }

        public async Task<IEnumerable<RSIResult>> GetLatestBySymbolAndIntervalAsync(string symbol, string interval, int count = 1)
        {
            return await _context.RSIResults
                .Where(r => r.Symbol == symbol && r.Interval == interval)
                .OrderByDescending(r => r.Timestamp)  // Adjust to actual datetime property name
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
    }
}
