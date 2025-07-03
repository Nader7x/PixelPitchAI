using AutoFixture;
using Domain.Models;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Footex.UnitTests.Common;

public class TestFixtureBase : IDisposable
{
    protected readonly FootballDbContext Context;
    protected readonly Fixture Fixture;
    protected readonly IServiceProvider ServiceProvider;

    public TestFixtureBase()
    {
        Fixture = new NoRecursionFixture();
        Fixture.Customizations.Add(new IFormFileSpecimenBuilder());

        var services = new ServiceCollection();

        services.AddDbContext<FootballDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString())
        );

        services.AddLogging();

        ServiceProvider = services.BuildServiceProvider();
        Context = ServiceProvider.GetRequiredService<FootballDbContext>();

        // Ensure database is created
        Context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        Context?.Dispose();
        ServiceProvider?.GetService<IServiceScope>()?.Dispose();
    }

    protected async Task<Team> CreateTestTeamAsync(string? name = null)
    {
        var team = new Team
        {
            Name = name ?? Fixture.Create<string>(),
            FoundationDate = Fixture.Create<DateTime>(),
            Country = Fixture.Create<string>(),
            City = Fixture.Create<string>(),
            Logo = Fixture.Create<string>(),
        };

        Context.Teams.Add(team);
        await Context.SaveChangesAsync();
        return team;
    }

    protected async Task<Season> CreateTestSeasonAsync(string? name = null)
    {
        var season = new Season
        {
            Name = name ?? "2023/24",
            StartDate = DateTime.UtcNow.AddMonths(-6),
            EndDate = DateTime.UtcNow.AddMonths(6),
            Country = "England",
            LeagueName = "Premier League",
        };

        Context.Seasons.Add(season);
        await Context.SaveChangesAsync();
        return season;
    }

    protected async Task<Match> CreateTestMatchAsync(
        int? homeTeamId = null,
        int? awayTeamId = null,
        int? seasonId = null
    )
    {
        var homeTeam = homeTeamId.HasValue
            ? await Context.Teams.FindAsync(homeTeamId.Value)
            : await CreateTestTeamAsync();

        var awayTeam = awayTeamId.HasValue
            ? await Context.Teams.FindAsync(awayTeamId.Value)
            : await CreateTestTeamAsync();

        var season = seasonId.HasValue
            ? await Context.Seasons.FindAsync(seasonId.Value)
            : await CreateTestSeasonAsync();

        var match = new Match
        {
            HomeTeamId = homeTeam!.Id,
            AwayTeamId = awayTeam!.Id,
            HomeTeamSeasonId = season!.Id,
            AwayTeamSeasonId = season.Id,
            ScheduledDateTimeUtc = DateTime.UtcNow.AddDays(7),
            MatchStatus = "Scheduled",
            CreatorId = Guid.NewGuid().ToString(),
            HomeTeamInMatchName = $"{homeTeam.Name}_2024",
            AwayTeamInMatchName = $"{awayTeam.Name}_2024",
        };

        Context.Matches.Add(match);
        await Context.SaveChangesAsync();
        return match;
    }
}
