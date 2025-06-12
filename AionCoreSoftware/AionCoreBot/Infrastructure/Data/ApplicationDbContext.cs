using AionCoreBot.Domain.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text.Json;

namespace AionCoreBot.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // DbSets
        public DbSet<Trade> Trades { get; set; }
        public DbSet<TradeDecision> TradeDecisions { get; set; }
        public DbSet<SignalEvaluationResult> SignalEvaluations { get; set; }
        public DbSet<RSIResult> RSIResults { get; set; }
        public DbSet<ATRResult> ATRResults { get; set; }
        public DbSet<EMAResult> EMAResults { get; set; }
        public DbSet<LogEntry> Logs { get; set; }

        // Let op: Candle is geen entiteit met een Id, we moeten dit expliciet configureren.
        public DbSet<Candle> Candles { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Complexe types of keys
            modelBuilder.Entity<TradeDecision>()
                .HasKey(td => new { td.Symbol, td.Interval, td.DecisionTime });

            modelBuilder.Entity<Candle>()
                .HasKey(c => new { c.Symbol, c.Interval, c.OpenTime });

            modelBuilder.Entity<SignalEvaluationResult>()
                .Property(e => e.IndicatorValues)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, decimal>>(v, (JsonSerializerOptions)null));

            modelBuilder.Entity<SignalEvaluationResult>()
                .Property(e => e.SignalDescriptions)
                .HasConversion(
                    v => string.Join(";", v ?? new List<string>()),
                    v => v.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList());

            // LogEntry string length constraints (optioneel)
            modelBuilder.Entity<LogEntry>()
                .Property(l => l.SourceComponent)
                .HasMaxLength(255);

            modelBuilder.Entity<LogEntry>()
                .Property(l => l.ExceptionDetails)
                .HasMaxLength(2000);
        }
    }
}
