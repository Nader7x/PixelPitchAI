using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class TeamSeasonsConfiguration : IEntityTypeConfiguration<TeamSeasons>
{
    public void Configure(EntityTypeBuilder<TeamSeasons> builder)
    {
        builder.HasKey(t => t.Id);

        // Set timestamp for UpdatedAt
        builder.Property(t => t.UpdatedAt)
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()");

        // Unique constraint with appropriate index
        builder.HasIndex(t => new { t.TeamId, t.SeasonId })
            .IsUnique()
            .HasMethod("btree")
            .HasDatabaseName("IX_TeamSeasons_TeamSeason");

        // Relationships
        builder.HasOne(t => t.Team)
            .WithMany(s => s.TeamSeasons) // Specify the collection in Team class
            .HasForeignKey(t => t.TeamId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_TeamSeasons_Team");

        builder.HasOne(t => t.Season)
            .WithMany(s => s.SeasonTeams)
            .HasForeignKey(t => t.SeasonId)
            .OnDelete(DeleteBehavior.Cascade);

        // Table comment
        builder.ToTable("TeamSeasons", tb => tb.HasComment("Team seasons table"));
    }
}