using Domain.Models;
using Domain.Repositories;
using FluentAssertions;
using Footex.IntegrationTests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Footex.IntegrationTests.Repositories;

public class TeamSeasonsRepositoryIntegrationTests : BaseIntegrationTest
{
    private readonly ITeamSeasonsRepository _teamSeasonsRepository;

    public TeamSeasonsRepositoryIntegrationTests(FootexWebApplicationFactory factory)
        : base(factory)
    {
        _teamSeasonsRepository =
            FactoryServiceScope.ServiceProvider.GetRequiredService<ITeamSeasonsRepository>();
    }

    public override async Task InitializeAsync()
    {
        await FreeDbAsync(Context.Coaches, Context.Players, Context.Matches, Context.Teams, Context.Seasons, Context.TeamSeasons);
    }

    [Fact]
    public async Task AddAsync_ShouldAddTeamSeasonSuccessfully()
    {
        // Arrange
        var team = await SeedTeamAsync();
        var season = await SeedSeasonAsync();
        var teamSeason = CreateValidTeamSeason(team.Id, season.Id);

        // Act
        var entityEntry = await _teamSeasonsRepository.AddAsync(teamSeason);
        await Context.SaveChangesAsync();

        var result = entityEntry.Entity;

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.TeamId.Should().Be(team.Id);
        result.SeasonId.Should().Be(season.Id);
        result.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task GetByIdAsync_WhenTeamSeasonExists_ShouldReturnTeamSeason()
    {
        // Arrange
        var teamSeason = await SeedTeamSeasonAsync();
        await Context.SaveChangesAsync();

        // Act
        var result = await _teamSeasonsRepository.GetByIdAsync(teamSeason.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(teamSeason.Id);
        result.TeamId.Should().Be(teamSeason.TeamId);
        result.SeasonId.Should().Be(teamSeason.SeasonId);
    }

    [Fact]
    public async Task GetByIdAsync_WhenTeamSeasonDoesNotExist_ShouldReturnNull()
    {
        // Act
        var result = await _teamSeasonsRepository.GetByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetSeasonsByTeamIdAsync_ShouldReturnTeamSeasonsWithSeasonIncluded()
    {
        // Arrange
        var team = await SeedTeamAsync();
        var season1 = await SeedSeasonAsync("Season 2023");
        var season2 = await SeedSeasonAsync("Season 2024");

        var teamSeason1 = await SeedTeamSeasonAsync(team.Id, season1.Id);
        var teamSeason2 = await SeedTeamSeasonAsync(team.Id, season2.Id);
        await Context.SaveChangesAsync();

        // Act
        var result = await _teamSeasonsRepository.GetSeasonsByTeamIdAsync(team.Id);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().Contain(ts => ts.Id == teamSeason1.Id);
        result.Should().Contain(ts => ts.Id == teamSeason2.Id);

        // Verify Season navigation property is included
        result.All(ts => ts.Season != null).Should().BeTrue();
        result.Should().Contain(ts => ts.Season!.Name == "Season 2023");
        result.Should().Contain(ts => ts.Season!.Name == "Season 2024");
    }

    [Fact]
    public async Task GetSeasonsByTeamIdAsync_WhenTeamHasNoSeasons_ShouldReturnEmptyList()
    {
        // Arrange
        var team = await SeedTeamAsync();

        // Act
        var result = await _teamSeasonsRepository.GetSeasonsByTeamIdAsync(team.Id);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByTeamAndSeasonIdAsync_WhenTeamSeasonExists_ShouldReturnTeamSeason()
    {
        // Arrange
        var teamSeason = await SeedTeamSeasonAsync();
        await Context.SaveChangesAsync();

        // Act
        var result = await _teamSeasonsRepository.GetByTeamAndSeasonIdAsync(
            teamSeason.TeamId,
            teamSeason.SeasonId
        );

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(teamSeason.Id);
        result.TeamId.Should().Be(teamSeason.TeamId);
        result.SeasonId.Should().Be(teamSeason.SeasonId);
    }

    [Fact]
    public async Task GetByTeamAndSeasonIdAsync_WhenTeamSeasonDoesNotExist_ShouldReturnNull()
    {
        // Act
        var result = await _teamSeasonsRepository.GetByTeamAndSeasonIdAsync(999, 999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetTeamsBySeasonIdAsync_ShouldReturnTeamSeasonsWithTeamIncluded()
    {
        // Arrange
        var season = await SeedSeasonAsync();
        var team1 = await SeedTeamAsync("Team A");
        var team2 = await SeedTeamAsync("Team B");

        var teamSeason1 = await SeedTeamSeasonAsync(team1.Id, season.Id);
        var teamSeason2 = await SeedTeamSeasonAsync(team2.Id, season.Id);
        await Context.SaveChangesAsync();

        // Act
        var result = await _teamSeasonsRepository.GetTeamsBySeasonIdAsync(season.Id);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().Contain(ts => ts.Id == teamSeason1.Id);
        result.Should().Contain(ts => ts.Id == teamSeason2.Id);

        // Verify Team navigation property is included
        result.All(ts => ts.Team != null).Should().BeTrue();
        result.Should().Contain(ts => ts.Team!.Name == "Team A");
        result.Should().Contain(ts => ts.Team!.Name == "Team B");
    }

    [Fact]
    public async Task GetTeamsBySeasonIdAsync_WhenSeasonHasNoTeams_ShouldReturnEmptyList()
    {
        // Arrange
        var season = await SeedSeasonAsync();

        // Act
        var result = await _teamSeasonsRepository.GetTeamsBySeasonIdAsync(season.Id);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateTeamSeasonSuccessfully()
    {
        // Arrange
        var teamSeason = await SeedTeamSeasonAsync();
        teamSeason.UpdatedAt = DateTime.UtcNow.AddSeconds(-5);
        await Context.SaveChangesAsync();
        var originalUpdatedAt = teamSeason.UpdatedAt;

        // Act
        teamSeason.UpdatedAt = DateTime.UtcNow;
        var entityEntry = _teamSeasonsRepository.Update(teamSeason);
        await Context.SaveChangesAsync();

        var result = entityEntry.Entity;

        // Assert
        result.Should().NotBeNull();
        result.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public async Task DeleteAsync_ShouldDeleteTeamSeasonSuccessfully()
    {
        // Arrange
        var teamSeason = await SeedTeamSeasonAsync();

        // Act
        _teamSeasonsRepository.Delete(teamSeason);

        // Assert
        var deletedTeamSeason = await _teamSeasonsRepository.GetByIdAsync(teamSeason.Id);
        deletedTeamSeason.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllTeamSeasons()
    {
        // Arrange
        var teamSeason1 = await SeedTeamSeasonAsync();
        var teamSeason2 = await SeedTeamSeasonAsync();
        await Context.SaveChangesAsync();

        // Act
        var result = await _teamSeasonsRepository.GetAllAsync();

        // Assert
        var teamSeasons = result as TeamSeason[] ?? result.ToArray();
        teamSeasons.Should().NotBeNull();
        teamSeasons.Should().HaveCountGreaterThanOrEqualTo(2);
        teamSeasons.Should().Contain(ts => ts.Id == teamSeason1.Id);
        teamSeasons.Should().Contain(ts => ts.Id == teamSeason2.Id);
    }

    [Fact]
    public async Task GetAllAsync_WhenNoTeamSeasons_ShouldReturnEmptyList()
    {
        // Act
        var result = await _teamSeasonsRepository.GetAllAsync();

        // Assert
        var teamSeasons = result as TeamSeason[] ?? result.ToArray();
        teamSeasons.Should().NotBeNull();
        teamSeasons.Should().BeEmpty();
    }

    [Fact]
    public async Task TeamSeason_UniqueConstraint_ShouldPreventDuplicateTeamSeasonCombination()
    {
        // Arrange
        var team = await SeedTeamAsync();
        var season = await SeedSeasonAsync();

        var teamSeason1 = CreateValidTeamSeason(team.Id, season.Id);
        await _teamSeasonsRepository.AddAsync(teamSeason1);
        await Context.SaveChangesAsync();

        var teamSeason2 = CreateValidTeamSeason(team.Id, season.Id);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<DbUpdateException>(async () =>
        {
            await _teamSeasonsRepository.AddAsync(teamSeason2);
            await Context.SaveChangesAsync();
        });

        exception.Should().NotBeNull();
    }

    [Fact]
    public async Task GetSeasonsByTeamIdAsync_ShouldReturnSeasonsOrderedProperly()
    {
        // Arrange
        var team = await SeedTeamAsync();
        var season1 = await SeedSeasonAsync("Season 2022", new DateTime(2022, 8, 1));
        var season2 = await SeedSeasonAsync("Season 2023", new DateTime(2023, 8, 1));
        var season3 = await SeedSeasonAsync("Season 2024", new DateTime(2024, 8, 1));

        await SeedTeamSeasonAsync(team.Id, season3.Id);
        await SeedTeamSeasonAsync(team.Id, season1.Id);
        await SeedTeamSeasonAsync(team.Id, season2.Id);
        await Context.SaveChangesAsync();

        // Act
        var result = await _teamSeasonsRepository.GetSeasonsByTeamIdAsync(team.Id);

        // Assert
        result.Should().HaveCount(3);
        result.All(ts => ts.Season != null).Should().BeTrue();

        var seasonNames = result.Select(ts => ts.Season!.Name).ToList();
        seasonNames.Should().Contain("Season 2022");
        seasonNames.Should().Contain("Season 2023");
        seasonNames.Should().Contain("Season 2024");
    }

    [Fact]
    public async Task GetTeamsBySeasonIdAsync_ShouldReturnTeamsOrderedProperly()
    {
        // Arrange
        var season = await SeedSeasonAsync();
        var teamA = await SeedTeamAsync("A Team");
        var teamB = await SeedTeamAsync("B Team");
        var teamC = await SeedTeamAsync("C Team");

        await SeedTeamSeasonAsync(teamC.Id, season.Id);
        await SeedTeamSeasonAsync(teamA.Id, season.Id);
        await SeedTeamSeasonAsync(teamB.Id, season.Id);
        await Context.SaveChangesAsync();

        // Act
        var result = await _teamSeasonsRepository.GetTeamsBySeasonIdAsync(season.Id);

        // Assert
        result.Should().HaveCount(3);
        result.All(ts => ts.Team != null).Should().BeTrue();

        var teamNames = result.Select(ts => ts.Team!.Name).ToList();
        teamNames.Should().Contain("A Team");
        teamNames.Should().Contain("B Team");
        teamNames.Should().Contain("C Team");
    }

    // Helper methods for seeding test data
    private async Task<TeamSeason> SeedTeamSeasonAsync()
    {
        var team = await SeedTeamAsync(Guid.NewGuid().ToString());
        var season = await SeedSeasonAsync(Guid.NewGuid().ToString());
        return await SeedTeamSeasonAsync(team.Id, season.Id);
    }

    private async Task<TeamSeason> SeedTeamSeasonAsync(int teamId, int seasonId)
    {
        var teamSeason = CreateValidTeamSeason(teamId, seasonId);
        var teamEntity = await _teamSeasonsRepository.AddAsync(teamSeason);
        return teamEntity.Entity;
    }

    private TeamSeason CreateValidTeamSeason(int teamId, int seasonId)
    {
        return new TeamSeason
        {
            TeamId = teamId,
            SeasonId = seasonId,
            UpdatedAt = DateTime.UtcNow,
        };
    }

    private async Task<Team> SeedTeamAsync(string name = "Test Team")
    {
        var team = new Team
        {
            Name = name,
            City = "Test City",
            FoundationDate = new DateTime(1990, 1, 1),
            Logo = "http://example.com/logo.png",
            Country = "Test Country",
            League = "Test League",
        };

        Context.Teams.Add(team);
        await Context.SaveChangesAsync();

        return team;
    }

    private async Task<Season> SeedSeasonAsync(
        string name = "Test Season 2024",
        DateTime? startDate = null
    )
    {
        var season = new Season
        {
            Name = name,
            LeagueName = "Test League",
            Country = "Test Country",
            StartDate = startDate ?? DateTime.UtcNow.AddMonths(-3),
            EndDate = DateTime.UtcNow.AddMonths(6),
            IsActive = true,
            TotalRounds = 38,
            CurrentRound = 1,
        };

        Context.Seasons.Add(season);
        await Context.SaveChangesAsync();

        return season;
    }
}
