using AionCoreBot.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AionCoreBot.Domain.Models
{
    public class ATRResult: IIndicatorResult
    {
        public int Id { get; set; }
        public string Symbol { get; set; }
        public string Interval { get; set; }
        public DateTime Timestamp { get; set; }
        public int Period { get; set; }
        public decimal Value { get; set; }
        public decimal ClosePrice { get; set; }
        public decimal ValuePct { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow.AddTicks(-(DateTime.UtcNow.Ticks % TimeSpan.TicksPerMillisecond));
    }
}
