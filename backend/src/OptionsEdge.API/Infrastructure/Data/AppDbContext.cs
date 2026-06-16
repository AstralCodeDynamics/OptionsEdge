using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OptionsEdge.API.Domain.Entities;

namespace OptionsEdge.API.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<Position> Positions { get; set; }
    public DbSet<Signal> Signals { get; set; }
    public DbSet<Alert> Alerts { get; set; }
    public DbSet<ChatMessage> ChatMessages { get; set; }
    public DbSet<AIUsageLog> AIUsageLogs { get; set; }
    public DbSet<BacktestResult> BacktestResults { get; set; }
    public DbSet<GrowwCredential> GrowwCredentials { get; set; }
    public DbSet<UserAICredential> UserAICredentials => Set<UserAICredential>();
    public DbSet<UserSignalPreference> UserSignalPreferences => Set<UserSignalPreference>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ApplicationUser>(e =>
        {
            e.Property(u => u.DisplayName).HasMaxLength(100);
            e.Property(u => u.SubscriptionPlan).HasMaxLength(20);
            e.Property(u => u.WalletBalance).HasColumnType("decimal(10,4)");
            e.Property(u => u.CreatedAt).HasDefaultValueSql("now()");
            e.Property(u => u.UpdatedAt).HasDefaultValueSql("now()");
        });

        modelBuilder.Entity<RefreshToken>(e =>
        {
            e.HasKey(r => r.Id);
            e.Property(r => r.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(r => r.Token).HasMaxLength(255).IsRequired();
            e.HasIndex(r => r.Token).IsUnique();
            e.HasIndex(r => new { r.UserId, r.IsRevoked });
            e.Property(r => r.CreatedAt).HasDefaultValueSql("now()");
            e.HasOne(r => r.User).WithMany().HasForeignKey(r => r.UserId);
        });

        modelBuilder.Entity<Position>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(p => p.Symbol).HasMaxLength(20).IsRequired();
            e.Property(p => p.OptionType).HasMaxLength(2).IsRequired();
            e.Property(p => p.Status).HasMaxLength(20);
            e.Property(p => p.ExitReason).HasMaxLength(50);
            e.Property(p => p.EntryPrice).HasColumnType("decimal(10,2)");
            e.Property(p => p.StopLoss).HasColumnType("decimal(10,2)");
            e.Property(p => p.Target1).HasColumnType("decimal(10,2)");
            e.Property(p => p.Target2).HasColumnType("decimal(10,2)");
            e.Property(p => p.ExitPrice).HasColumnType("decimal(10,2)");
            e.Property(p => p.CreatedAt).HasDefaultValueSql("now()");
            e.HasOne(p => p.User).WithMany(u => u.Positions).HasForeignKey(p => p.UserId);
            e.HasIndex(p => new { p.UserId, p.Status });
        });

        modelBuilder.Entity<Signal>(e =>
        {
            e.HasKey(s => s.Id);
            e.Property(s => s.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(s => s.Symbol).HasMaxLength(20).IsRequired();
            e.Property(s => s.SignalType).HasMaxLength(20);
            e.Property(s => s.OptionType).HasMaxLength(2);
            e.Property(s => s.ModelUsed).HasMaxLength(50);
            e.Property(s => s.EntryLow).HasColumnType("decimal(10,2)");
            e.Property(s => s.EntryHigh).HasColumnType("decimal(10,2)");
            e.Property(s => s.StopLoss).HasColumnType("decimal(10,2)");
            e.Property(s => s.Target1).HasColumnType("decimal(10,2)");
            e.Property(s => s.Target2).HasColumnType("decimal(10,2)");
            e.Property(s => s.RiskReward).HasColumnType("decimal(5,2)");
            e.Property(s => s.CostUsd).HasColumnType("decimal(10,6)");
            e.Property(s => s.Rationale).HasColumnType("text[]");
            e.Property(s => s.MarketSnapshot).HasColumnType("jsonb");
            e.Property(s => s.CreatedAt).HasDefaultValueSql("now()");
            e.HasOne(s => s.User).WithMany(u => u.Signals).HasForeignKey(s => s.UserId);
            e.HasIndex(s => new { s.UserId, s.CreatedAt }).IsDescending(false, true);
        });

        modelBuilder.Entity<Alert>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(a => a.Severity).HasMaxLength(10);
            e.Property(a => a.AlertType).HasMaxLength(50);
            e.Property(a => a.CreatedAt).HasDefaultValueSql("now()");
            e.HasOne(a => a.User).WithMany(u => u.Alerts).HasForeignKey(a => a.UserId);
            e.HasOne(a => a.Position).WithMany(p => p.Alerts).HasForeignKey(a => a.PositionId);
            e.HasIndex(a => new { a.UserId, a.IsRead });
        });

        modelBuilder.Entity<ChatMessage>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(c => c.Role).HasMaxLength(10);
            e.Property(c => c.ModelUsed).HasMaxLength(50);
            e.Property(c => c.CostUsd).HasColumnType("decimal(10,6)");
            e.Property(c => c.CreatedAt).HasDefaultValueSql("now()");
            e.HasOne(c => c.User).WithMany(u => u.ChatMessages).HasForeignKey(c => c.UserId);
            e.HasIndex(c => new { c.SessionId, c.CreatedAt });
            e.HasIndex(c => new { c.UserId, c.SessionId, c.CreatedAt });
        });

        modelBuilder.Entity<AIUsageLog>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(a => a.Feature).HasMaxLength(50);
            e.Property(a => a.ModelUsed).HasMaxLength(50);
            e.Property(a => a.CostUsd).HasColumnType("decimal(10,6)");
            e.Property(a => a.WalletBefore).HasColumnType("decimal(10,4)");
            e.Property(a => a.WalletAfter).HasColumnType("decimal(10,4)");
            e.Property(a => a.CreatedAt).HasDefaultValueSql("now()");
            e.HasOne(a => a.User).WithMany(u => u.AIUsageLogs).HasForeignKey(a => a.UserId);
            e.HasIndex(a => new { a.UserId, a.CreatedAt });
        });

        modelBuilder.Entity<BacktestResult>(e =>
        {
            e.HasKey(b => b.Id);
            e.Property(b => b.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(b => b.Strategy).HasMaxLength(50);
            e.Property(b => b.Parameters).HasColumnType("jsonb");
            e.Property(b => b.TradeLog).HasColumnType("jsonb");
            e.Property(b => b.WinRate).HasColumnType("decimal(5,2)");
            e.Property(b => b.NetPnl).HasColumnType("decimal(12,2)");
            e.Property(b => b.MaxDrawdown).HasColumnType("decimal(12,2)");
            e.Property(b => b.SharpeRatio).HasColumnType("decimal(5,2)");
            e.Property(b => b.ProfitFactor).HasColumnType("decimal(5,2)");
            e.Property(b => b.CreatedAt).HasDefaultValueSql("now()");
            e.HasOne(b => b.User).WithMany(u => u.BacktestResults).HasForeignKey(b => b.UserId);
            e.HasIndex(b => new { b.UserId, b.CreatedAt }).IsDescending(false, true);
        });

        modelBuilder.Entity<GrowwCredential>(e =>
        {
            e.HasKey(g => g.Id);
            e.Property(g => g.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(g => g.ApiKeyEncrypted).IsRequired();
            e.Property(g => g.ApiSecretEncrypted).IsRequired();
            e.Property(g => g.CreatedAt).HasDefaultValueSql("now()");
            e.Property(g => g.UpdatedAt).HasDefaultValueSql("now()");
            e.HasIndex(g => g.UserId).IsUnique();
            e.HasOne(g => g.User).WithOne().HasForeignKey<GrowwCredential>(g => g.UserId);
        });

        modelBuilder.Entity<UserAICredential>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(x => x.ApiKeyEncrypted).IsRequired();
            e.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
            e.Property(x => x.UpdatedAt).HasDefaultValueSql("now()");
            e.HasIndex(x => x.UserId).IsUnique();
            e.HasOne(x => x.User).WithOne().HasForeignKey<UserAICredential>(x => x.UserId);
        });

        modelBuilder.Entity<UserSignalPreference>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(x => x.NiftyAutoSignalTimes).IsRequired();
            e.Property(x => x.BankNiftyAutoSignalTimes).IsRequired();
            e.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
            e.Property(x => x.UpdatedAt).HasDefaultValueSql("now()");
            e.HasIndex(x => x.UserId).IsUnique();
            e.HasOne(x => x.User).WithOne().HasForeignKey<UserSignalPreference>(x => x.UserId);
        });

        // Defense-in-depth: normalize all DateTimeOffset properties to UTC before Npgsql writes
        // them. Postgres timestamptz requires offset=0; any non-UTC offset (e.g. +05:30 from AI
        // output) would otherwise throw ArgumentException at SaveChangesAsync.
        var utcConverter = new ValueConverter<DateTimeOffset, DateTimeOffset>(
            v => v.ToUniversalTime(),
            v => v);
        var utcConverterNullable = new ValueConverter<DateTimeOffset?, DateTimeOffset?>(
            v => v.HasValue ? v.Value.ToUniversalTime() : v,
            v => v);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTimeOffset))
                    property.SetValueConverter(utcConverter);
                else if (property.ClrType == typeof(DateTimeOffset?))
                    property.SetValueConverter(utcConverterNullable);
            }
        }
    }
}
