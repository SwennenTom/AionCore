using AionCoreBot.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AionCoreBot.Domain.Models
{
    public class SignalEvaluationResult
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string? AnalyzerName { get; set; }

        public string Symbol { get; set; } = string.Empty;

        public string Interval { get; set; } = string.Empty;

        public DateTime EvaluationTime { get; set; } = DateTime.UtcNow.AddTicks(-(DateTime.UtcNow.Ticks % TimeSpan.TicksPerMillisecond));

        public TradeAction ProposedAction { get; set; } = TradeAction.Hold;

        public bool? WasContradicted { get; set; } = null;

        public string? Reason { get; set; }

        public Dictionary<string, decimal> IndicatorValues { get; set; } = new();

        public decimal? ConfidenceScore { get; set; }

        public List<string>? SignalDescriptions { get; set; }
        public DateTime CreatedAt { get; set; } = new DateTime(DateTime.UtcNow.Ticks - (DateTime.UtcNow.Ticks % TimeSpan.TicksPerSecond), DateTimeKind.Utc);


    }
}
