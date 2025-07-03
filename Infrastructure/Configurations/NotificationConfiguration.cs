using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Content).IsRequired().HasMaxLength(512);
        builder.Property(n => n.Type).IsRequired();
        builder.Property(n => n.Time).IsRequired().HasDefaultValueSql("now()"); // Use SQL function to set default value to current UTC time
        builder.Property(n => n.IsRead).IsRequired().HasDefaultValue(false);
        builder.Property(n => n.UserId).IsRequired();
        builder.HasIndex(n => n.UserId); // Index for faster lookups by UserId
        builder
            .HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade); // Cascade delete if user is deleted
        builder.HasIndex(n => n.Time); // Index for faster lookups by Time
        builder.HasIndex(n => n.IsRead); // Index for faster lookups by IsRead status
        builder.HasIndex(n => n.Type); // Index for faster lookups by Type
        builder.Property(n => n.Id).HasDefaultValueSql("gen_random_uuid()"); // Use PostgreSQL's gen_random_uuid() for default value
    }
}
