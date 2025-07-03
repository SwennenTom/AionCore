using AionCoreBot.Application.Risk.Interfaces;
using AionCoreBot.Domain.Enums;
using System;
using System.Collections.Generic;

namespace AionCoreBot.Application.Risk.Services
{
    public class RiskManagementService : IRiskManagementService
    {
        // Aannames: Simpel configurabel percentage risico
        private const decimal DefaultRiskPercentage = 0.01m; // 1% risico per trade
        private const decimal FakeAccountBalance = 10_000m;   // Dummy balans in USDT
        private readonly Dictionary<string, decimal> _maxPositionSizes = new(); // Eventueel later vullen met echte limieten

        public decimal CalculateMaxRiskAmount(string symbol, decimal currentPositionSize)
        {
            // Simpele implementatie: altijd 1% van het account
            return FakeAccountBalance * DefaultRiskPercentage;
        }

        public decimal CalculatePositionSize(string symbol, decimal riskAmount, decimal entryPrice)
        {
            if (entryPrice <= 0)
                throw new ArgumentException("Entry price moet groter dan 0 zijn.");

            // Simpele regel: hoeveel eenheden kun je kopen als je maximaal dit risico wil nemen?
            // Voor nu: riskAmount = max verlies, dus positionSize = riskAmount / assumedStopLossDistance
            // Zonder SL gebruiken we gewoon riskAmount / entryPrice
            return riskAmount / entryPrice;
        }

        public bool IsTradeWithinRiskLimits(string symbol, TradeAction proposedAction, decimal positionSize)
        {
            // Simpele limietcontrole (bijv. max 5 BTC per trade)
            if (_maxPositionSizes.TryGetValue(symbol, out var maxAllowed))
            {
                return positionSize <= maxAllowed;
            }

            // Geen limiet bekend? Sta toe, maar dit kun je later strenger maken
            return true;
        }

        public void UpdateRiskParameters(int tradeId)
        {
            // Placeholder — later uitbreiden voor SL/TP/trailing stop updates
            Console.WriteLine($"Risk parameters voor trade {tradeId} geüpdatet (stub).");
        }
    }
}
