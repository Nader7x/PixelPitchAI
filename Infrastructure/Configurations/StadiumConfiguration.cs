using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class StadiumConfiguration : IEntityTypeConfiguration<Stadium>
{
    public void Configure(EntityTypeBuilder<Stadium> builder)
    {
        builder.HasKey(s => s.Id);
        
        // Use citext for case-insensitive search optimization
        builder.Property(s => s.Name).IsRequired().HasMaxLength(100).HasColumnType("varchar(100)");
        builder.Property(s => s.City).HasMaxLength(100).HasColumnType("varchar(100)");
        builder.Property(s => s.Country).HasMaxLength(50).HasColumnType("varchar(50)");
        builder.Property(s => s.SurfaceType).HasMaxLength(50).HasColumnType("varchar(50)");
        builder.Property(s => s.Address).HasMaxLength(200).HasColumnType("varchar(200)");
        builder.Property(s => s.ImageUrl).HasMaxLength(500).HasColumnType("varchar(500)");
        builder.Property(s => s.Description).HasMaxLength(1000).HasColumnType("text");
        
        // Use JSONB for structured data storage with indexing capability
        builder.Property(s => s.Facilities).HasColumnType("jsonb");
        
        // Precise numeric values with PostgreSQL
        builder.Property(s => s.Latitude).HasPrecision(9, 6);
        builder.Property(s => s.Longitude).HasPrecision(9, 6);
        
        // Use B-tree indexes for exact matches
        builder.HasIndex(s => s.Name)
            .HasMethod("btree")
            .HasDatabaseName("IX_Stadium_Name");
            
        builder.HasIndex(s => new { s.City, s.Country })
            .HasMethod("btree")
            .HasDatabaseName("IX_Stadium_Location");
            
        // Add JSONB path operation index for facilities searches
        builder.HasIndex(s => s.Facilities)
            .HasMethod("gin")
            .HasOperators("jsonb_path_ops")
            .HasDatabaseName("IX_Stadium_Facilities");
            
        // Set table comment for documentation
        builder.ToTable("Stadiums", tb => tb.HasComment("Stadium information"));
    }
}
