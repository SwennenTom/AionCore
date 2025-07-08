using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AionCoreBot.Domain.Models
{
    public class AccountBalance
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid AccountId { get; set; }
        public Account Account { get; set; } = null!;

        public string Asset { get; set; } = null!; // bv. "EUR", "USDT", "BTC"
        public decimal Total { get; set; }
        public decimal Available { get; set; }

        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }

}
