using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.HasKey(r => r.Id);
        
        // Use appropriate PostgreSQL types
        builder.Property(r => r.Token).IsRequired().HasMaxLength(256).HasColumnType("varchar(256)");
        builder.Property(r => r.JwtId).IsRequired().HasMaxLength(128).HasColumnType("varchar(128)");
        builder.Property(r => r.CreatedByIp).HasMaxLength(50).HasColumnType("varchar(50)");
        builder.Property(r => r.RevokedByIp).HasMaxLength(50).HasColumnType("varchar(50)");
        
        // Use timestamp for date fields
        builder.Property(r => r.Created)
            .IsRequired()
            .HasColumnType("timestamp with time zone");
            
        builder.Property(r => r.Expires)
            .IsRequired()
            .HasColumnType("timestamp with time zone");
            
        builder.Property(r => r.Revoked)
            .HasColumnType("timestamp with time zone");
            
        // Create indexes for common queries
        builder.HasIndex(r => r.Token)
            .IsUnique()
            .HasMethod("btree")
            .HasDatabaseName("IX_RefreshToken_Token");
            
        // Create index for active tokens without using now()
        builder.HasIndex(r => new { r.UserId, r.Revoked, r.Expires })
            .HasMethod("btree")
            .HasDatabaseName("IX_RefreshToken_Active")
            .HasFilter("\"Revoked\" IS NULL");
            
        // Relationships
        builder.HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);
            
        // Table comment
        builder.ToTable("RefreshTokens", tb => tb.HasComment("User refresh tokens"));
    }
}
