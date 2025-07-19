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

        builder.Property(m => m.HomeTeamInMatchName).HasMaxLength(500);
        builder.Property(m => m.AwayTeamInMatchName).HasMaxLength(500);

        // Use timestamp for date fields
        builder.Property(m => m.ScheduledDateTimeUtc).HasColumnType("timestamp with time zone");

        builder
            .Property(m => m.CreatedAt)
            .IsRequired()
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()");

        builder
            .Property(m => m.UpdatedAt)
            .IsRequired()
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()");

        // Create indexes for common queries

        // Find matches by date
        builder
            .HasIndex(m => m.ScheduledDateTimeUtc)
            .HasMethod("btree")
            .HasDatabaseName("IX_Match_KickoffTime");

        // Find matches by team
        builder
            .HasIndex(m => m.HomeTeamId)
            .HasMethod("btree")
            .HasDatabaseName("IX_Match_HomeTeam");

        builder.HasIndex(m => m.AwayTeamId).HasMethod("btree").HasDatabaseName("IX_Match_AwayTeam");

        // Find matches by status (e.g., upcoming, live, completed)
        builder.HasIndex(m => m.MatchStatus).HasMethod("btree").HasDatabaseName("IX_Match_Status");

        // Add index for CreatorId
        builder
            .HasIndex(m => m.CreatorId)
            .HasMethod("btree")
            .HasDatabaseName("IX_Match_CreatorId");

        // Add index for StadiumId
        builder
            .HasIndex(m => m.StadiumId)
            .HasMethod("btree")
            .HasDatabaseName("IX_Match_StadiumId");

        // Add composite index for match search optimization
        builder
            .HasIndex(m => new
            {
                m.HomeTeamId,
                m.AwayTeamId,
                m.ScheduledDateTimeUtc,
            })
            .HasMethod("btree")
            .HasDatabaseName("IX_Match_Teams_Date");

        // Relationships

        builder
            .HasOne(m => m.HomeTeam)
            .WithMany(t => t.HomeMatches) // Make sure to specify the correct collection property
            .HasForeignKey(m => m.HomeTeamId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_Match_HomeTeam");

        builder
            .HasOne(m => m.AwayTeam)
            .WithMany(t => t.AwayMatches) // Make sure to specify the correct collection property
            .HasForeignKey(m => m.AwayTeamId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_Match_AwayTeam");

        builder
            .HasOne(m => m.HomeTeamSeason)
            .WithMany()
            .HasForeignKey(m => m.HomeTeamSeasonId)
            .OnDelete(DeleteBehavior.Cascade);
        builder
            .HasOne(m => m.AwayTeamSeason)
            .WithMany()
            .HasForeignKey(m => m.AwayTeamSeasonId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(m => m.Stadium)
            .WithMany()
            .HasForeignKey(m => m.StadiumId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        // One-to-one relationship with match events
        builder
            .HasOne(m => m.MatchEvents)
            .WithOne(e => e.Match)
            .HasForeignKey<MatchEvents>(e => e.MatchId)
            .OnDelete(DeleteBehavior.Cascade);
        // One-to-one relationship with match statistics
        builder
            .HasOne(m => m.MatchStatistics)
            .WithOne(s => s.Match)
            .HasForeignKey<MatchStatistics>(s => s.MatchId)
            .OnDelete(DeleteBehavior.Cascade);

        // Table comment
        builder.ToTable("Matches", tb => tb.HasComment("Football match information"));
    }
}
