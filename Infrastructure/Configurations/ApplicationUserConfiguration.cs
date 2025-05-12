using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        
        // Base properties customization
        builder.Property(u => u.UserName).HasMaxLength(256).HasColumnType("varchar(256)");
        builder.Property(u => u.NormalizedUserName).HasMaxLength(256).HasColumnType("varchar(256)");
        builder.Property(u => u.Email).HasMaxLength(256).HasColumnType("varchar(256)");
        builder.Property(u => u.NormalizedEmail).HasMaxLength(256).HasColumnType("varchar(256)");
        
        // Custom properties
        builder.Property(u => u.FirstName).HasMaxLength(100).HasColumnType("varchar(100)");
        builder.Property(u => u.LastName).HasMaxLength(100).HasColumnType("varchar(100)");
        builder.Property(u => u.Created).HasColumnType("timestamp with time zone");
        builder.Property(u => u.LastLogin).HasColumnType("timestamp with time zone");
        builder.Property(u => u.IsActive).HasColumnType("boolean").HasDefaultValue(true);
        
        // Optimize indexes for PostgreSQL
        builder.HasIndex(u => u.NormalizedUserName)
            .HasMethod("btree")
            .HasDatabaseName("IX_ApplicationUser_UserName")
            .IsUnique();
            
        builder.HasIndex(u => u.NormalizedEmail)
            .HasMethod("btree")
            .HasDatabaseName("IX_ApplicationUser_Email");
            
        // Add index for favorite team queries
        builder.HasIndex(u => u.FavoriteTeamId)
            .HasMethod("btree")
            .HasDatabaseName("IX_ApplicationUser_FavoriteTeam");
            
        // Relationship with favorite team
        builder.HasOne(u => u.FavoriteTeam)
            .WithMany()
            .HasForeignKey(u => u.FavoriteTeamId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);
            
        // Table comment
        builder.ToTable("Users", tb => tb.HasComment("Application users with custom profile data"));
    }
}
