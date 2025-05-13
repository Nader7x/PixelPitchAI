using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class PlayerSeasonStatsConfiguration : IEntityTypeConfiguration<PlayerSeasonStats>
{
    public void Configure(EntityTypeBuilder<PlayerSeasonStats> builder)
    {
        builder.HasKey(p => p.Id);
        
        // Numeric precision for statistical fields
        builder.Property(p => p.PassCompletionRate).HasPrecision(5, 2);
        
        // Add nullable flag for Rating
        builder.Property(p => p.Rating).HasPrecision(3, 1).IsRequired(false);
        
        // Composite unique index to ensure a player only has one stat record per season/team
        builder.HasIndex(p => new { p.PlayerId, p.SeasonId, p.TeamId })
            .IsUnique()
            .HasMethod("btree")
            .HasFilter("\"TeamId\" IS NOT NULL")
            .HasDatabaseName("IX_PlayerSeasonStats_PlayerSeason");
        
        // Indexes for common stat queries
        builder.HasIndex(p => new { p.SeasonId, p.Goals })
            .HasMethod("btree")
            .HasDatabaseName("IX_PlayerSeasonStats_Goals");
            
        builder.HasIndex(p => new { p.SeasonId, p.Assists })
            .HasMethod("btree")
            .HasDatabaseName("IX_PlayerSeasonStats_Assists");
            
        // Index for finding top players by rating
        builder.HasIndex(p => new { p.SeasonId, p.Rating })
            .HasMethod("btree")
            .HasDatabaseName("IX_PlayerSeasonStats_Rating")
            .HasFilter("\"Rating\" IS NOT NULL"); // Partial index to exclude NULL ratings
            
        // Relationships
// In PlayerSeasonStatsConfiguration.cs
        builder.HasOne(p => p.Player)
            .WithMany(player => player.PlayerSeasonStats)  // Specify the collection in Player class
            .HasForeignKey(p => p.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasOne(p => p.Season)
            .WithMany(s => s.PlayerSeasonStats)
            .HasForeignKey(p => p.SeasonId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasOne(p => p.Team)
            .WithMany(t => t.PlayerSeasonStats)
            .HasForeignKey(p => p.TeamId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);
        
        // Table comment
        builder.ToTable("PlayerSeasonStats", tb => tb.HasComment("Player statistics by season"));
    }
}
