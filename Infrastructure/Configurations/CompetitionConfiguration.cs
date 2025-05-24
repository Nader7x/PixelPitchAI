using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class CompetitionConfiguration : IEntityTypeConfiguration<Competition>
{
    public void Configure(EntityTypeBuilder<Competition> builder)
    {
        builder.HasKey(c => c.Id);

        // Use PostgreSQL-specific column types
        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(200)
            .HasColumnType("varchar(200)");

        builder.Property(c => c.Description)
            .HasMaxLength(500)
            .HasColumnType("varchar(500)");
        builder.Property(c => c.Logo)
            .HasMaxLength(500)
            .HasColumnType("varchar(500)");
        builder.Property(c => c.Country)
            .HasMaxLength(100)
            .HasColumnType("varchar(100)");

        // Configure the relationship with Season
        builder.HasMany(c => c.Seasons)
            .WithOne(s => s.Competition)
            .HasForeignKey(s => s.CompetitionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Table comment
        builder.ToTable("Competitions", tb => tb.HasComment("Football competitions information"));
    }
}