using AionCoreBot.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AionCoreBot.Infrastructure.Interfaces
{
    public interface IBalanceHistoryRepository
    {
        Task AddAsync(BalanceHistory history);
        Task AddRangeAsync(IEnumerable<BalanceHistory> history);
    }
}
