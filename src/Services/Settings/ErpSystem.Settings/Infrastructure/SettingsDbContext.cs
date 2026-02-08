using ErpSystem.Settings.Domain;
using Microsoft.EntityFrameworkCore;

namespace ErpSystem.Settings.Infrastructure;

public class SettingsDbContext : DbContext
{
    public SettingsDbContext(DbContextOptions<SettingsDbContext> options) : base(options)
    {
    }

    public DbSet<UserPreference> UserPreferences => Set<UserPreference>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<UserPreference>(entity =>
        {
            entity.ToTable("UserPreferences");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.TenantId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Language).HasMaxLength(10);
            entity.Property(e => e.TimeZone).HasMaxLength(50);
            entity.Property(e => e.DateFormat).HasMaxLength(20);
            entity.Property(e => e.CurrencyFormat).HasMaxLength(10);
            entity.Property(e => e.Theme).HasMaxLength(20);
        });
    }
}
