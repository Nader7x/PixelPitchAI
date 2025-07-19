using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class PlayerConfiguration : IEntityTypeConfiguration<Player>
{
    public void Configure(EntityTypeBuilder<Player> builder)
    {
        builder.HasKey(p => p.Id);

        // Use appropriate PostgresSQL types
        builder
            .Property(p => p.FullName)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnType("varchar(100)");
        builder
            .Property(p => p.KnownName)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnType("varchar(100)");
        builder.Property(p => p.Nationality).HasMaxLength(50).HasColumnType("varchar(50)");
        builder.Property(p => p.ShirtNumber).HasColumnType("smallint"); // Use a smaller int type
        builder.Property(p => p.Nationality).HasMaxLength(50);
        builder.Property(p => p.PreferredFoot).HasMaxLength(20);
        builder.Property(p => p.PhotoUrl).HasMaxLength(500);
        builder
            .Property(p => p.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW() AT TIME ZONE 'UTC'");
        builder
            .Property(p => p.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW() AT TIME ZONE 'UTC'");

        // If you have image or photo URLs
        if (builder.Metadata.FindProperty("PhotoUrl") != null)
            builder.Property("PhotoUrl").HasMaxLength(500).HasColumnType("varchar(500)");

        // Use timestamp for date fields
        if (builder.Metadata.FindProperty("DateOfBirth") != null)
            builder.Property("DateOfBirth").HasColumnType("timestamp with time zone");

        // Add indexes for common queries
        builder
            .HasIndex(p => new { p.FullName, p.KnownName })
            .HasMethod("btree")
            .HasDatabaseName("IX_Player_Name");

        builder
            .HasIndex(p => p.Nationality)
            .HasMethod("btree")
            .HasDatabaseName("IX_Player_Nationality");
        builder.HasIndex(p => p.FullName);

        // Add team index for filtering players by team
        builder.HasIndex(p => p.TeamId).HasMethod("btree").HasDatabaseName("IX_Player_TeamId");

        // Note: PostgreSQL full-text search indexes will be created via raw SQL in migration
        // as EF Core doesn't support tsvector expressions in HasIndex

        // Relationships
        // (Assuming there's a Team relationship)
        builder
            .HasOne(p => p.Team)
            .WithMany(t => t.Players)
            .HasForeignKey(p => p.TeamId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);
        // Table comment
        builder.ToTable("Players", tb => tb.HasComment("Football players information"));
    }
}
