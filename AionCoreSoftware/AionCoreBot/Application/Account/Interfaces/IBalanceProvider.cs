using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AionCoreBot.Application.Account.Interfaces
{
    public interface IBalanceProvider
    {
        Task<Dictionary<string, decimal>> GetBalancesAsync(); // bv. { "EUR": 1000m, "BTC": 0.05m }
    }
}
