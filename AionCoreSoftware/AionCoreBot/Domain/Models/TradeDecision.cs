using AionCoreBot.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AionCoreBot.Domain.Models
{
    public class TradeDecision
    {
        public string Symbol { get; set; } = string.Empty;
        public string Interval { get; set; } = string.Empty;

        public TradeAction Action { get; set; }

        public DateTime DecisionTime { get; set; } = DateTime.UtcNow;

        public string? Reason { get; set; }

        public decimal? SuggestedPrice { get; set; }  // Optioneel, bv. voor limit orders

        public decimal? Quantity { get; set; }  // Optioneel, kan door de strategie gesuggereerd worden

        public TradeDecision() { }

        public TradeDecision(string symbol, string interval, TradeAction action, string? reason = null)
        {
            Symbol = symbol;
            Interval = interval;
            Action = action;
            Reason = reason;
            DecisionTime = DateTime.UtcNow;
        }
    }
}
