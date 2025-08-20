using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AionCoreBot.Domain.Models;
using AionCoreBot.Infrastructure.Data;
using AionCoreBot.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AionCoreBot.Infrastructure.Repositories
{
    public class TradeRepository : ITradeRepository
    {
        private readonly ApplicationDbContext _context;

        public TradeRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Trade?> GetByIdAsync(int id)
        {
            return await _context.Trades.FindAsync(id);
        }

        public async Task<IEnumerable<Trade>> GetAllAsync()
        {
            return await _context.Trades.ToListAsync();
        }

        public async Task<IEnumerable<Trade>> GetOpenTradesAsync()
        {
            return await _context.Trades
                .Where(t => !t.IsClosed)
                .ToListAsync();
        }

        public async Task AddAsync(Trade entity)
        {
            await _context.Trades.AddAsync(entity);
            await _context.SaveChangesAsync();
        }

        public Task Update(Trade entity)
        {
            _context.Trades.Update(entity);
            return Task.CompletedTask;
        }

        public Task Delete(Trade entity)
        {
            _context.Trades.Remove(entity);
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
