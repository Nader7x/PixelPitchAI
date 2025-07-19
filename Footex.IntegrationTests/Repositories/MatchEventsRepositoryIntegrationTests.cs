using System.Text.Json;
using Domain.Models;
using Domain.Repositories;
using FluentAssertions;
using Footex.IntegrationTests.Common;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Footex.IntegrationTests.Repositories;

public class MatchEventsRepositoryIntegrationTests : BaseIntegrationTest
{
    private readonly IMatchEventsRepository _matchEventsRepository;

    public MatchEventsRepositoryIntegrationTests(FootexWebApplicationFactory factory)
        : base(factory)
    {
        _matchEventsRepository =
            FactoryServiceScope.ServiceProvider.GetRequiredService<IMatchEventsRepository>();
    }

    [Fact]
    public async Task AddAsync_ShouldAddMatchEventsSuccessfully()
    {
        // Arrange
        var match = await SeedMatchAsync();
        var matchEvents = CreateValidMatchEvents(match.Id);

        // Act
        var entityEntry = await _matchEventsRepository.AddAsync(matchEvents);
        await Context.SaveChangesAsync();

        var result = entityEntry.Entity;

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.MatchId.Should().Be(match.Id);
        result.EventsJson.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetByIdAsync_WhenMatchEventsExists_ShouldReturnMatchEvents()
    {
        // Arrange
        var matchEventsEntity = await SeedMatchEventsAsync();
        await Context.SaveChangesAsync();
        var matchEvents = matchEventsEntity.Entity;

        // Act
        var result = await _matchEventsRepository.GetByIdAsync(matchEvents.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(matchEvents.Id);
        result.MatchId.Should().Be(matchEvents.MatchId);
        result.GoalsHomeTeam.Should().Be(matchEvents.GoalsHomeTeam);
        result.GoalsAwayTeam.Should().Be(matchEvents.GoalsAwayTeam);
    }

    [Fact]
    public async Task GetByIdAsync_WhenMatchEventsDoesNotExist_ShouldReturnNull()
    {
        // Act
        var result = await _matchEventsRepository.GetByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByMatchIdAsync_WhenMatchEventsExists_ShouldReturnMatchEventsWithMatch()
    {
        // Arrange
        var matchEventsEntity = await SeedMatchEventsAsync();
        await Context.SaveChangesAsync();

        var matchEvents = matchEventsEntity.Entity;

        // Act
        var result = await _matchEventsRepository.GetByMatchIdAsync(matchEvents.MatchId);

        // Assert
        result.Should().NotBeNull();
        result!.MatchId.Should().Be(matchEvents.MatchId);
        result.Match.Should().NotBeNull();
        result.Match!.Id.Should().Be(matchEvents.MatchId);
    }

    [Fact]
    public async Task GetByMatchIdAsync_WhenMatchEventsDoesNotExist_ShouldReturnNull()
    {
        // Act
        var result = await _matchEventsRepository.GetByMatchIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateMatchEventsSuccessfully()
    {
        // Arrange
        var matchEventsEntity = await SeedMatchEventsAsync();
        await Context.SaveChangesAsync();
        var matchEvents = matchEventsEntity.Entity;
        const int updatedGoalsHome = 3;
        const int updatedGoalsAway = 1;
        const int updatedTotalEvents = 25;

        // Act
        matchEvents.GoalsHomeTeam = updatedGoalsHome;
        matchEvents.GoalsAwayTeam = updatedGoalsAway;
        matchEvents.TotalEvents = updatedTotalEvents;
        var entityEntry = _matchEventsRepository.Update(matchEvents);
        await Context.SaveChangesAsync();

        var result = entityEntry.Entity;

        // Assert
        result.Should().NotBeNull();
        result.GoalsHomeTeam.Should().Be(updatedGoalsHome);
        result.GoalsAwayTeam.Should().Be(updatedGoalsAway);
        result.TotalEvents.Should().Be(updatedTotalEvents);
    }

    [Fact]
    public async Task DeleteAsync_ShouldDeleteMatchEventsSuccessfully()
    {
        // Arrange
        var matchEventsEntity = await SeedMatchEventsAsync();

        var matchEvents = matchEventsEntity.Entity;

        // Act
        _matchEventsRepository.Delete(matchEvents);

        // Assert
        var deletedMatchEvents = await _matchEventsRepository.GetByIdAsync(matchEvents.Id);
        deletedMatchEvents.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllMatchEvents()
    {
        // Arrange
        var matchEvents1Entity = await SeedMatchEventsAsync();
        var matchEvents2Entity = await SeedMatchEventsAsync();
        await Context.SaveChangesAsync();
        var matchEvents1 = matchEvents1Entity.Entity;
        var matchEvents2 = matchEvents2Entity.Entity;

        // Act
        var result = await _matchEventsRepository.GetAllAsync();

        // Assert
        var matchEventsEnumerable = result as MatchEvents[] ?? result.ToArray();
        matchEventsEnumerable.Should().NotBeNull();
        matchEventsEnumerable.Should().HaveCountGreaterOrEqualTo(2);
        matchEventsEnumerable.Should().Contain(me => me.Id == matchEvents1.Id);
        matchEventsEnumerable.Should().Contain(me => me.Id == matchEvents2.Id);
    }

    [Fact]
    public async Task GetAllAsync_WhenNoMatchEvents_ShouldReturnEmptyList()
    {
        // Act
        var result = await _matchEventsRepository.GetAllAsync();

        // Assert
        var matchEventsEnumerable = result as MatchEvents[] ?? result.ToArray();
        matchEventsEnumerable.Should().NotBeNull();
        matchEventsEnumerable.Should().BeEmpty();
    }

    [Fact]
    public async Task MatchEvents_ShouldHandleJsonEventsSerialization()
    {
        // Arrange
        var match = await SeedMatchAsync();
        var events = new[]
        {
            new
            {
                Type = "Goal",
                Time = 15,
                Player = "Player1",
                Team = "Home",
            },
            new
            {
                Type = "Yellow Card",
                Time = 23,
                Player = "Player2",
                Team = "Away",
            },
            new
            {
                Type = "Goal",
                Time = 67,
                Player = "Player3",
                Team = "Home",
            },
        };

        var matchEvents = CreateValidMatchEvents(match.Id);
        matchEvents.SetEvents(events);

        // Act
        var entityEntry = await _matchEventsRepository.AddAsync(matchEvents);
        await Context.SaveChangesAsync();

        var result = entityEntry.Entity;

        // Assert
        result.Should().NotBeNull();
        result.EventsJson.Should().NotBeNullOrEmpty();

        var deserializedEvents = result.GetEvents<object[]>();
        deserializedEvents.Should().NotBeNull();
        deserializedEvents.Should().HaveCount(3);
    }

    [Fact]
    public async Task MatchEvents_ShouldUpdateLastUpdatedWhenSettingEvents()
    {
        // Arrange
        var matchEventsEntity = await SeedMatchEventsAsync();
        await Context.SaveChangesAsync();

        var matchEvents = matchEventsEntity.Entity;
        var originalLastUpdated = matchEvents.LastUpdated;
        await Task.Delay(1000); // Ensure time difference

        var newEvents = new[]
        {
            new
            {
                Type = "Goal",
                Time = 45,
                Player = "TestPlayer",
            },
        };

        // Act
        matchEvents.SetEvents(newEvents);
        var entityEntry = _matchEventsRepository.Update(matchEvents);
        await Context.SaveChangesAsync();

        var result = entityEntry.Entity;

        // Assert
        result.LastUpdated.Should().BeAfter(originalLastUpdated);
    }

    [Fact]
    public async Task MatchEvents_ShouldHandleAllEventCounters()
    {
        // Arrange
        var match = await SeedMatchAsync();
        var matchEvents = new MatchEvents
        {
            MatchId = match.Id,
            EventsJson = "[]",
            LastUpdated = DateTime.UtcNow,
            GoalsHomeTeam = 2,
            GoalsAwayTeam = 1,
            TotalEvents = 45,
            TotalShots = 12,
            TotalPasses = 345,
            TotalFouls = 8,
            TotalCards = 3,
            TotalYellowCards = 2,
            TotalRedCards = 1,
            TotalOffsides = 4,
            TotalCorners = 6,
            TotalSubstitutions = 5,
            TotalInjuries = 1,
            TotalPenalties = 1,
            TotalThrowIns = 15,
            TotalOuts = 8,
            TotalGoals = 3,
            TotalGoalKicks = 7,
            TotalGoalkeeperSaves = 9,
            TotalDribbles = 23,
            TotalPossessionWon = 67,
            TotalFreeKicks = 5,
            TotalDuels = 89,
            TotalErrors = 3,
            TotalBlocks = 12,
            TotalClearances = 18,
            TotalInterceptions = 14,
        };

        // Act
        var entityEntry = await _matchEventsRepository.AddAsync(matchEvents);
        await Context.SaveChangesAsync();

        var result = entityEntry.Entity;

        // Assert
        result.Should().NotBeNull();
        result.GoalsHomeTeam.Should().Be(2);
        result.GoalsAwayTeam.Should().Be(1);
        result.TotalEvents.Should().Be(45);
        result.TotalShots.Should().Be(12);
        result.TotalPasses.Should().Be(345);
        result.TotalFouls.Should().Be(8);
        result.TotalCards.Should().Be(3);
        result.TotalYellowCards.Should().Be(2);
        result.TotalRedCards.Should().Be(1);
        result.TotalOffsides.Should().Be(4);
        result.TotalCorners.Should().Be(6);
        result.TotalSubstitutions.Should().Be(5);
        result.TotalInjuries.Should().Be(1);
        result.TotalPenalties.Should().Be(1);
        result.TotalThrowIns.Should().Be(15);
        result.TotalOuts.Should().Be(8);
        result.TotalGoals.Should().Be(3);
        result.TotalGoalKicks.Should().Be(7);
        result.TotalGoalkeeperSaves.Should().Be(9);
        result.TotalDribbles.Should().Be(23);
        result.TotalPossessionWon.Should().Be(67);
        result.TotalFreeKicks.Should().Be(5);
        result.TotalDuels.Should().Be(89);
        result.TotalErrors.Should().Be(3);
        result.TotalBlocks.Should().Be(12);
        result.TotalClearances.Should().Be(18);
        result.TotalInterceptions.Should().Be(14);
    }

    [Fact]
    public async Task GetByMatchIdAsync_ShouldIncludeMatchNavigationProperty()
    {
        // Arrange
        var matchEventsEntity = await SeedMatchEventsAsync();
        await Context.SaveChangesAsync();

        var matchEvents = matchEventsEntity.Entity;

        // Act
        var result = await _matchEventsRepository.GetByMatchIdAsync(matchEvents.MatchId);

        // Assert
        result.Should().NotBeNull();
        result!.Match.Should().NotBeNull();
        result.Match!.Id.Should().Be(matchEvents.MatchId);
        result.Match.HomeTeam.Should().NotBeNull();
        result.Match.AwayTeam.Should().NotBeNull();
    }

    // Helper methods for seeding test data
    private async Task<EntityEntry<MatchEvents>> SeedMatchEventsAsync()
    {
        var match = await SeedMatchAsync();
        var matchEvents = CreateValidMatchEvents(match.Id);

        return await _matchEventsRepository.AddAsync(matchEvents);
    }

    private static MatchEvents CreateValidMatchEvents(int matchId)
    {
        var events = new[]
        {
            new
            {
                Type = "Goal",
                Time = 25,
                Player = "TestPlayer1",
                Team = "Home",
            },
            new
            {
                Type = "Yellow Card",
                Time = 40,
                Player = "TestPlayer2",
                Team = "Away",
            },
        };

        return new MatchEvents
        {
            MatchId = matchId,
            EventsJson = JsonSerializer.Serialize(events),
            LastUpdated = DateTime.UtcNow,
            GoalsHomeTeam = 1,
            GoalsAwayTeam = 0,
            TotalEvents = 15,
            TotalShots = 8,
            TotalPasses = 234,
            TotalFouls = 5,
            TotalCards = 1,
            TotalYellowCards = 1,
            TotalRedCards = 0,
            TotalOffsides = 2,
            TotalCorners = 3,
            TotalSubstitutions = 2,
            TotalInjuries = 0,
            TotalPenalties = 0,
            TotalThrowIns = 12,
            TotalOuts = 5,
            TotalGoals = 1,
            TotalGoalKicks = 4,
            TotalGoalkeeperSaves = 3,
            TotalDribbles = 15,
            TotalPossessionWon = 45,
            TotalFreeKicks = 3,
            TotalDuels = 67,
            TotalErrors = 1,
            TotalBlocks = 8,
            TotalClearances = 12,
            TotalInterceptions = 9,
        };
    }

    private async Task<Match> SeedMatchAsync()
    {
        var user = new ApplicationUser
        {
            FirstName = "Matches",
            LastName = "Creator",
            UserName = "match-creator",
            Email = "matchecreator69@example.com",
            EmailConfirmed = true,
        };
        Context.Users.Add(user);
        await Context.SaveChangesAsync();

        var homeTeam = await SeedTeamAsync("Home Team" + Guid.NewGuid());
        var awayTeam = await SeedTeamAsync("Away Team" + Guid.NewGuid());
        var season = await SeedSeasonAsync();

        var match = new Match
        {
            HomeTeamId = homeTeam.Id,
            AwayTeamId = awayTeam.Id,
            HomeTeamSeasonId = season.Id,
            AwayTeamSeasonId = season.Id,
            ScheduledDateTimeUtc = DateTime.UtcNow.AddDays(7),
            MatchStatus = "Scheduled",
            HomeTeamScore = 0,
            AwayTeamScore = 0,
            CreatorId = user.Id,
        };

        Context.Matches.Add(match);
        await Context.SaveChangesAsync();

        return match;
    }

    private async Task<Team> SeedTeamAsync(string name)
    {
        var team = new Team
        {
            Name = name,
            City = "Test City",
            FoundationDate = new DateTime(1900, 1, 1),
            Logo = "http://example.com/logo.png",
        };

        Context.Teams.Add(team);
        await Context.SaveChangesAsync();

        return team;
    }

    private async Task<Season> SeedSeasonAsync()
    {
        var season = new Season
        {
            Name = "Test Season 2024" + Guid.NewGuid(),
            StartDate = DateTime.UtcNow.AddMonths(-3),
            EndDate = DateTime.UtcNow.AddMonths(6),
            IsActive = true,
            LeagueName = "Test League",
            Country = "Test Country",
        };

        Context.Seasons.Add(season);
        await Context.SaveChangesAsync();

        return season;
    }
}
