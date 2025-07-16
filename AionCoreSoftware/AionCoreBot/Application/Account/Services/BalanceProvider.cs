using AionCoreBot.Application.Account.Interfaces;
using Binance.Net.Clients;
using Binance.Net.Objects.Models.Spot;
using CryptoExchange.Net.Authentication;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AionCoreBot.Application.Account.Services
{
    public class BalanceProvider : IBalanceProvider
    {
        private readonly BinanceRestClient _binance;
        private readonly bool _paper;

        public BalanceProvider(BinanceRestClient binanceClient, IConfiguration cfg)
        {
            _binance = binanceClient ?? throw new ArgumentNullException(nameof(binanceClient));
            _paper = cfg.GetValue<bool>("Switches:PaperTrading");
        }

        /* ---------- balances ---------- */
        public async Task<Dictionary<string, decimal>> GetBalancesAsync(CancellationToken ct = default)
        {
            if (_paper)
            {
                // Alleen mock-budget in EUR
                return new Dictionary<string, decimal>
                {
                    { "EUR", 10_000m }
                };
            }

            var accountResult = await _binance.SpotApi.Account.GetAccountInfoAsync(ct: ct);
            if (!accountResult.Success || accountResult.Data == null)
                throw new Exception($"Kon accountinfo niet ophalen: {accountResult.Error?.Message}");

            // Filter alleen assets met vrije balans > 0
            return accountResult.Data.Balances
                .Where(b => b.Available > 0)
                .ToDictionary(
                    b => b.Asset,
                    b => b.Available
                );
        }

        /* ---------- huidige positie ---------- */
        public async Task<decimal> GetPositionSizeAsync(string symbol, CancellationToken ct = default)
        {
            if (_paper)
                return 0m; // geen open posities in paper-mode

            var balances = await GetBalancesAsync(ct);

            // Voor BTCUSDT halen we "BTC"
            var baseAsset = symbol.Replace("USDT", "", StringComparison.InvariantCultureIgnoreCase)
                                  .Replace("EUR", "", StringComparison.InvariantCultureIgnoreCase);

            return balances.TryGetValue(baseAsset, out var amount) ? amount : 0m;
        }

        /* ---------- laatste marktprijs ---------- */
        public async Task<decimal> GetLastPriceAsync(string symbol, CancellationToken ct = default)
        {
            var priceResult = await _binance.SpotApi.ExchangeData.GetPriceAsync(symbol, ct: ct);
            if (!priceResult.Success || priceResult.Data == null)
                throw new Exception($"Kon prijs niet ophalen voor {symbol}: {priceResult.Error?.Message}");

            return priceResult.Data.Price;
        }
    }
}
