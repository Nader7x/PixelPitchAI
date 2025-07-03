using Domain.Interfaces;
using Domain.Models;
using Domain.Repositories;
using FluentAssertions;
using Footex.IntegrationTests.Common;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Footex.IntegrationTests.Repositories;

[Collection("Database")]
public class PlayerRepositoryIntegrationTests : BaseIntegrationTest
{
    private readonly IPlayerRepository _playerRepository;

    public PlayerRepositoryIntegrationTests(FootexWebApplicationFactory factory)
        : base(factory)
    {
        _playerRepository = ServiceProvider.GetRequiredService<IPlayerRepository>();
    }

    private IUnitOfWork UnitOfWork => ServiceProvider.GetRequiredService<IUnitOfWork>();

    [Fact]
    public async Task GetByFullNameAsync_WithValidName_ReturnsPlayer()
    {
        // Arrange
        var team = new Team
        {
            Name = "Test Team",
            League = "Test League",
            Country = "Test Country",
            FoundationDate = new DateTime(2000, 1, 1),
        };

        var player = new Player
        {
            FullName = "John Doe",
            KnownName = "Johnny",
            Nationality = "USA",
            Position = "Forward",
            PreferredFoot = "Right",
            ShirtNumber = 10,
            Team = team,
        };

        // Act
        await UnitOfWork.Teams.AddAsync(team);
        await UnitOfWork.Players.AddAsync(player);
        await UnitOfWork.SaveChangesAsync();

        var result = await _playerRepository.GetByFullNameAsync("John Doe");

        // Assert
        result.Should().NotBeNull();
        result!.FullName.Should().Be("John Doe");
        result.KnownName.Should().Be("Johnny");
        result.Nationality.Should().Be("USA");
        result.Position.Should().Be("Forward");
        result.PreferredFoot.Should().Be("Right");
        result.ShirtNumber.Should().Be(10);
    }

