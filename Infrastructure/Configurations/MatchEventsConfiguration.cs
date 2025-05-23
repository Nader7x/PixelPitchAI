using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class MatchEventsConfiguration : IEntityTypeConfiguration<MatchEvents>
{
    public void Configure(EntityTypeBuilder<MatchEvents> builder)
    {
        builder.HasKey(e => e.Id);
        
        // Use PostgreSQL-specific JSONB type for better performance and query capabilities
        builder.Property(e => e.EventsJson).HasColumnType("jsonb");
        builder.Property(e => e.LastUpdated).IsRequired()
            .HasColumnType("timestamp with time zone");
        
        // Add GIN index for efficient JSON searching with appropriate operator class
        builder.HasIndex(e => e.EventsJson)
            .HasMethod("gin")
            .HasDatabaseName("IX_MatchEvents_EventsJson");
            
        // Unique constraint - one event record per match
        builder.HasIndex(e => e.MatchId)
            .IsUnique()
            .HasDatabaseName("IX_MatchEvents_MatchId");
            
        // Add additional indexes for frequently queried fields
        builder.HasIndex(e => new { e.GoalsHomeTeam, e.GoalsAwayTeam })
            .HasDatabaseName("IX_MatchEvents_Goals");

        builder.HasIndex(e => e.LastUpdated)
            .HasMethod("btree")
            .HasDatabaseName("IX_MatchEvents_LastUpdated");
            
        // Set storage parameters for the large table
        builder.ToTable("MatchEvents", tb => tb.HasComment("Contains event data for matches"));
    }
}

