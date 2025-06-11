using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AionCoreBot.Domain.Models
{
    public class EMAResult
    {
        public int Id { get; set; }
        public string Symbol { get; set; }
        public string Interval { get; set; }
        public DateTime Timestamp { get; set; }
        public int Period { get; set; }
        public decimal EMAValue { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