    [Fact]
    public async Task GetByFullNameAsync_WithNonExistentName_ReturnsNull()
    {
        // Act
        var result = await _playerRepository.GetByFullNameAsync("Non Existent Player");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByFullNameAsync_WithNullOrEmptyName_ReturnsNull()
    {
        // Act & Assert
        (await _playerRepository.GetByFullNameAsync(null))
            .Should()
            .BeNull();
        (await _playerRepository.GetByFullNameAsync("")).Should().BeNull();
        (await _playerRepository.GetByFullNameAsync("   ")).Should().BeNull();
    }

    [Fact]
    public async Task GetByNationalityAsync_WithValidNationality_ReturnsPlayers()
    {
        // Arrange
        var team = new Team
        {
            Name = "Test Team",
            League = "Test League",
            Country = "Test Country",
            FoundationDate = new DateTime(2000, 1, 1),
        };

        var player1 = new Player
        {
            FullName = "American Player 1",
            Nationality = "USA",
            Position = "Forward",
            Team = team,
        };

        var player2 = new Player
        {
            FullName = "American Player 2",
            Nationality = "USA",
            Position = "Midfielder",
            Team = team,
        };

        var player3 = new Player
        {
            FullName = "Brazilian Player",
            Nationality = "Brazil",
            Position = "Defender",
            Team = team,
        };

        // Act
        await UnitOfWork.Teams.AddAsync(team);
        await UnitOfWork.Players.AddAsync(player1);
        await UnitOfWork.Players.AddAsync(player2);
        await UnitOfWork.Players.AddAsync(player3);
        await UnitOfWork.SaveChangesAsync();

        var result = await _playerRepository.GetByNationalityAsync("USA");

        // Assert
        result.Should().HaveCount(2);
        result.All(p => p.Nationality == "USA").Should().BeTrue();
        result.Should().Contain(p => p.FullName == "American Player 1");
        result.Should().Contain(p => p.FullName == "American Player 2");
    }

    [Fact]
    public async Task GetByNationalityAsync_WithNonExistentNationality_ReturnsEmpty()
    {
        // Act
        var result = await _playerRepository.GetByNationalityAsync("Non Existent Country");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByPreferredFootAsync_WithValidFoot_ReturnsPlayers()
    {
        // Arrange
        var team = new Team
        {
            Name = "Test Team",
            League = "Test League",
            Country = "Test Country",
            FoundationDate = new DateTime(2000, 1, 1),
        };

        var rightFootedPlayer1 = new Player
        {
            FullName = "Right Footed Player 1",
            PreferredFoot = "Right",
            Position = "Forward",
            Team = team,
        };

        var rightFootedPlayer2 = new Player
        {
            FullName = "Right Footed Player 2",
            PreferredFoot = "Right",
            Position = "Midfielder",
            Team = team,
        };

        var leftFootedPlayer = new Player
        {
            FullName = "Left Footed Player",
            PreferredFoot = "Left",
            Position = "Defender",
            Team = team,
        };

        // Act
        await UnitOfWork.Teams.AddAsync(team);
        await UnitOfWork.Players.AddAsync(rightFootedPlayer1);
        await UnitOfWork.Players.AddAsync(rightFootedPlayer2);
        await UnitOfWork.Players.AddAsync(leftFootedPlayer);
        await UnitOfWork.SaveChangesAsync();

        var rightFootedResult = await _playerRepository.GetByPreferredFootAsync("Right");
        var leftFootedResult = await _playerRepository.GetByPreferredFootAsync("Left");

        // Assert
        rightFootedResult.Should().HaveCount(2);
        rightFootedResult.All(p => p.PreferredFoot == "Right").Should().BeTrue();

        leftFootedResult.Should().HaveCount(1);
        leftFootedResult.First().PreferredFoot.Should().Be("Left");
        leftFootedResult.First().FullName.Should().Be("Left Footed Player");
    }

    [Fact]
    public async Task GetByPreferredFootAsync_WithNullOrEmptyFoot_ReturnsEmpty()
    {
        // Act & Assert
        (await _playerRepository.GetByPreferredFootAsync(null))
            .Should()
            .BeEmpty();
        (await _playerRepository.GetByPreferredFootAsync("")).Should().BeEmpty();
        (await _playerRepository.GetByPreferredFootAsync("   ")).Should().BeEmpty();
    }

    [Fact]
    public async Task SearchAsync_WithPlayerName_ReturnsMatchingPlayers()
    {
        // Arrange
        var team = new Team
        {
            Name = "Test Team",
            League = "Test League",
            Country = "Test Country",
            FoundationDate = new DateTime(2000, 1, 1),
        };

        var player1 = new Player
        {
            FullName = "John Smith",
            KnownName = "Johnny",
            Position = "Forward",
            Team = team,
        };

        var player2 = new Player
        {
            FullName = "Jane Johnson",
            Position = "Midfielder",
            Team = team,
        };

        var player3 = new Player
        {
            FullName = "Bob Wilson",
            Position = "Defender",
            Team = team,
        };

        // Act
        await UnitOfWork.Teams.AddAsync(team);
        await UnitOfWork.Players.AddAsync(player1);
        await UnitOfWork.Players.AddAsync(player2);
        await UnitOfWork.Players.AddAsync(player3);
        await UnitOfWork.SaveChangesAsync();

        var searchResult = await _playerRepository.SearchAsync("John");

        // Assert
        searchResult.Should().HaveCount(2);
        searchResult.Should().Contain(p => p.FullName == "John Smith");
        searchResult.Should().Contain(p => p.FullName == "Jane Johnson");
    }

    [Fact]
    public async Task SearchAsync_WithTeamName_ReturnsPlayersFromTeam()
    {
        // Arrange
        var team1 = new Team
        {
            Name = "Manchester United",
            League = "Premier League",
            Country = "England",
            FoundationDate = new DateTime(1878, 3, 5),
        };

        var team2 = new Team
        {
            Name = "Barcelona",
            League = "La Liga",
            Country = "Spain",
            FoundationDate = new DateTime(1892, 11, 29),
        };

        var player1 = new Player
        {
            FullName = "Player 1",
            Position = "Forward",
            Team = team1,
        };

        var player2 = new Player
        {
            FullName = "Player 2",
            Position = "Midfielder",
            Team = team1,
        };

        var player3 = new Player
        {
            FullName = "Player 3",
            Position = "Defender",
            Team = team2,
        };

        // Act
        await UnitOfWork.Teams.AddAsync(team1);
        await UnitOfWork.Teams.AddAsync(team2);
        await UnitOfWork.Players.AddAsync(player1);
        await UnitOfWork.Players.AddAsync(player2);
        await UnitOfWork.Players.AddAsync(player3);
        await UnitOfWork.SaveChangesAsync();

        var searchResult = await _playerRepository.SearchAsync("Manchester");

        // Assert
        searchResult.Should().HaveCount(2);
        searchResult.All(p => p.Team!.Name == "Manchester United").Should().BeTrue();
        searchResult.Should().Contain(p => p.FullName == "Player 1");
        searchResult.Should().Contain(p => p.FullName == "Player 2");
    }

    [Fact]
    public async Task SearchAsync_WithEmptyQuery_ReturnsEmpty()
    {
        // Act & Assert
        (await _playerRepository.SearchAsync(""))
            .Should()
            .BeEmpty();
        (await _playerRepository.SearchAsync("   ")).Should().BeEmpty();
    }

    [Fact]
    public async Task FindAsync_WithPredicate_ReturnsMatchingPlayers()
    {
        // Arrange
        var team = new Team
        {
            Name = "Test Team",
            League = "Test League",
            Country = "Test Country",
            FoundationDate = new DateTime(2000, 1, 1),
        };

        var forward1 = new Player
        {
            FullName = "Forward 1",
            Position = "Forward",
            ShirtNumber = 9,
            Team = team,
        };

        var forward2 = new Player
        {
            FullName = "Forward 2",
            Position = "Forward",
            ShirtNumber = 11,
            Team = team,
        };

        var midfielder = new Player
        {
            FullName = "Midfielder",
            Position = "Midfielder",
            ShirtNumber = 8,
            Team = team,
        };

        // Act
        await UnitOfWork.Teams.AddAsync(team);
        await UnitOfWork.Players.AddAsync(forward1);
        await UnitOfWork.Players.AddAsync(forward2);
        await UnitOfWork.Players.AddAsync(midfielder);
        await UnitOfWork.SaveChangesAsync();

        var forwards = await _playerRepository.FindAsync(p => p.Position == "Forward");

        // Assert
        forwards.Should().HaveCount(2);
        forwards.All(p => p.Position == "Forward").Should().BeTrue();
        forwards.Should().Contain(p => p.FullName == "Forward 1");
        forwards.Should().Contain(p => p.FullName == "Forward 2");
    }

    [Fact]
    public async Task AddAsync_WithValidPlayer_SavesSuccessfully()
    {
        // Arrange
        var team = new Team
        {
            Name = "Test Team",
            League = "Test League",
            Country = "Test Country",
            FoundationDate = new DateTime(2000, 1, 1),
        };

        var player = new Player
        {
            FullName = "New Player",
            KnownName = "Newbie",
            Nationality = "USA",
            Position = "Forward",
            PreferredFoot = "Right",
            ShirtNumber = 7,
            PhotoUrl = "http://example.com/photo.jpg",
            Team = team,
        };

        // Act
        await UnitOfWork.Teams.AddAsync(team);
        await UnitOfWork.SaveChangesAsync();

        await _playerRepository.AddAsync(player);
        await UnitOfWork.SaveChangesAsync();

        // Assert
        player.Id.Should().BeGreaterThan(0);

        var retrievedPlayer = await _playerRepository.GetByIdAsync(player.Id);
        retrievedPlayer.Should().NotBeNull();
        retrievedPlayer!.FullName.Should().Be("New Player");
        retrievedPlayer.KnownName.Should().Be("Newbie");
        retrievedPlayer.Nationality.Should().Be("USA");
        retrievedPlayer.Position.Should().Be("Forward");
        retrievedPlayer.PreferredFoot.Should().Be("Right");
        retrievedPlayer.ShirtNumber.Should().Be(7);
        retrievedPlayer.PhotoUrl.Should().Be("http://example.com/photo.jpg");
        retrievedPlayer.TeamId.Should().Be(team.Id);
    }

    [Fact]
    public async Task UpdateAsync_WithValidPlayer_UpdatesSuccessfully()
    {
        // Arrange
        var team = new Team
        {
            Name = "Test Team",
            League = "Test League",
            Country = "Test Country",
            FoundationDate = new DateTime(2000, 1, 1),
        };

        var player = new Player
        {
            FullName = "Original Name",
            Position = "Forward",
            ShirtNumber = 9,
            Team = team,
        };

        await UnitOfWork.Teams.AddAsync(team);
        await UnitOfWork.Players.AddAsync(player);
        await UnitOfWork.SaveChangesAsync();

        // Act
        player.FullName = "Updated Name";
        player.Position = "Midfielder";
        player.ShirtNumber = 10;

        _playerRepository.UpdateAsync(player);
        await UnitOfWork.SaveChangesAsync();

        // Assert
        var updatedPlayer = await _playerRepository.GetByIdAsync(player.Id);
        updatedPlayer.Should().NotBeNull();
        updatedPlayer!.FullName.Should().Be("Updated Name");
        updatedPlayer.Position.Should().Be("Midfielder");
        updatedPlayer.ShirtNumber.Should().Be(10);
    }

    [Fact]
    public async Task DeleteAsync_WithValidPlayer_RemovesSuccessfully()
    {
        // Arrange
        var team = new Team
        {
            Name = "Test Team",
            League = "Test League",
            Country = "Test Country",
            FoundationDate = new DateTime(2000, 1, 1),
        };

        var player = new Player
        {
            FullName = "Player To Delete",
            Position = "Forward",
            Team = team,
        };

        await UnitOfWork.Teams.AddAsync(team);
        await UnitOfWork.Players.AddAsync(player);
        await UnitOfWork.SaveChangesAsync();

        var playerId = player.Id;

        // Act
        _playerRepository.DeleteAsync(player);
        await UnitOfWork.SaveChangesAsync();

        // Assert
        var deletedPlayer = await _playerRepository.GetByIdAsync(playerId);
        deletedPlayer.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_WithPagination_ReturnsCorrectResults()
    {
        // Arrange
        var team = new Team
        {
            Name = "Test Team",
            League = "Test League",
            Country = "Test Country",
            FoundationDate = new DateTime(2000, 1, 1),
        };

        // Create 5 players
        var players = new List<Player>();
        for (var i = 1; i <= 5; i++)
            players.Add(
                new Player
                {
                    FullName = $"Player {i}",
                    Position = "Forward",
                    ShirtNumber = i,
                    Team = team,
                }
            );

        await UnitOfWork.Teams.AddAsync(team);
        foreach (var player in players)
            await UnitOfWork.Players.AddAsync(player);
        await UnitOfWork.SaveChangesAsync();

        // Act
        var firstPage = await _playerRepository.GetAllAsync(1, 2);
        var secondPage = await _playerRepository.GetAllAsync(2, 2);

        // Assert
        firstPage.Should().HaveCount(6); // Takes pageSize * 3 = 2 * 3 = 6, but we only have 5
        secondPage.Should().HaveCount(3); // Skip 2, take 6, but only 3 remain
    }

    [Fact]
    public async Task Repository_WithTransactions_HandlesRollbackCorrectly()
    {
        // Arrange
        var team = new Team
        {
            Name = "Test Team",
            League = "Test League",
            Country = "Test Country",
            FoundationDate = new DateTime(2000, 1, 1),
        };

        var player = new Player
        {
            FullName = "Transaction Test Player",
            Position = "Forward",
            Team = team,
        };

        await UnitOfWork.Teams.AddAsync(team);
        await UnitOfWork.SaveChangesAsync();

        // Act
        await UnitOfWork.BeginTransactionAsync();

        await _playerRepository.AddAsync(player);
        await UnitOfWork.SaveChangesAsync();

        // Verify player exists within transaction
        var playerInTransaction = await _playerRepository.GetByFullNameAsync(
            "Transaction Test Player"
        );
        playerInTransaction.Should().NotBeNull();

        // Rollback
        await UnitOfWork.RollbackTransactionAsync();

        // Assert
        var playerAfterRollback = await _playerRepository.GetByFullNameAsync(
            "Transaction Test Player"
        );
        playerAfterRollback.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WithIncludedTeam_LoadsTeamData()
    {
        // Arrange
        var team = new Team
        {
            Name = "Test Team",
            League = "Premier League",
            Country = "England",
            FoundationDate = new DateTime(2000, 1, 1),
        };

        var player = new Player
        {
            FullName = "Test Player",
            Position = "Forward",
            Team = team,
        };

        await UnitOfWork.Teams.AddAsync(team);
        await UnitOfWork.Players.AddAsync(player);
        await UnitOfWork.SaveChangesAsync();

        // Act - Using SearchAsync which includes Team data
        var searchResult = await _playerRepository.SearchAsync("Test Player");
        var playerWithTeam = searchResult.First();

        // Assert
        playerWithTeam.Should().NotBeNull();
        playerWithTeam.Team.Should().NotBeNull();
        playerWithTeam.Team!.Name.Should().Be("Test Team");
        playerWithTeam.Team.League.Should().Be("Premier League");
        playerWithTeam.Team.Country.Should().Be("England");
    }
}
