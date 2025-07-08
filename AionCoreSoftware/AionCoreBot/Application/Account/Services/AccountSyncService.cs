using AionCoreBot.Application.Account.Interfaces;
using AionCoreBot.Domain.Models;
using AionCoreBot.Infrastructure.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AionCoreBot.Application.Account.Services
{
    public class AccountSyncService : IAccountSyncService
    {
        private readonly IAccountBalanceRepository _balanceRepository;
        private readonly IBalanceHistoryRepository _historyRepository;
        private readonly IBalanceProvider _balanceProvider;

        public AccountSyncService(
            IAccountBalanceRepository balanceRepository,
            IBalanceHistoryRepository historyRepository,
            IBalanceProvider balanceProvider)
        {
            _balanceRepository = balanceRepository;
            _historyRepository = historyRepository;
            _balanceProvider = balanceProvider;
        }

        public async Task InitializeAsync()
        {
            var balances = await _balanceProvider.GetBalancesAsync();

            var entries = balances.Select(b => new AccountBalance
            {
                Asset = b.Key,
                Amount = b.Value,
                LastUpdated = DateTime.UtcNow
            });

            await _balanceRepository.BulkUpsertAsync(entries);
            await LogHistoryAsync(entries);
        }

        public async Task SyncAsync()
        {
            var liveBalances = await _balanceProvider.GetBalancesAsync();
            var currentBalances = await _balanceRepository.GetAllAsync();

            var updatedList = new List<AccountBalance>();
            var historyList = new List<BalanceHistory>();

            foreach (var kv in liveBalances)
            {
                var asset = kv.Key;
                var amount = kv.Value;

                var existing = currentBalances.FirstOrDefault(b => b.Asset == asset);
                if (existing != null)
                {
                    if (existing.Amount != amount)
                    {
                        existing.Amount = amount;
                        existing.LastUpdated = DateTime.UtcNow;
                        updatedList.Add(existing);
                        historyList.Add(new BalanceHistory { Asset = asset, Amount = amount });
                    }
                }
                else
                {
                    var newEntry = new AccountBalance
                    {
                        Asset = asset,
                        Amount = amount,
                        LastUpdated = DateTime.UtcNow
                    };
                    updatedList.Add(newEntry);
                    historyList.Add(new BalanceHistory { Asset = asset, Amount = amount });
                }
            }

            await _balanceRepository.BulkUpsertAsync(updatedList);
            await _historyRepository.AddRangeAsync(historyList);
        }

        private async Task LogHistoryAsync(IEnumerable<AccountBalance> balances)
        {
            var history = balances.Select(b => new BalanceHistory
            {
                Asset = b.Asset,
                Amount = b.Amount,
                Timestamp = DateTime.UtcNow
            });

            await _historyRepository.AddRangeAsync(history);
        }
    }
}
