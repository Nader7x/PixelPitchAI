using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class CoachConfiguration : IEntityTypeConfiguration<Coach>
{
    public void Configure(EntityTypeBuilder<Coach> builder)
    {
        builder.HasKey(c => c.Id);

        // Use PostgreSQL-specific column types
        builder
            .Property(c => c.FirstName)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnType("varchar(100)");
        builder
            .Property(c => c.LastName)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnType("varchar(100)");
        builder.Property(c => c.Nationality).HasMaxLength(50).HasColumnType("varchar(50)");
        builder.Property(c => c.Role).HasMaxLength(50).HasColumnType("varchar(50)");

        // Ensure date fields use timestamp
        if (builder.Metadata.FindProperty("DateOfBirth") != null)
            builder.Property("DateOfBirth").HasColumnType("timestamp with time zone");

        // Use properly named indexes with appropriate methods for search patterns
        builder
            .HasIndex(c => new { c.FirstName, c.LastName })
            .HasMethod("btree")
            .HasDatabaseName("IX_Coach_Name"); // Add practical indexes for common queries
        builder
            .HasIndex(c => c.Nationality)
            .HasMethod("btree")
            .HasDatabaseName("IX_Coach_Nationality");

        // Note: PostgreSQL full-text search indexes will be created via raw SQL in migration
        // as EF Core doesn't support tsvector expressions in HasIndex
        builder.Property(c => c.DateOfBirth).HasColumnType("date");

        // If TeamId exists in the entity
        if (builder.Metadata.FindProperty("TeamId") != null)
            builder.HasIndex("TeamId").HasDatabaseName("IX_Coach_TeamId");

        // Table comment
        builder.ToTable("Coaches", tb => tb.HasComment("Football coaches information"));
    }
}
