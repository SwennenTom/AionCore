using AionCoreBot.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AionCoreBot.Domain.Models
{
    public class Trade
    {
        public int Id { get; set; }

        public string Symbol { get; set; } = string.Empty;
        public string Interval { get; set; } = string.Empty;

        public TradeAction EntryAction { get; set; }           // Bijv. Buy, Short, LimitBuy
        public DateTime OpenTime { get; set; }                 // Moment van openen
        public decimal OpenPrice { get; set; }

        public decimal Quantity { get; set; }
        public decimal PaidFee { get; set; }

        public TradeAction? ExitAction { get; set; }           // Bijv. Sell, Cover, Liquidate
        public DateTime? CloseTime { get; set; }               // Moment van sluiten (null zolang open)
        public decimal? ClosePrice { get; set; }               // null zolang positie niet gesloten is

        public decimal? ProfitLoss { get; set; }               // Kan negatief zijn

        public string Strategy { get; set; } = string.Empty;   // Naam van strategie die deze trade veroorzaakte
        public string? Reason { get; set; }                    // (optioneel) Toelichting (bijv. signaalomschrijving)

        public bool IsClosed => CloseTime.HasValue;            // Handige property

        public string Exchange { get; set; } = "Unknown";       // Future-proof: welke broker of exchange
        public string? ExchangeOrderId { get; set; }              // Bijv. Binance OrderId
        public string? ClientOrderId { get; set; }              // Zelf gegenereerde order-ID
        public string? BrokerOrderReference { get; set; }
    }
}
