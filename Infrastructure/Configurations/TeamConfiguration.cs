using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class TeamConfiguration : IEntityTypeConfiguration<Team>
{
    public void Configure(EntityTypeBuilder<Team> builder)
    {
        builder.HasKey(t => t.Id);
        
        // Use appropriate PostgreSQL types
        builder.Property(t => t.Name).IsRequired().HasMaxLength(100).HasColumnType("varchar(100)");
        builder.Property(t => t.ShortName).HasMaxLength(10).HasColumnType("varchar(10)");
        builder.Property(t => t.Logo).HasMaxLength(500).HasColumnType("varchar(500)");
        builder.Property(t => t.Country).HasMaxLength(50).HasColumnType("varchar(50)");
        builder.Property(t => t.City).HasMaxLength(100).HasColumnType("varchar(100)");
        builder.Property(t => t.PrimaryColor).HasMaxLength(20).HasColumnType("varchar(20)");
        builder.Property(t => t.SecondaryColor).HasMaxLength(20).HasColumnType("varchar(20)");
        
        
        builder.Property(t => t.FoundationDate).HasColumnType("date");
        
        // Optimized indexes - specify methods for PostgreSQL
        builder.HasIndex(t => t.Name)
            .IsUnique()
            .HasMethod("btree")
            .HasDatabaseName("IX_Team_Name");
            
        builder.HasIndex(t => t.ShortName)
            .IsUnique()
            .HasMethod("btree")
            .HasDatabaseName("IX_Team_ShortName");
            
        // Add composite index for common query patterns
        builder.HasIndex(t => new { t.Country, t.City })
            .HasMethod("btree")
            .HasDatabaseName("IX_Team_Location");
            
        // Relationships with optimized foreign key constraints
        builder.HasOne(t => t.Stadium)
            .WithMany(s => s.Teams)
            .HasForeignKey(t => t.StadiumId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);
            
        builder.HasMany(t => t.Players)
            .WithOne(p => p.Team)
            .HasForeignKey(p => p.TeamId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);
            
        builder.HasMany(t => t.Coaches)
            .WithOne(c => c.Team)
            .HasForeignKey(c => c.TeamId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);
        builder.HasMany(t => t.HomeMatches)
            .WithOne(m => m.HomeTeam)
            .HasForeignKey(m => m.HomeTeamId);
        builder.HasMany(t => t.AwayMatches)
            .WithOne(m => m.AwayTeam)
            .HasForeignKey(m => m.AwayTeamId);
            
        // Add table comments
        builder.ToTable("Teams", tb => tb.HasComment("Football teams information"));
    }
}
