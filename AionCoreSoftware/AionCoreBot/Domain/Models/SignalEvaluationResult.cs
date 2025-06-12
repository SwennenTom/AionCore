using AionCoreBot.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AionCoreBot.Domain.Models
{
    public class SignalEvaluationResult
    {
        public int Id { get; set; }
        public string? AnalyzerName { get; set; }

        public string Symbol { get; set; } = string.Empty;

        public string Interval { get; set; } = string.Empty;

        public DateTime EvaluationTime { get; set; } = DateTime.UtcNow;

        // De voorlopige trade-actie, bv. Buy, Sell, Hold
        public TradeAction ProposedAction { get; set; } = TradeAction.Hold;

        // Optionele toelichting of reden voor deze beslissing
        public string? Reason { get; set; }

        // Indicatorwaarden die gebruikt zijn voor deze evaluatie
        // Je kunt hier bijvoorbeeld per indicator naam en waarde opslaan
        public Dictionary<string, decimal> IndicatorValues { get; set; } = new();

        // Optionele score of confidence van deze evaluatie (bijv. 0-100%)
        public decimal? ConfidenceScore { get; set; }

        // Eventueel een lijst van relevante signalen (bv. "EMA crossover", "RSI oversold")
        public List<string>? SignalDescriptions { get; set; }
    }
}
