using Microsoft.EntityFrameworkCore;
using OmniEurope.Blazor.Showcase.Back.Models;
using OmniEurope.Blazor.Showcase.Shared.Components.Preferences;

namespace OmniEurope.Blazor.Showcase.Back.Data;

public sealed class ShowcaseDbContext(DbContextOptions<ShowcaseDbContext> options) : DbContext(options)
{
    /// <summary>The length of <see cref="Visitor.Id"/>: a SHA-256 in hexadecimal.</summary>
    public const int VisitorKeyLength = 64;

    public DbSet<Visitor> Visitors => Set<Visitor>();

    public DbSet<VisitorPreference> VisitorPreferences => Set<VisitorPreference>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<Visitor>(entity =>
        {
            entity.HasKey(visitor => visitor.Id);
            entity.Property(visitor => visitor.Id).HasMaxLength(VisitorKeyLength).IsFixedLength();
            entity.Property(visitor => visitor.Revision).IsConcurrencyToken();
            // The retention deletes by idle date.
            entity.HasIndex(visitor => visitor.LastSeenAt);
            // A visitor removed by the retention takes its preferences with it, in the database itself:
            // the bulk delete of the retention never loads them.
            entity.HasMany(visitor => visitor.Preferences).WithOne()
                .HasForeignKey(preference => preference.VisitorId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<VisitorPreference>(entity =>
        {
            entity.HasKey(preference => preference.Id);
            entity.Property(preference => preference.VisitorId).HasMaxLength(VisitorKeyLength).IsFixedLength();
            entity.Property(preference => preference.Key).HasMaxLength(VisitorPreferenceApi.MaxKeyLength);
            entity.HasIndex(preference => new { preference.VisitorId, preference.Key }).IsUnique();
            entity.HasIndex(preference => new { preference.VisitorId, preference.UpdatedAt });
        });
    }
}
