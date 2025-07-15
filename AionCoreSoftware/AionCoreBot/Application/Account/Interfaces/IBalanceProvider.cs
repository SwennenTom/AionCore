using System.Collections.Generic;
using System.Threading.Tasks;

namespace AionCoreBot.Application.Account.Interfaces
{
    public interface IBalanceProvider
    {
        Task<decimal> GetLastPriceAsync(string symbol, CancellationToken ct = default);
        Task<decimal> GetPositionSizeAsync(string symbol, CancellationToken ct = default);
        Task<Dictionary<string, decimal>> GetBalancesAsync(CancellationToken ct = default);
    }

}
