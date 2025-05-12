using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class TeamSeasonStatsConfiguration : IEntityTypeConfiguration<TeamSeasonStats>
{
    public void Configure(EntityTypeBuilder<TeamSeasonStats> builder)
    {
        builder.HasKey(t => t.Id);
        
        // Use appropriate PostgreSQL types for string properties
        builder.Property(t => t.Form).HasMaxLength(20).HasColumnType("varchar(20)");
        
        // Use numeric with appropriate precision for decimal values
        builder.Property(t => t.AveragePossession).HasPrecision(5, 2);
        builder.Property(t => t.PassAccuracy).HasPrecision(5, 2);
        builder.Property(t => t.ConversionRate).HasPrecision(5, 2);
        builder.Property(t => t.ExpectedGoals).HasPrecision(6, 2);
        builder.Property(t => t.ExpectedGoalsAgainst).HasPrecision(6, 2);
        
        // Set timestamp for UpdatedAt
        builder.Property(t => t.UpdatedAt)
            .IsRequired()
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()");
        
        // Unique constraint with appropriate index
        builder.HasIndex(t => new { t.TeamId, t.SeasonId })
            .IsUnique()
            .HasMethod("btree")
            .HasDatabaseName("IX_TeamSeasonStats_TeamSeason");
        
        // Create a composite index for standings queries
        // This is a critical performance index for league tables
        builder.HasIndex(t => new { t.SeasonId, t.Points, t.GoalDifference, t.GoalsScored })
            .HasMethod("btree")
            .HasDatabaseName("IX_TeamSeasonStats_Standings");
            
        // Additional indexes for common queries
        builder.HasIndex(t => new { t.SeasonId, t.Position })
            .HasMethod("btree")
            .HasDatabaseName("IX_TeamSeasonStats_Position");
            
        // Create index on home/away performance for filtered queries
        builder.HasIndex(t => new { t.SeasonId, t.HomeWins, t.HomeDraws, t.HomeLosses })
            .HasMethod("btree")
            .HasDatabaseName("IX_TeamSeasonStats_HomePerformance");
            
        builder.HasIndex(t => new { t.SeasonId, t.AwayWins, t.AwayDraws, t.AwayLosses })
            .HasMethod("btree")
            .HasDatabaseName("IX_TeamSeasonStats_AwayPerformance");
            
        // Relationships
        builder.HasOne(t => t.Team)
            .WithMany()
            .HasForeignKey(t => t.TeamId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_TeamSeasonStats_Team");
            
        builder.HasOne(t => t.Season)
            .WithMany(s => s.TeamSeasonStats)
            .HasForeignKey(t => t.SeasonId)
            .OnDelete(DeleteBehavior.Cascade);
            
        // Table comment
        builder.ToTable("TeamSeasonStats", tb => tb.HasComment("Team statistics by season"));
    }
}

