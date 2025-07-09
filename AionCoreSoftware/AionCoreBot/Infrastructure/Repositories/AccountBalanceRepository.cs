using AionCoreBot.Domain.Models;
using AionCoreBot.Infrastructure.Data;
using AionCoreBot.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AionCoreBot.Infrastructure.Repositories
{
    public class AccountBalanceRepository : IAccountBalanceRepository
    {
        private readonly ApplicationDbContext _context;

        public AccountBalanceRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<AccountBalance>> GetAllAsync()
        {
            return await _context.AccountBalances.ToListAsync();
        }

        public async Task<AccountBalance?> GetByAssetAsync(string asset)
        {
            return await _context.AccountBalances.FirstOrDefaultAsync(x => x.Asset == asset);
        }

        public async Task UpsertAsync(AccountBalance balance)
        {
            var existing = await GetByAssetAsync(balance.Asset);
            if (existing != null)
            {
                existing.Amount = balance.Amount;
                existing.LastUpdated = balance.LastUpdated;
                _context.AccountBalances.Update(existing);
            }
            else
            {
                await _context.AccountBalances.AddAsync(balance);
            }

            await _context.SaveChangesAsync();
        }

        public async Task BulkUpsertAsync(IEnumerable<AccountBalance> balances)
        {
            foreach (var balance in balances)
            {
                await UpsertAsync(balance);
            }
        }
    }
}
