using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class SeasonConfiguration : IEntityTypeConfiguration<Season>
{
    public void Configure(EntityTypeBuilder<Season> builder)
    {
        builder.HasKey(s => s.Id);

        // Use appropriate PostgreSQL types
        builder.Property(s => s.Name).IsRequired().HasMaxLength(100).HasColumnType("varchar(100)");
        builder.Property(s => s.LeagueName).HasMaxLength(50).HasColumnType("varchar(50)");
        builder.Property(s => s.Country).HasMaxLength(50).HasColumnType("varchar(50)");

        // Use boolean type for flags
        builder.Property(s => s.IsActive).HasColumnType("boolean").HasDefaultValue(false);
        builder.Property(s => s.IsCompleted).HasColumnType("boolean").HasDefaultValue(false);
        builder.Property(s => s.StartDate).HasColumnType("date");
        builder.Property(s => s.EndDate).HasColumnType("date");


        // Set timestamp for audit fields
        builder.Property(s => s.CreatedAt)
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()");

        builder.Property(s => s.UpdatedAt)
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()");

        // Create unique index on league and country combination
        builder.HasIndex(s => new { s.LeagueName, s.Country, s.Name })
            .IsUnique()
            .HasMethod("btree")
            .HasDatabaseName("IX_Season_LeagueSeason");

        // Create index for active seasons
        builder.HasIndex(s => s.IsActive)
            .HasMethod("btree")
            .HasDatabaseName("IX_Season_Active");

        // Index for finding the current round of active seasons
        builder.HasIndex(s => new { s.IsActive, s.CurrentRound })
            .HasMethod("btree")
            .HasDatabaseName("IX_Season_CurrentRound")
            .HasFilter("\"IsActive\" = 'true'");
        // Relationships

        builder.HasMany(s => s.SeasonTeams)
            .WithOne(t => t.Season)
            .HasForeignKey(t => t.SeasonId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.Competition)
            .WithMany(c => c.Seasons)
            .HasForeignKey(s => s.CompetitionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Table comment
        builder.ToTable("Seasons", tb => tb.HasComment("Football competition seasons"));
    }
}