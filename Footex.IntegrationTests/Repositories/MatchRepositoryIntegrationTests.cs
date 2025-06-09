using Domain.Models;
using Domain.Repositories;
using FluentAssertions;
using Footex.IntegrationTests.Common;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Footex.IntegrationTests.Repositories;

[Collection("IntegrationTests")]
public class MatchRepositoryIntegrationTests : BaseIntegrationTest
{
    private readonly IMatchRepository _matchRepository;

    public MatchRepositoryIntegrationTests(FootexWebApplicationFactory factory) : base(factory)
    {
        _matchRepository = ServiceProvider.GetRequiredService<IMatchRepository>();
    }

    [Fact]
    public async Task AddAsync_WithValidMatch_ShouldPersistToDatabase()
    {
        // Arrange
        var teams = await SeedTeamsAsync();
        var season = await SeedSeasonAsync();
        
        var match = new Match
        {
            HomeTeamId = teams.HomeTeam.Id,
            AwayTeamId = teams.AwayTeam.Id,
            CreatorId = ""
        };

        // Act
        var entity = await _matchRepository.AddAsync(match);
        var result = entity.Entity;
        await Context.SaveChangesAsync();

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        
        var persistedMatch = await Context.Matches.FindAsync(result.Id);
        persistedMatch.Should().NotBeNull();
        persistedMatch!.HomeTeamId.Should().Be(teams.HomeTeam.Id);
        persistedMatch.AwayTeamId.Should().Be(teams.AwayTeam.Id);
        persistedMatch.MatchStatus.Should().Be("Scheduled");
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingMatch_ShouldReturnMatch()
    {
        // Arrange
        var match = await SeedMatchAsync();

        // Act
        var result = await _matchRepository.GetByIdAsync(match.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(match.Id);
        result.HomeTeamId.Should().Be(match.HomeTeamId);
        result.AwayTeamId.Should().Be(match.AwayTeamId);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentMatch_ShouldReturnNull()
    {
        // Act
        var result = await _matchRepository.GetByIdAsync(99999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdWithDetailsAsync_WithExistingMatch_ShouldReturnMatchWithDetails()
    {
        // Arrange
        var match = await SeedMatchAsync();

        // Act
        var result = await _matchRepository.GetByIdWithDetailsAsync(match.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(match.Id);
        result.HomeTeam.Should().NotBeNull();
        result.AwayTeam.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAllAsync_WithMultipleMatches_ShouldReturnAllMatches()
    {
        // Arrange
        var match1 = await SeedMatchAsync();
        var match2 = await SeedMatchAsync();

        // Act
        var result = await _matchRepository.GetAllAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCountGreaterOrEqualTo(2);
        result.Should().Contain(m => m.Id == match1.Id);
        result.Should().Contain(m => m.Id == match2.Id);
    }

    [Fact]
    public async Task GetAllWithDetailsAsync_WithMultipleMatches_ShouldReturnAllMatchesWithDetails()
    {
        // Arrange
        var match1 = await SeedMatchAsync();
        var match2 = await SeedMatchAsync();

        // Act
        var result = await _matchRepository.GetAllWithDetailsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCountGreaterOrEqualTo(2);
        result.All(m => m.HomeTeam != null).Should().BeTrue();
        result.All(m => m.AwayTeam != null).Should().BeTrue();
    }

    [Fact]
    public async Task GetBySeasonIdAsync_WithValidSeasonIds_ShouldReturnMatchesForSeason()
    {
        // Arrange
        var teams = await SeedTeamsAsync();
        var season1 = await SeedSeasonAsync();
        var season2 = await SeedSeasonAsync();
        
        var match1 = await SeedMatchAsync(homeTeamId: teams.HomeTeam.Id, awayTeamId: teams.AwayTeam.Id, seasonId: season1.Id);
        var match2 = await SeedMatchAsync(homeTeamId: teams.HomeTeam.Id, awayTeamId: teams.AwayTeam.Id, seasonId: season2.Id);

        // Act
        var result = await _matchRepository.GetBySeasonIdAsync(season1.Id, season1.Id);

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain(m => m.Id == match1.Id);
        result.Should().NotContain(m => m.Id == match2.Id);
    }

    [Fact]
    public async Task GetByTeamIdAsync_WithValidTeamId_ShouldReturnMatchesForTeam()
    {
        // Arrange
        var teams = await SeedTeamsAsync();
        var season = await SeedSeasonAsync();
        
        var match1 = await SeedMatchAsync(homeTeamId: teams.HomeTeam.Id, awayTeamId: teams.AwayTeam.Id, seasonId: season.Id);
        var match2 = await SeedMatchAsync(homeTeamId: teams.AwayTeam.Id, awayTeamId: teams.HomeTeam.Id, seasonId: season.Id);

        // Act
        var result = await _matchRepository.GetByTeamIdAsync(teams.HomeTeam.Id);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCountGreaterOrEqualTo(2);
        result.Should().Contain(m => m.Id == match1.Id);
        result.Should().Contain(m => m.Id == match2.Id);
    }

    [Fact]
    public async Task GetByDateRangeAsync_WithValidDateRange_ShouldReturnMatchesInRange()
    {
        // Arrange
        var startDate = DateTime.UtcNow.Date;
        var endDate = startDate.AddDays(7);
        
        var match1 = await SeedMatchAsync(matchDate: startDate.AddDays(2));
        var match2 = await SeedMatchAsync(matchDate: startDate.AddDays(10)); // Outside range

        // Act
        var result = await _matchRepository.GetByDateRangeAsync(startDate, endDate);

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain(m => m.Id == match1.Id);
        result.Should().NotContain(m => m.Id == match2.Id);
    }

    [Fact]
    public async Task GetUpcomingMatchesAsync_WithValidCount_ShouldReturnUpcomingMatches()
    {
        // Arrange
        var futureDate = DateTime.UtcNow.AddDays(5);
        var pastDate = DateTime.UtcNow.AddDays(-5);
        
        var upcomingMatch = await SeedMatchAsync(matchDate: futureDate);
        var pastMatch = await SeedMatchAsync(matchDate: pastDate);

        // Act
        var result = await _matchRepository.GetUpcomingMatchesAsync(10);

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain(m => m.Id == upcomingMatch.Id);
        result.Should().NotContain(m => m.Id == pastMatch.Id);
    }

    [Fact]
    public async Task GetRecentMatchesAsync_WithValidCount_ShouldReturnRecentMatches()
    {
        // Arrange
        var recentDate = DateTime.UtcNow.AddDays(-2);
        var futureDate = DateTime.UtcNow.AddDays(5);
        
        var recentMatch = await SeedMatchAsync(matchDate: recentDate);
        var futureMatch = await SeedMatchAsync(matchDate: futureDate);

        // Act
        var result = await _matchRepository.GetRecentMatchesAsync(10);

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain(m => m.Id == recentMatch.Id);
        result.Should().NotContain(m => m.Id == futureMatch.Id);
    }

    [Fact]
    public async Task GetByStatusAsync_WithValidStatus_ShouldReturnMatchesWithStatus()
    {
        // Arrange
        var scheduledMatch = await SeedMatchAsync(status: "Scheduled");
        var completedMatch = await SeedMatchAsync(status: "Completed");

        // Act
        var result = await _matchRepository.GetByStatusAsync("Scheduled");

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain(m => m.Id == scheduledMatch.Id);
        result.Should().NotContain(m => m.Id == completedMatch.Id);
    }

    [Fact]
    public async Task SearchAsync_WithValidQuery_ShouldReturnMatchingMatches()
    {
        // Arrange
        var teams = await SeedTeamsAsync();
        var season = await SeedSeasonAsync();
        
        var match = await SeedMatchAsync(
            homeTeamId: teams.HomeTeam.Id, 
            awayTeamId: teams.AwayTeam.Id, 
            seasonId: season.Id,
            venue: "Old Trafford");

        // Act
        var result = await _matchRepository.SearchAsync("Old Trafford");

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain(m => m.Id == match.Id);
    }

    [Fact]
    public async Task GetMatchesBySeasonIdAsync_WithValidSeasonId_ShouldReturnMatchesForSeason()
    {
        // Arrange
        var season1 = await SeedSeasonAsync();
        var season2 = await SeedSeasonAsync();
        
        var match1 = await SeedMatchAsync(seasonId: season1.Id);
        var match2 = await SeedMatchAsync(seasonId: season2.Id);

        // Act
        var result = await _matchRepository.GetMatchesBySeasonIdAsync(season1.Id);

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain(m => m.Id == match1.Id);
        result.Should().NotContain(m => m.Id == match2.Id);
    }

    [Fact]
    public async Task UpdateAsync_WithValidMatch_ShouldUpdateMatchInDatabase()
    {
        // Arrange
        var match = await SeedMatchAsync();
        var originalVenue = match.Stadium;
        
        match.HomeTeamScore = 2;
        match.AwayTeamScore = 1;
        match.MatchStatus = "Completed";

        // Act
        var entityEntry = _matchRepository.UpdateAsync(match);
        var result = entityEntry.Entity;
        await Context.SaveChangesAsync();

        // Assert
        result.Should().NotBeNull();
        result.HomeTeamScore.Should().Be(2);
        result.AwayTeamScore.Should().Be(1);
        result.MatchStatus.Should().Be("Completed");
    }

    [Fact]
    public async Task DeleteAsync_WithExistingMatch_ShouldRemoveFromDatabase()
    {
        // Arrange
        var match = await SeedMatchAsync();
        var matchId = match.Id;

        // Act
         _matchRepository.DeleteAsync(match);
        await Context.SaveChangesAsync();

        // Assert
        var deletedMatch = await Context.Matches.FindAsync(matchId);
        deletedMatch.Should().BeNull();
    }

    [Fact]
    public async Task UpdateSimulationIdAsync_WithValidMatchId_ShouldUpdateSimulationId()
    {
        // Arrange
        var match = await SeedMatchAsync();
        var simulationId = Guid.NewGuid().ToString();

        // Act
        var result = await _matchRepository.UpdateSimulationIdAsync(match.Id, simulationId, CancellationToken.None);
        await Context.SaveChangesAsync();

        // Assert
        result.Should().NotBeNull();
        result!.SimulationId.Should().Be(simulationId);
        
        var persistedMatch = await Context.Matches.FindAsync(match.Id);
        persistedMatch!.SimulationId.Should().Be(simulationId);
    }

    private async Task<Match> SeedMatchAsync(
        int? homeTeamId = null, 
        int? awayTeamId = null, 
        int? seasonId = null,
        DateTime? matchDate = null,
        string status = "Scheduled",
        string venue = "Test Stadium")
    {
        if (homeTeamId == null || awayTeamId == null)
        {
            var teams = await SeedTeamsAsync();
            homeTeamId ??= teams.HomeTeam.Id;
            awayTeamId ??= teams.AwayTeam.Id;
        }

        seasonId ??= (await SeedSeasonAsync()).Id;

        var match = new Match
        {
            HomeTeamId = homeTeamId.Value,
            AwayTeamId = awayTeamId.Value,
            HomeTeamSeasonId = seasonId,
            AwayTeamSeasonId = seasonId,
            ScheduledDateTimeUtc = matchDate ?? DateTime.UtcNow.AddDays(1),
            MatchStatus = status,
            HomeTeamScore = 0,
            AwayTeamScore = 0,
            CreatorId = "creatorid"
        };

        Context.Matches.Add(match);
        await Context.SaveChangesAsync();
        return match;
    }

    private async Task<(Team HomeTeam, Team AwayTeam)> SeedTeamsAsync()
    {
        var homeTeam = new Team
        {
            Name = "Home Team " + Guid.NewGuid().ToString()[..8],
            City = "Home City",
            FoundationDate = new DateTime(1900, 1, 1),
            Logo = "https://example.com/home-logo.png"
        };

        var awayTeam = new Team
        {
            Name = "Away Team " + Guid.NewGuid().ToString()[..8],
            City = "Away City",
            FoundationDate = new DateTime(1900, 1, 1),
            Logo = "https://example.com/away-logo.png"
        };

        Context.Teams.AddRange(homeTeam, awayTeam);
        await Context.SaveChangesAsync();
        
        return (homeTeam, awayTeam);
    }

    private async Task<Season> SeedSeasonAsync()
    {
        var season = new Season
        {
            Name = "Test Season " +
                   Guid.NewGuid()
                       .ToString()[..8],
            StartDate = DateTime.UtcNow.AddDays(-30),
            EndDate = DateTime.UtcNow.AddDays(300),
            IsActive = true,
            LeagueName = "Test League",
            Country = "Test Country",
        };

        Context.Seasons.Add(season);
        await Context.SaveChangesAsync();
        return season;
    }
}
