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
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid AccountId { get; set; }
        public Account Account { get; set; } = null!;

        public string Symbol { get; set; } = null!; // bv. "BTCEUR"
        public decimal Quantity { get; set; }
        public decimal EntryPrice { get; set; }
        public TradeAction Side { get; set; } // Buy / Short

        public DateTime OpenedAt { get; set; }
        public DateTime? ClosedAt { get; set; }

        public Guid? OpenTradeId { get; set; }
        public Trade? OpenTrade { get; set; }

        public Guid? CloseTradeId { get; set; }
        public Trade? CloseTrade { get; set; }
    }

}
