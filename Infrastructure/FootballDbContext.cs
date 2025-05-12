using Domain.Models;
using Infrastructure.Configurations;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace Infrastructure;

public class FootballDbContext : DbContext
{
    public FootballDbContext(DbContextOptions<FootballDbContext> options) : base(options)
    {
    }

    public DbSet<Match> Matches { get; set; }
    public DbSet<MatchEvents> MatchEvents { get; set; }
    public DbSet<Team> Teams { get; set; }
    public DbSet<Player> Players { get; set; }
    public DbSet<Coach> Coaches { get; set; }
    public DbSet<Stadium> Stadiums { get; set; }
    public DbSet<Season> Seasons { get; set; }
    public DbSet<TeamSeasonStats> TeamSeasonStats { get; set; }
    public DbSet<PlayerSeasonStats> PlayerSeasonStats { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all configurations from the Configurations namespace
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
