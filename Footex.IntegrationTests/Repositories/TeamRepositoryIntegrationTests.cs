using Domain.Models;
using Domain.Repositories;
using FluentAssertions;
using Footex.IntegrationTests.Common;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Footex.IntegrationTests.Repositories;

public class TeamRepositoryIntegrationTests : BaseIntegrationTest
{
    private readonly ITeamRepository _teamRepository;
    private readonly FootexWebApplicationFactory _factory;

    public TeamRepositoryIntegrationTests(FootexWebApplicationFactory factory)
        : base(factory)
    {
        _teamRepository = FactoryServiceScope.ServiceProvider.GetRequiredService<ITeamRepository>();
        _factory = factory;
        FreeDbAsync(Context.Coaches).Wait();
        FreeDbAsync(Context.Players).Wait();
        FreeDbAsync(Context.Teams).Wait();
    }

    [Fact]
    public async Task AddAsync_ValidTeam_AddsTeamToDatabase()
    {
        // Arrange
        var team = TestData.CreateTeam("Villareal CF");
        team.FoundationDate = new DateTime(1902, 1, 1);
        team.City = "Villareal";
        team.Country = "Spain";

        // Act
        await _teamRepository.AddAsync(team);
        await UnitOfWork.SaveChangesAsync();

        // Assert
        var savedTeam = await _teamRepository.GetByIdAsync(team.Id);
        Assert.NotNull(savedTeam);
        Assert.Equal(team.Name, savedTeam.Name);
        Assert.Equal(team.FoundationDate, savedTeam.FoundationDate);
        Assert.Equal(team.City, savedTeam.City);
        Assert.Equal(team.Country, savedTeam.Country);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingTeam_ReturnsTeam()
    {
        // Arrange
        var team = TestData.CreateTeam("Existing Team");
        await _teamRepository.AddAsync(team);
        await UnitOfWork.SaveChangesAsync();

        // Act
        var retrievedTeam = await _teamRepository.GetByIdAsync(team.Id);

        // Assert
        Assert.NotNull(retrievedTeam);
        Assert.Equal(team.Id, retrievedTeam.Id);
        Assert.Equal(team.Name, retrievedTeam.Name);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentTeam_ReturnsNull()
    {
        // Act
        var retrievedTeam = await _teamRepository.GetByIdAsync(-1);

        // Assert
        Assert.Null(retrievedTeam);
    }

    [Fact]
    public async Task GetByIdAsync_DeletedTeam_ReturnsNull()
    {
        // Arrange
        var team = TestData.CreateTeam("Deleted Team");
        await _teamRepository.AddAsync(team);
        await UnitOfWork.SaveChangesAsync();
        // Simulate soft delete
        _teamRepository.Delete(team);
        await UnitOfWork.SaveChangesAsync();

        // Act
        var retrievedTeam = await _teamRepository.GetByIdAsync(team.Id);

        // Assert
        Assert.Null(retrievedTeam);
    }

    [Fact]
    public async Task GetAllAsync_MultipleTeams_ReturnsAllActiveTeams()
    {
        // Arrange
        var team1 = TestData.CreateTeam("Team 1");
        var team2 = TestData.CreateTeam("Team 2");
        team2.Id = 2;
        var deletedTeam = TestData.CreateTeam("Deleted Team");
        deletedTeam.Id = 3;

        await UnitOfWork.Teams.AddRangeAsync(CancellationToken.None, team1, team2, deletedTeam);
        await UnitOfWork.SaveChangesAsync();

        // Simulate soft delete
        _teamRepository.Delete(deletedTeam);
        await UnitOfWork.SaveChangesAsync();

        // Act
        var teams = await _teamRepository.GetAllAsync();

        // Assert
        Assert.NotNull(teams);
        var retrievedTeams = teams as Team[] ?? teams.ToArray();
        Assert.Equal(2, retrievedTeams.Length);
        Assert.Contains(retrievedTeams, t => t.Id == team1.Id);
        Assert.Contains(retrievedTeams, t => t.Id == team2.Id);
        Assert.DoesNotContain(retrievedTeams, t => t.Id == deletedTeam.Id);
    }

    [Fact]
    public async Task Update_ExistingTeam_UpdatesTeamInDatabase()
    {
        // Arrange
        var team = TestData.CreateTeam("Original Name");
        await _teamRepository.AddAsync(team);
        await UnitOfWork.SaveChangesAsync();

        // Act
        team.Name = "Updated Name";
        team.City = "Updated City";
        _teamRepository.Update(team);
        await UnitOfWork.SaveChangesAsync();

        // Assert
        var updatedTeam = await _teamRepository.GetByIdAsync(team.Id);
        Assert.NotNull(updatedTeam);
        Assert.Equal("Updated Name", updatedTeam.Name);
        Assert.Equal("Updated City", updatedTeam.City);
    }

    [Fact]
    public async Task Delete_ExistingTeam_SoftDeletesTeam()
    {
        // Arrange
        var team = TestData.CreateTeam("Team To Delete");
        await _teamRepository.AddAsync(team);
        await UnitOfWork.SaveChangesAsync();

        // Act
        _teamRepository.Delete(team);
        await UnitOfWork.SaveChangesAsync();

        // Assert
        var deletedTeam = await _teamRepository.GetByIdAsync(team.Id);
        Assert.Null(deletedTeam); // Should return null for deleted teams

        // // Verify soft delete in database (check IsDeleted flag)
        // var allTeamsIncludingDeleted = await UnitOfWork.Teams.FindAsync(t => t.Id == team.Id);
        // Assert.NotNull(allTeamsIncludingDeleted);
    }

    [Fact]
    public async Task GetTeamWithPlayersAsync_TeamWithPlayers_ReturnsTeamWithPlayerDetails()
    {
        // Arrange
        var team = TestData.CreateTeam("Team With Players");
        await _teamRepository.AddAsync(team);
        await UnitOfWork.SaveChangesAsync();

        var player1 = TestData.CreateTestDbPlayer(team.Id);
        var player2 = TestData.CreateTestDbPlayer(team.Id);
        await UnitOfWork.Players.AddRangeAsync(CancellationToken.None, player1, player2);
        await UnitOfWork.SaveChangesAsync();

        // Act
        var teamWithPlayers = await _teamRepository.GetTeamWithPlayersAsync(team.Id);

        // Assert
        Assert.NotNull(teamWithPlayers);
        Assert.Equal(team.Id, teamWithPlayers.Id);
        Assert.NotNull(teamWithPlayers.Players);
        Assert.Equal(2, teamWithPlayers.Players.Count);
    }

    [Fact]
    public async Task GetTeamWithCoachesAsync_TeamWithCoaches_ReturnsTeamWithCoachDetails()
    {
        // Arrange
        var team = TestData.CreateTeam("Team With Coaches");
        await _teamRepository.AddAsync(team);
        await UnitOfWork.SaveChangesAsync();

        var headCoach = TestData.CreateTestDbCoach(team.Id, "Head Coach");
        var assistantCoach = TestData.CreateTestDbCoach(team.Id, "Assistant Coach");
        headCoach.Role = "Head Coach";
        assistantCoach.Role = "Assistant Coach";

        await UnitOfWork.Coaches.AddRangeAsync(CancellationToken.None, headCoach, assistantCoach);
        await UnitOfWork.SaveChangesAsync();

        // Act
        var teamWithCoaches = await _teamRepository.GetTeamWithCoachesAsync(team.Id);

        // Assert
        Assert.NotNull(teamWithCoaches);
        Assert.Equal(team.Id, teamWithCoaches.Id);
        Assert.NotNull(teamWithCoaches.Coaches);
        Assert.Equal(2, teamWithCoaches.Coaches.Count);
        Assert.Contains(teamWithCoaches.Coaches, c => c.Role == "Head Coach");
        Assert.Contains(teamWithCoaches.Coaches, c => c.Role == "Assistant Coach");
    }

    [Fact]
    public async Task GetTeamsByCountryAsync_TeamsInCountry_ReturnsFilteredTeams()
    {
        // Arrange
        var spanishTeam1 = TestData.CreateTeam("Real Madrid");
        var spanishTeam2 = TestData.CreateTeam("Barcelona");
        var englishTeam = TestData.CreateTeam("Manchester United");

        spanishTeam1.Country = "Spain";
        spanishTeam2.Country = "Spain";
        englishTeam.Country = "England";

        await _teamRepository.AddAsync(spanishTeam1);
        await _teamRepository.AddAsync(spanishTeam2);
        await _teamRepository.AddAsync(englishTeam);
        await UnitOfWork.SaveChangesAsync();

        // Act
        var spanishTeams = await _teamRepository.GetByCountryAsync("Spain");

        // Assert
        Assert.NotNull(spanishTeams);
        Assert.Equal(2, spanishTeams.Count);
        Assert.All(spanishTeams, team => Assert.Equal("Spain", team.Country));
        Assert.Contains(spanishTeams, t => t.Name == "Real Madrid");
        Assert.Contains(spanishTeams, t => t.Name == "Barcelona");
    }

    [Fact]
    public async Task GetTeamsByCityAsync_TeamsInCity_ReturnsFilteredTeams()
    {
        // Arrange
        var madridTeam1 = TestData.CreateTeam("Getafe CF");
        var madridTeam2 = TestData.CreateTeam("Rayo Vallecano");
        var barcelonaTeam = TestData.CreateTeam("Espanyol");

        madridTeam1.City = "Madrid";
        madridTeam2.City = "Madrid";
        barcelonaTeam.City = "Barcelona";

        await _teamRepository.AddRangeAsync(
            CancellationToken.None,
            madridTeam1,
            madridTeam2,
            barcelonaTeam
        );
        await UnitOfWork.SaveChangesAsync();

        // Act
        var madridTeams = await _teamRepository.GetByCityAsync("Madrid");

        // Assert
        Assert.NotNull(madridTeams);
        var teamsInMadrid = madridTeams as Team[] ?? madridTeams.ToArray();
        Assert.All(teamsInMadrid, team => Assert.Equal("Madrid", team.City));
    }

    [Fact]
    public async Task SearchTeamsAsync_SearchByName_ReturnsMatchingTeams()
    {
        // Arrange
        var team1 = TestData.CreateTeam("Manchester Yanited");
        var team2 = TestData.CreateTeam("Manchester City");
        var team3 = TestData.CreateTeam("Liverpool");

        await UnitOfWork.Teams.AddRangeAsync(CancellationToken.None, team1, team2, team3);
        await UnitOfWork.SaveChangesAsync();

        // Act
        var manchesterTeams = await _teamRepository.SearchAsync("Manchester");

        // Assert
        Assert.NotNull(manchesterTeams);
        var matchingTeams = manchesterTeams as Team[] ?? manchesterTeams.ToArray();
        Assert.Equal(2, matchingTeams.Length);
        Assert.All(matchingTeams, team => Assert.Contains("Manchester", team.Name));
    }

    [Fact]
    public async Task GetTeamsFoundedAfterYearAsync_TeamsFoundedAfterYear_ReturnsFilteredTeams()
    {
        // Arrange
        var oldTeam = TestData.CreateTeam("Old Team");
        var moderateTeam = TestData.CreateTeam("Moderate Team");
        var newTeam = TestData.CreateTeam("New Team");

        oldTeam.FoundationDate = new DateTime(1890, 1, 1);
        moderateTeam.FoundationDate = new DateTime(1920, 1, 1);
        newTeam.FoundationDate = new DateTime(1950, 1, 1);

        await _teamRepository.AddAsync(oldTeam);
        await _teamRepository.AddAsync(moderateTeam);
        await _teamRepository.AddAsync(newTeam);
        await UnitOfWork.SaveChangesAsync();

        // Act
        var recentTeams = await _teamRepository.GetTeamsFoundedAfterYearAsync(1910);

        // Assert
        Assert.NotNull(recentTeams);
        var filteredTeams = recentTeams as Team[] ?? recentTeams.ToArray();
        Assert.Equal(2, filteredTeams.Length);
        Assert.All(
            filteredTeams,
            team => Assert.True(team.FoundationDate > new DateTime(1910, 1, 1))
        );
        Assert.Contains(filteredTeams, t => t.Name == "Moderate Team");
        Assert.Contains(filteredTeams, t => t.Name == "New Team");
    }

    [Fact]
    public async Task Transaction_RollbackOnError_DoesNotCommitChanges()
    {
        // Arrange
        var team1 = TestData.CreateTeam("Team To Rollback");

        // Act & Assert
        // We expect a DbUpdateException, which occurs when SaveChangesAsync fails
        // due to a database constraint violation.
        await Assert.ThrowsAsync<DbUpdateException>(async () =>
        {
            await using var transaction = await UnitOfWork.BeginTransactionAsync();

            // Add and save the first team to get a valid ID from the database.
            await _teamRepository.AddAsync(team1);
            await UnitOfWork.SaveChangesAsync();

            // At this point, 'team1' is being tracked by the DbContext. To force a
            // database-level conflict (DbUpdateException), we must first make the
            // change tracker "forget" the original instance.
            // NOTE: This assumes your repository or unit of work provides access
            // to the underlying DbContext to clear its change tracker.
            _teamRepository.ClearChangeTracker();

            // Create a new team instance with the same ID as the one we just saved.
            var conflictingTeam = TestData.CreateTeam("Conflicting Team");
            conflictingTeam.Id = team1.Id;

            // Add the new instance. The change tracker will accept this because
            // it's no longer tracking the original 'team1' object.
            await _teamRepository.AddAsync(conflictingTeam);

            // This call will now fail at the database level with a primary key violation,
            // which EF Core correctly wraps in a DbUpdateException.
            await UnitOfWork.SaveChangesAsync();

            // This line is never reached.
            await transaction.CommitAsync();
        });

        // Verify that the rollback was successful using a new, clean DbContext.
        using var assertScope = _factory.Services.CreateScope();
        var context = assertScope.ServiceProvider.GetRequiredService<FootballDbContext>();
        var retrievedTeam1 = await context.Teams.FindAsync(team1.Id);

        // Because the transaction failed and was rolled back, the team should not exist.
        retrievedTeam1.Should().BeNull();
    }
}
