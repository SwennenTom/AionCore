using AionCoreBot.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AionCoreBot.Domain.Models
{
    public class Position
    {
        public int Id { get; set; }

        public int AccountId { get; set; }
        public Account Account { get; set; } = null!;

        public string Symbol { get; set; } = null!; // bv. "BTCEUR"
        public decimal Quantity { get; set; }
        public decimal EntryPrice { get; set; }
        public TradeAction Side { get; set; } // Buy / Short

        public DateTime OpenedAt { get; set; }
        public DateTime? ClosedAt { get; set; }

        public int? OpenTradeId { get; set; }
        public Trade? OpenTrade { get; set; }

        public int? CloseTradeId { get; set; }
        public Trade? CloseTrade { get; set; }
    }

}
