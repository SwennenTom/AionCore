using AionCoreBot.Domain.Models;
using AionCoreBot.Infrastructure.Data;
using AionCoreBot.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AionCoreBot.Infrastructure.Repositories
{
    public class BalanceHistoryRepository : IBalanceHistoryRepository
    {
        private readonly ApplicationDbContext _context;

        public BalanceHistoryRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(BalanceHistory history)
        {
            await _context.BalanceHistories.AddAsync(history);
            await _context.SaveChangesAsync();
        }

        public async Task AddRangeAsync(IEnumerable<BalanceHistory> history)
        {
            await _context.BalanceHistories.AddRangeAsync(history);
            await _context.SaveChangesAsync();
        }
    }
}
