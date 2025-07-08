using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AionCoreBot.Domain.Models
{
    public class Account
    {
        public int Id { get; set; }
        public string BrokerName { get; set; } = null!;
        public string ExternalAccountId { get; set; } = null!;

        public string? DisplayName { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<AccountBalance> Balances { get; set; } = new List<AccountBalance>();
        public ICollection<Trade> Trades { get; set; } = new List<Trade>();
        public ICollection<Position> Positions { get; set; } = new List<Position>();
    }
}
