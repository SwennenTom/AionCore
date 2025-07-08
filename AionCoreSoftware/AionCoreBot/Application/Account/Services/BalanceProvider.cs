using AionCoreBot.Application.Account.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AionCoreBot.Application.Account.Services
{
    public class MockBalanceProvider : IBalanceProvider
    {//MOCK DATA, VERWIJDER DIT IN ECHTE IMPLEMENTATIE
        public Task<Dictionary<string, decimal>> GetBalancesAsync()
        {
            return Task.FromResult(new Dictionary<string, decimal>
        {
            { "EUR", 1200.45m },
            { "BTC", 0.075m },
            { "ETH", 1.92m }
        });
        }
    }

}
