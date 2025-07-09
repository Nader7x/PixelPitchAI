using System.Reflection;
using Domain.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure;

public class FootballDbContext(DbContextOptions<FootballDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public virtual DbSet<Match> Matches { get; set; }
    public virtual DbSet<MatchEvents> MatchEvents { get; set; }
    public virtual DbSet<Team> Teams { get; set; }
    public virtual DbSet<Player> Players { get; set; }
    public virtual DbSet<Coach> Coaches { get; set; }
    public virtual DbSet<Competition> Competitions { get; set; }
    public virtual DbSet<Stadium> Stadiums { get; set; }
    public virtual DbSet<Season> Seasons { get; set; }
    public virtual DbSet<TeamSeason> TeamSeasons { get; set; }
    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }
    public virtual DbSet<Notification> Notifications { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all configurations from the Configurations namespace
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // Configure identity tables - customize table names
        modelBuilder.Entity<ApplicationUser>().ToTable("Users");
        modelBuilder.Entity<IdentityRole>().ToTable("Roles");
        modelBuilder.Entity<IdentityUserRole<string>>().ToTable("UserRoles");
        modelBuilder.Entity<IdentityUserClaim<string>>().ToTable("UserClaims");
        modelBuilder.Entity<IdentityUserLogin<string>>().ToTable("UserLogins");
        modelBuilder.Entity<IdentityRoleClaim<string>>().ToTable("RoleClaims");
        modelBuilder.Entity<IdentityUserToken<string>>().ToTable("UserTokens");
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        // Query splitting should be configured at the provider level in Program.cs
        // or can be set per query using AsSplitQuery()
    }
}
