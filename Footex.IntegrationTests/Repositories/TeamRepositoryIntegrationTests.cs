using Domain.Interfaces;
using Domain.Models;
using Domain.Repositories;
using Footex.IntegrationTests.Common;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Footex.IntegrationTests.Repositories;

public class TeamRepositoryIntegrationTests : BaseIntegrationTest
{
    private readonly ITeamRepository _teamRepository;
    private readonly IUnitOfWork _unitOfWork;

    public TeamRepositoryIntegrationTests(FootexWebApplicationFactory factory) : base(factory)
    {
        _teamRepository = ServiceProvider.GetRequiredService<ITeamRepository>();
        _unitOfWork = ServiceProvider.GetRequiredService<IUnitOfWork>();
    }

    [Fact]
    public async Task AddAsync_ValidTeam_AddsTeamToDatabase()
    {
        // Arrange
        var team = TestData.CreateTeam("Real Madrid");
        team.FoundationDate = new DateTime(1902,1,1);
        team.City = "Madrid";
        team.Country = "Spain";

        // Act
        await _teamRepository.AddAsync(team);
        await _unitOfWork.SaveChangesAsync();

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
        var team = TestData.CreateTeam("Barcelona");
        await _teamRepository.AddAsync(team);
        await _unitOfWork.SaveChangesAsync();

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
        await _unitOfWork.SaveChangesAsync();

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
        var deletedTeam = TestData.CreateTeam("Deleted Team");

        await _teamRepository.AddAsync(team1);
        await _teamRepository.AddAsync(team2);
        await _teamRepository.AddAsync(deletedTeam);
        await _unitOfWork.SaveChangesAsync();

        // Act
        var teams = await _teamRepository.GetAllAsync();

        // Assert
        Assert.NotNull(teams);
        Assert.Equal(2, teams.Count()); // Only active teams
        Assert.Contains(teams, t => t.Id == team1.Id);
        Assert.Contains(teams, t => t.Id == team2.Id);
        Assert.DoesNotContain(teams, t => t.Id == deletedTeam.Id);
    }

    [Fact]
    public async Task Update_ExistingTeam_UpdatesTeamInDatabase()
    {
        // Arrange
        var team = TestData.CreateTeam("Original Name");
        await _teamRepository.AddAsync(team);
        await _unitOfWork.SaveChangesAsync();

        // Act
        team.Name = "Updated Name";
        team.City = "Updated City";
        _teamRepository.UpdateAsync(team);
        await _unitOfWork.SaveChangesAsync();

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
        await _unitOfWork.SaveChangesAsync();

        // Act
        _teamRepository.DeleteAsync(team);
        await _unitOfWork.SaveChangesAsync();

        // Assert
        var deletedTeam = await _teamRepository.GetByIdAsync(team.Id);
        Assert.Null(deletedTeam); // Should return null for deleted teams

        // Verify soft delete in database (check IsDeleted flag)
        var allTeamsIncludingDeleted = await _unitOfWork.Teams.FindAsync(t => t.Id == team.Id);
        Assert.NotNull(allTeamsIncludingDeleted);
    }

    [Fact]
    public async Task GetTeamWithPlayersAsync_TeamWithPlayers_ReturnsTeamWithPlayerDetails()
    {
        // Arrange
        var team = TestData.CreateTeam("Team With Players");
        await _teamRepository.AddAsync(team);
        await _unitOfWork.SaveChangesAsync();

        var player1 = TestData.CreatePlayer("Player 1", team.Id);
        var player2 = TestData.CreatePlayer("Player 2", team.Id);
        await _unitOfWork.Players.AddAsync(player1);
        await _unitOfWork.Players.AddAsync(player2);
        await _unitOfWork.SaveChangesAsync();

        // Act
        var teamWithPlayers = await _teamRepository.GetTeamWithPlayersAsync(team.Id);

        // Assert
        Assert.NotNull(teamWithPlayers);
        Assert.Equal(team.Id, teamWithPlayers.Id);
        Assert.NotNull(teamWithPlayers.Players);
        Assert.Equal(2, teamWithPlayers.Players.Count);
        Assert.Contains(teamWithPlayers.Players, p => p.FullName == "Player 1");
        Assert.Contains(teamWithPlayers.Players, p => p.FullName == "Player 2");
    }

    [Fact]
    public async Task GetTeamWithCoachesAsync_TeamWithCoaches_ReturnsTeamWithCoachDetails()
    {
        // Arrange
        var team = TestData.CreateTeam("Team With Coaches");
        await _teamRepository.AddAsync(team);
        await _unitOfWork.SaveChangesAsync();

        var headCoach = TestData.CreateCoach("Head Coach", team.Id);
        var assistantCoach = TestData.CreateCoach("Assistant Coach", team.Id);
        headCoach.Role = "Head Coach";
        assistantCoach.Role = "Assistant Coach";

        await _unitOfWork.Coaches.AddAsync(headCoach);
        await _unitOfWork.Coaches.AddAsync(assistantCoach);
        await _unitOfWork.SaveChangesAsync();

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
        await _unitOfWork.SaveChangesAsync();

        // Act
        var spanishTeams = await _teamRepository.GetByCountryAsync("Spain");

        // Assert
        Assert.NotNull(spanishTeams);
        Assert.Equal(2, spanishTeams.Count());
        Assert.All(spanishTeams, team => Assert.Equal("Spain", team.Country));
        Assert.Contains(spanishTeams, t => t.Name == "Real Madrid");
        Assert.Contains(spanishTeams, t => t.Name == "Barcelona");
    }

