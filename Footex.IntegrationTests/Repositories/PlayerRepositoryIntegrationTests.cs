using Domain.Interfaces;
using Domain.Models;
using Domain.Repositories;
using FluentAssertions;
using Footex.IntegrationTests.Common;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Footex.IntegrationTests.Repositories;

public class PlayerRepositoryIntegrationTests : BaseIntegrationTest
{
    private readonly IPlayerRepository _playerRepository;

    public PlayerRepositoryIntegrationTests(FootexWebApplicationFactory factory)
        : base(factory)
    {
        _playerRepository =
            FactoryServiceScope.ServiceProvider.GetRequiredService<IPlayerRepository>();
    }

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
            Name = "Test Team" + new Random().Next(1, 1000),
            League = "Test League",
            Country = "Test Country",
            FoundationDate = new DateTime(2000, 1, 1),
        };

        var player1 = new Player
        {
            FullName = "American Player 1",
            KnownName = "Ameri1",
            Nationality = "USA",
            Position = "Forward",
            Team = team,
        };

        var player2 = new Player
        {
            FullName = "American Player 2",
            KnownName = "Ameri2",
            Nationality = "USA",
            Position = "Midfielder",
            Team = team,
        };

        var player3 = new Player
        {
            FullName = "Brazilian Player",
            KnownName = "Braz",
            Nationality = "Brazil",
            Position = "Defender",
            Team = team,
        };

        // Act
        await UnitOfWork.Teams.AddAsync(team);
        await UnitOfWork.Players.AddRangeAsync(CancellationToken.None, player1, player2, player3);
        await UnitOfWork.SaveChangesAsync();

        var result = await _playerRepository.GetByNationalityAsync("USA");

        // Assert
        var players = result as Player[] ?? result.ToArray();
        players.Should().HaveCount(2);
        players.All(p => p.Nationality == "USA").Should().BeTrue();
        players.Should().Contain(p => p.FullName == "American Player 1");
        players.Should().Contain(p => p.FullName == "American Player 2");
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
            Name = "Test Team" + new Random().Next(1, 1000),
            League = "Test League",
            Country = "Test Country",
            FoundationDate = new DateTime(2000, 1, 1),
        };

        var rightFootedPlayer1 = new Player
        {
            FullName = "Right Footed Player 1",
            KnownName = "Righty",
            PreferredFoot = "Right",
            Position = "Forward",
            Team = team,
        };

        var rightFootedPlayer2 = new Player
        {
            FullName = "Right Footed Player 2",
            KnownName = "Righty2",
            PreferredFoot = "Right",
            Position = "Midfielder",
            Team = team,
        };

        var leftFootedPlayer = new Player
        {
            FullName = "Left Footed Player",
            KnownName = "Lefty",
            PreferredFoot = "Left",
            Position = "Defender",
            Team = team,
        };

        // Act
        await UnitOfWork.Teams.AddAsync(team);
        await UnitOfWork.Players.AddRangeAsync(
            CancellationToken.None,
            rightFootedPlayer1,
            rightFootedPlayer2,
            leftFootedPlayer
        );
        await UnitOfWork.SaveChangesAsync();

        var rightFootedResult = await _playerRepository.GetByPreferredFootAsync("Right");
        var leftFootedResult = await _playerRepository.GetByPreferredFootAsync("Left");

        // Assert
        var rightFootedPlayers = rightFootedResult as Player[] ?? rightFootedResult.ToArray();
        rightFootedPlayers.Should().HaveCount(2);
        rightFootedPlayers.All(p => p.PreferredFoot == "Right").Should().BeTrue();

        var leftFootedPlayers = leftFootedResult as Player[] ?? leftFootedResult.ToArray();
        leftFootedPlayers.Should().HaveCount(1);
        leftFootedPlayers.First().PreferredFoot.Should().Be("Left");
        leftFootedPlayers.First().FullName.Should().Be("Left Footed Player");
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
            Name = "Test Team" + new Random().Next(20, 1000),
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
            KnownName = "JJ",
            Position = "Midfielder",
            Team = team,
        };

        var player3 = new Player
        {
            FullName = "Bob Wilson",
            KnownName = "Bobby",
            Position = "Defender",
            Team = team,
        };

        // Act
        await UnitOfWork.Teams.AddAsync(team);
        await UnitOfWork.Players.AddRangeAsync(CancellationToken.None, player1, player2, player3);
        await UnitOfWork.SaveChangesAsync();

        var searchResult = await _playerRepository.SearchAsync("John");

        // Assert
        var matchingPlayers = searchResult as Player[] ?? searchResult.ToArray();
        matchingPlayers.Should().HaveCount(2);
        matchingPlayers.Should().Contain(p => p.FullName == "John Smith");
        matchingPlayers.Should().Contain(p => p.FullName == "Jane Johnson");
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
            FullName = "Player Search 1",
            KnownName = "PS1",
            Position = "Forward",
            Team = team1,
        };

        var player2 = new Player
        {
            FullName = "Player Search 2",
            KnownName = "PS2",
            Position = "Midfielder",
            Team = team1,
        };

        var player3 = new Player
        {
            FullName = "Player Search 3",
            KnownName = "PS3",
            Position = "Defender",
            Team = team2,
        };

        // Act
        await UnitOfWork.Teams.AddRangeAsync(CancellationToken.None, team1, team2);
        await UnitOfWork.Players.AddRangeAsync(CancellationToken.None, player1, player2, player3);
        await UnitOfWork.SaveChangesAsync();

        var searchResult = await _playerRepository.SearchAsync("Manchester");

        // Assert
        var resultingPlayers = searchResult as Player[] ?? searchResult.ToArray();
        resultingPlayers.Should().HaveCount(2);
        resultingPlayers.All(p => p.Team!.Name == "Manchester United").Should().BeTrue();
        resultingPlayers.Should().Contain(p => p.FullName == "Player Search 1");
        resultingPlayers.Should().Contain(p => p.FullName == "Player Search 2");
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
        // Free Db
        Context.Players.RemoveRange(Context.Players);
        await Context.SaveChangesAsync();
        // Arrange
        var team = new Team
        {
            Name = "Test Team" + new Random().Next(1, 1000),
            League = "Test League",
            Country = "Test Country",
            FoundationDate = new DateTime(2000, 1, 1),
        };

        var forward1 = new Player
        {
            FullName = "Forward 1",
            KnownName = "ForwardOne",
            Position = "Forward",
            ShirtNumber = 9,
            Team = team,
        };

        var forward2 = new Player
        {
            FullName = "Forward 2",
            KnownName = "ForwardTwo",
            Position = "Forward",
            ShirtNumber = 11,
            Team = team,
        };

        var midfielder = new Player
        {
            FullName = "Midfielder",
            KnownName = "MidfieldMaster",
            Position = "Midfielder",
            ShirtNumber = 8,
            Team = team,
        };

        // Act
        await UnitOfWork.Teams.AddAsync(team);
        await UnitOfWork.Players.AddRangeAsync(
            CancellationToken.None,
            forward1,
            forward2,
            midfielder
        );
        await UnitOfWork.SaveChangesAsync();

        var forwards = await _playerRepository.FindAsync(p => p.Position == "Forward");

        // Assert
        var players = forwards as Player[] ?? forwards.ToArray();
        players.Should().HaveCount(2);
        players.All(p => p.Position == "Forward").Should().BeTrue();
        players.Should().Contain(p => p.FullName == "Forward 1");
        players.Should().Contain(p => p.FullName == "Forward 2");
    }

    [Fact]
    public async Task AddAsync_WithValidPlayer_SavesSuccessfully()
    {
        // Arrange
        var team = new Team
        {
            Name = "Test Team" + new Random().Next(1, 1000),
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
            Name = "Test Team" + new Random().Next(30, 1000),
            League = "Test League",
            Country = "Test Country",
            FoundationDate = new DateTime(2000, 1, 1),
        };

        var player = new Player
        {
            FullName = "Original Name",
            KnownName = "OriginalKnown",
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

        _playerRepository.Update(player);
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
            Name = "Test Team" + new Random().Next(40, 1000),
            League = "Test League",
            Country = "Test Country",
            FoundationDate = new DateTime(2000, 1, 1),
        };

        var player = new Player
        {
            FullName = "Player To Delete",
            KnownName = "DeleteMe",
            Position = "Forward",
            ShirtNumber = 9,
            PhotoUrl = "http://example.com/photo.jpg",
            Team = team,
        };

        await UnitOfWork.Teams.AddAsync(team);
        await UnitOfWork.Players.AddAsync(player);
        await UnitOfWork.SaveChangesAsync();

        var playerId = player.Id;

        // Act
        _playerRepository.Delete(player);
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
            Name = "Test Team" + new Random().Next(50, 1000),
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
                    FullName = $"Player List {i}",
                    KnownName = $"PLS{i}",
                    Position = "Forward",
                    ShirtNumber = i,
                    Team = team,
                }
            );

        await UnitOfWork.Teams.AddAsync(team);
        await UnitOfWork.Players.AddRangeAsync(CancellationToken.None, [.. players]);
        await UnitOfWork.SaveChangesAsync();

        // Act
        var firstPage = await _playerRepository.GetAllAsync(1, 2);
        var secondPage = await _playerRepository.GetAllAsync(2, 2);

        // Assert
        firstPage.Should().HaveCount(2); // Takes pageSize = 2
        secondPage.Should().HaveCount(2); // Skip 2
    }

    [Fact]
    public async Task Repository_WithTransactions_HandlesRollbackCorrectly()
    {
        // Arrange
        var team = new Team
        {
            Name = "Test Team" + new Random().Next(60, 1000),
            League = "Test League",
            Country = "Test Country",
            FoundationDate = new DateTime(2000, 1, 1),
        };

        var player = new Player
        {
            FullName = "Transaction Test Player",
            KnownName = "TransPlayer",
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
            Name = "Test Team" + new Random().Next(70, 1000),
            League = "Premier League",
            Country = "England",
            FoundationDate = new DateTime(2000, 1, 1),
        };

        var player = new Player
        {
            FullName = "Test Player with Team",
            KnownName = "Tester",
            Position = "Forward",
            Team = team,
        };

        await UnitOfWork.Teams.AddAsync(team);
        await UnitOfWork.Players.AddAsync(player);
        await UnitOfWork.SaveChangesAsync();

        // Act - Using SearchAsync which includes Team data
        var searchResult = await _playerRepository.SearchAsync("Test Player with Team");
        var playerWithTeam = searchResult.First();

        // Assert
        playerWithTeam.Should().NotBeNull();
        playerWithTeam.Team.Should().NotBeNull();
        playerWithTeam.Team?.League.Should().Be("Premier League");
        playerWithTeam.Team?.Country.Should().Be("England");
    }
}
