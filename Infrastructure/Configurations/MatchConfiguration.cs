using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class MatchConfiguration : IEntityTypeConfiguration<Match>
{
    public void Configure(EntityTypeBuilder<Match> builder)
    {
        builder.HasKey(m => m.Id);
        
        // Use PostgreSQL-specific types
        builder.Property(m => m.MatchWeek).HasColumnType("smallint");
        builder.Property(m => m.HomeTeamScore).HasColumnType("smallint");
        builder.Property(m => m.AwayTeamScore).HasColumnType("smallint");
        builder.Property(m => m.MatchStatus).HasMaxLength(20).HasColumnType("varchar(20)");
        
        // Use timestamp for date fields
        builder.Property(m => m.ScheduledDateTimeUTC)
            .HasColumnType("timestamp with time zone");
            
        builder.Property(m => m.CreatedAt)
            .IsRequired()
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()");
            
        builder.Property(m => m.UpdatedAt)
            .IsRequired()
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()");
            
        // Create indexes for common queries
        
        // Find matches by season and round
        builder.HasIndex(m => new { m.SeasonId, m.MatchWeek })
            .HasMethod("btree")
            .HasDatabaseName("IX_Match_SeasonRound");
            
        // Find matches by date
        builder.HasIndex(m => m.ScheduledDateTimeUTC)
            .HasMethod("btree")
            .HasDatabaseName("IX_Match_KickoffTime");
            
        // Find matches by team
        builder.HasIndex(m => m.HomeTeamId)
            .HasMethod("btree")
            .HasDatabaseName("IX_Match_HomeTeam");
            
        builder.HasIndex(m => m.AwayTeamId)
            .HasMethod("btree")
            .HasDatabaseName("IX_Match_AwayTeam");
            
        // Find matches by status (e.g., upcoming, live, completed)
        builder.HasIndex(m => m.MatchStatus)
            .HasMethod("btree")
            .HasDatabaseName("IX_Match_Status");
            
        // Combined index for teams and season
        builder.HasIndex(m => new { m.HomeTeamId, m.AwayTeamId, m.SeasonId })
            .HasMethod("btree")
            .HasDatabaseName("IX_Match_Teams_Season");
            
        // Relationships
        builder.HasOne(m => m.HomeTeam)
            .WithMany()
            .HasForeignKey(m => m.HomeTeamId)
            .OnDelete(DeleteBehavior.Restrict);
            
        builder.HasOne(m => m.AwayTeam)
            .WithMany()
            .HasForeignKey(m => m.AwayTeamId)
            .OnDelete(DeleteBehavior.Restrict);
            
        builder.HasOne(m => m.Season)
            .WithMany(s => s.Matches)
            .HasForeignKey(m => m.SeasonId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasOne(m => m.Stadium)
            .WithMany()
            .HasForeignKey(m => m.StadiumId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);
            
        // One-to-one relationship with match events
        builder.HasOne(m => m.MatchEvents)
            .WithOne(e => e.Match)
            .HasForeignKey<MatchEvents>(e => e.MatchId)
            .OnDelete(DeleteBehavior.Cascade);
            
        // Table comment
        builder.ToTable("Matches", tb => tb.HasComment("Football match information"));
    }
}

