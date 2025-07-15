using AionCoreBot.Application.Account.Interfaces;
using AionCoreBot.Infrastructure.Interfaces;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AionCoreBot.Application.Account.Services
{
    public class BalanceProvider : IBalanceProvider
    {
        private readonly IBinanceRestClient _binance;
        private readonly bool _paper;

        public BalanceProvider(IBinanceRestClient binanceClient, IConfiguration cfg)
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

            string json = await _binance.GetAccountInfoAsync();

            using var doc = JsonDocument.Parse(json);
            var balances = new Dictionary<string, decimal>();

            foreach (var bal in doc.RootElement.GetProperty("balances").EnumerateArray())
            {
                string asset = bal.GetProperty("asset").GetString()!;
                string free = bal.GetProperty("free").GetString()!;

                if (decimal.TryParse(
                        free,
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out var amount) && amount > 0)
                {
                    balances[asset] = amount;
                }
            }
            return balances;
        }

        /* ---------- huidige positie ---------- */

        public async Task<decimal> GetPositionSizeAsync(string symbol, CancellationToken ct = default)
        {
            if (_paper) return 0m;                                 // geen open posities in paper-mode

            var bals = await GetBalancesAsync(ct);
            string asset = symbol.Replace("EUR", "", StringComparison.InvariantCultureIgnoreCase);

            return bals.TryGetValue(asset, out var size) ? size : 0m;
        }

        /* ---------- laatste marktprijs ---------- */

        public async Task<decimal> GetLastPriceAsync(string symbol, CancellationToken ct = default)
        {
            // Ook in paper-mode willen we een échte marktprijs gebruiken
            string json = await _binance.GetRawAsync($"/api/v3/ticker/price?symbol={symbol.ToUpperInvariant()}");

            using var doc = JsonDocument.Parse(json);
            string priceStr = doc.RootElement.GetProperty("price").GetString()!;

            if (decimal.TryParse(priceStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var price))
                return price;

            throw new Exception($"Kon prijs niet parsen voor {symbol}");
        }
    }
}