    [Fact]
    public async Task GetTeamsByCityAsync_TeamsInCity_ReturnsFilteredTeams()
    {
        // Arrange
        var madridTeam1 = TestData.CreateTeam("Real Madrid");
        var madridTeam2 = TestData.CreateTeam("Atletico Madrid");
        var barcelonaTeam = TestData.CreateTeam("Barcelona");

        madridTeam1.City = "Madrid";
        madridTeam2.City = "Madrid";
        barcelonaTeam.City = "Barcelona";

        await _teamRepository.AddAsync(madridTeam1);
        await _teamRepository.AddAsync(madridTeam2);
        await _teamRepository.AddAsync(barcelonaTeam);
        await _unitOfWork.SaveChangesAsync();

        // Act
        var madridTeams = await _teamRepository.GetByCityAsync("Madrid");

        // Assert
        Assert.NotNull(madridTeams);
        Assert.Equal(2, madridTeams.Count());
        Assert.All(madridTeams, team => Assert.Equal("Madrid", team.City));
    }

    [Fact]
    public async Task SearchTeamsAsync_SearchByName_ReturnsMatchingTeams()
    {
        // Arrange
        var team1 = TestData.CreateTeam("Manchester United");
        var team2 = TestData.CreateTeam("Manchester City");
        var team3 = TestData.CreateTeam("Liverpool");

        await _teamRepository.AddAsync(team1);
        await _teamRepository.AddAsync(team2);
        await _teamRepository.AddAsync(team3);
        await _unitOfWork.SaveChangesAsync();

        // Act
        var manchesterTeams = await _teamRepository.SearchAsync("Manchester");

        // Assert
        Assert.NotNull(manchesterTeams);
        Assert.Equal(2, manchesterTeams.Count());
        Assert.All(manchesterTeams, team => Assert.Contains("Manchester", team.Name));
    }

    [Fact]
    public async Task GetTeamsFoundedAfterYearAsync_TeamsFoundedAfterYear_ReturnsFilteredTeams()
    {
        // Arrange
        var oldTeam = TestData.CreateTeam("Old Team");
        var moderateTeam = TestData.CreateTeam("Moderate Team");
        var newTeam = TestData.CreateTeam("New Team");

        oldTeam.FoundationDate = new DateTime(1890, 1, 1);
        moderateTeam.FoundationDate = new DateTime(1920,1,1);
        newTeam.FoundationDate = new DateTime(1950,1,1);

        await _teamRepository.AddAsync(oldTeam);
        await _teamRepository.AddAsync(moderateTeam);
        await _teamRepository.AddAsync(newTeam);
        await _unitOfWork.SaveChangesAsync();

        // Act
        var recentTeams = await _teamRepository.GetTeamsFoundedAfterYearAsync(1910);

        // Assert
        Assert.NotNull(recentTeams);
        Assert.Equal(2, recentTeams.Count());
        Assert.All(recentTeams, team => Assert.True(team.FoundationDate > new DateTime(1910, 1, 1)));
        Assert.Contains(recentTeams, t => t.Name == "Moderate Team");
        Assert.Contains(recentTeams, t => t.Name == "New Team");
    }

    [Fact]
    public async Task Transaction_RollbackOnError_DoesNotCommitChanges()
    {
        // Arrange
        var team1 = TestData.CreateTeam("Team 1");
        var team2 = TestData.CreateTeam("Team 2");

        // Act & Assert
        using (var transaction = await _unitOfWork.BeginTransactionAsync())
        {
            try
            {
                await _teamRepository.AddAsync(team1);
                await _unitOfWork.SaveChangesAsync();

                // Simulate an error after first save
                await _teamRepository.AddAsync(team2);
                // Force an error by trying to add the same team again
                await _teamRepository.AddAsync(team2);
                await _unitOfWork.SaveChangesAsync();

                await transaction.CommitAsync();
                Assert.True(false, "Should have thrown an exception");
            }
            catch
            {
                await transaction.RollbackAsync();
            }
        }

        // Verify rollback - neither team should exist
        var retrievedTeam1 = await _teamRepository.GetByIdAsync(team1.Id);
        var retrievedTeam2 = await _teamRepository.GetByIdAsync(team2.Id);
        Assert.Null(retrievedTeam1);
        Assert.Null(retrievedTeam2);
    }
}
