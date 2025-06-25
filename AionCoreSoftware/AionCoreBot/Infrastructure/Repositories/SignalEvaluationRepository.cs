using AionCoreBot.Domain.Models;
using AionCoreBot.Infrastructure.Data;
using AionCoreBot.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AionCoreBot.Infrastructure.Repositories
{
    public class SignalEvaluationRepository : ISignalEvaluationRepository
    {
        private readonly ApplicationDbContext _context;

        public SignalEvaluationRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(SignalEvaluationResult result)
        {
            await _context.SignalEvaluations.AddAsync(result);
        }

        public async Task AddRangeAsync(IEnumerable<SignalEvaluationResult> results)
        {
            await _context.SignalEvaluations.AddRangeAsync(results);
        }

        public async Task<IEnumerable<SignalEvaluationResult>> GetBySymbolAndIntervalAsync(string symbol, string interval)
        {
            return await _context.SignalEvaluations
                .Where(s => s.Symbol == symbol && s.Interval == interval)
                .ToListAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task ClearAllAsync()
        {   _context.SignalEvaluations.RemoveRange(_context.SignalEvaluations);
            await _context.SaveChangesAsync();
        }
    }

}
