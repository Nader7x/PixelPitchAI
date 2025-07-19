using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class MatchStatisticsConfiguration : IEntityTypeConfiguration<MatchStatistics>
{
    public void Configure(EntityTypeBuilder<MatchStatistics> builder)
    {
        builder.HasKey(s => s.Id);
        builder
            .HasOne(s => s.Match)
            .WithOne(m => m.MatchStatistics)
            .HasForeignKey<MatchStatistics>(s => s.MatchId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(s => s.MatchId).IsUnique().HasDatabaseName("IX_MatchStatistics_MatchId");
        builder.Property(s => s.HomeTeamPossessionDurationSeconds).HasColumnType("bigint");
        builder.Property(s => s.AwayTeamPossessionDurationSeconds).HasColumnType("bigint");
        builder.Property(s => s.LastEventPossessingTeamName).HasMaxLength(500);
        builder.Property(s => s.LastEventPossessingTeamName).HasMaxLength(500);
    }
}
