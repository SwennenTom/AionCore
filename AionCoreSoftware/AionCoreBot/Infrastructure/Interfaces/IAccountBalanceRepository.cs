using AionCoreBot.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AionCoreBot.Infrastructure.Interfaces
{
    public interface IAccountBalanceRepository
    {
        Task<List<AccountBalance>> GetAllAsync();
        Task<AccountBalance?> GetByAssetAsync(string asset);
        Task UpsertAsync(AccountBalance balance);
        Task BulkUpsertAsync(IEnumerable<AccountBalance> balances);
        Task ClearAllAsync();
    }
}
