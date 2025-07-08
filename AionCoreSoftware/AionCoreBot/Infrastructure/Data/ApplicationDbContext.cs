using AionCoreBot.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
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
        public DbSet<Account> Accounts => Set<Account>();
        public DbSet<AccountBalance> AccountBalances => Set<AccountBalance>();
        public DbSet<Position> Positions => Set<Position>();
        public DbSet<Trade> Trades { get; set; }
        public DbSet<TradeDecision> TradeDecisions { get; set; }
        public DbSet<SignalEvaluationResult> SignalEvaluations { get; set; }
        public DbSet<RSIResult> RSIResults { get; set; }
        public DbSet<ATRResult> ATRResults { get; set; }
        public DbSet<EMAResult> EMAResults { get; set; }
        public DbSet<LogEntry> Logs { get; set; }
        public DbSet<Candle> Candles { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // --- Primaire sleutels ---
            modelBuilder.Entity<TradeDecision>()
                .HasKey(td => new { td.Symbol, td.Interval, td.DecisionTime });

            modelBuilder.Entity<Candle>()
                .HasKey(c => new { c.Symbol, c.Interval, c.OpenTime });

            // --- SignalEvaluationResult: IndicatorValues (Dictionary<string, decimal>) ---
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

            // --- SignalEvaluationResult: SignalDescriptions (List<string>) ---
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

            // --- LogEntry constraints (optioneel) ---
            modelBuilder.Entity<LogEntry>()
                .Property(l => l.SourceComponent)
                .HasMaxLength(255);

            modelBuilder.Entity<LogEntry>()
                .Property(l => l.ExceptionDetails)
                .HasMaxLength(2000);
            modelBuilder.Entity<Position>()
        .HasOne(p => p.OpenTrade)
        .WithMany()
        .HasForeignKey(p => p.OpenTradeId)
        .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Position>()
                .HasOne(p => p.CloseTrade)
                .WithMany()
                .HasForeignKey(p => p.CloseTradeId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
