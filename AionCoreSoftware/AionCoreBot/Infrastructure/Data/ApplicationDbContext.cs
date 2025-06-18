using AionCoreBot.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
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

            // --- Fix for IndicatorValues (Dictionary<string, decimal>) ---
            var dictConverter = new ValueConverter<Dictionary<string, decimal>, string>(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<Dictionary<string, decimal>>(v, (JsonSerializerOptions?)null) ?? new());

            var dictComparer = new ValueComparer<Dictionary<string, decimal>>(
                (d1, d2) => d1.Count == d2.Count && !d1.Except(d2).Any(),
                d => d.Aggregate(0, (a, v) => HashCode.Combine(a, v.Key.GetHashCode(), v.Value.GetHashCode())),
                d => d.ToDictionary(entry => entry.Key, entry => entry.Value));

            modelBuilder.Entity<SignalEvaluationResult>()
                .Property(e => e.IndicatorValues)
                .HasConversion(dictConverter)
                .Metadata.SetValueComparer(dictComparer);

            // --- Fix for SignalDescriptions (List<string>) ---
            var listConverter = new ValueConverter<List<string>, string>(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new());

            var listComparer = new ValueComparer<List<string>>(
                (l1, l2) => l1.SequenceEqual(l2),
                l => l.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                l => l.ToList());

            modelBuilder.Entity<SignalEvaluationResult>()
                .Property(e => e.SignalDescriptions)
                .HasConversion(listConverter)
                .Metadata.SetValueComparer(listComparer);

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
