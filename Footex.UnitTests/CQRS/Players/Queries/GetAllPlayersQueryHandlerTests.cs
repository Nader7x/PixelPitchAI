using System.Linq.Expressions;
using Application.CQRS.Players.Queries;
using Application.Dtos;
using Application.Mappers;
using AutoFixture;
using Domain.Interfaces;
using Domain.Models;
using FluentAssertions;
using Moq;
using Xunit;

namespace Footex.UnitTests.CQRS.Players.Queries;

public class GetAllPlayersQueryHandlerTests
{
    private readonly Fixture _fixture;
    private readonly GetAllPlayersQueryHandler _handler;
    private readonly Mock<PlayerMapper> _playerMapperMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;

    public GetAllPlayersQueryHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _playerMapperMock = new Mock<PlayerMapper>();
        _handler = new GetAllPlayersQueryHandler(_unitOfWorkMock.Object, _playerMapperMock.Object);

        _fixture = new Fixture();
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }

    [Fact]
    public async Task Handle_WithNoFilters_ReturnsAllPlayers()
    {
        // Arrange
        var query = new GetAllPlayersQuery();
        var players = new List<Player>
        {
            CreateValidPlayer(1),
            CreateValidPlayer(2),
            CreateValidPlayer(3)
        };
        var expectedPlayerDtos = players.Select(p => new PlayerDto
        {
            Id = p.Id,
            FullName = p.FullName,
            Nationality = p.Nationality,
            Position = p.Position
        }).ToList();

        _unitOfWorkMock.Setup(x => x.Players.GetAllAsync(null, null))
            .ReturnsAsync(players);
        _playerMapperMock.Setup(x => x.ToDtoList(players))
            .Returns(expectedPlayerDtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Players.Should().NotBeNull();
        result.Players!.Count.Should().Be(3);
        result.Error.Should().BeNull();

        _unitOfWorkMock.Verify(x => x.Players.GetAllAsync(null, null), Times.Once);
        _playerMapperMock.Verify(x => x.ToDtoList(players), Times.Once);
    }

    [Fact]
    public async Task Handle_WithPagination_ReturnsPagedPlayers()
    {
        // Arrange
        var pageNumber = 1;
        var pageSize = 10;
        var query = new GetAllPlayersQuery { PageNumber = pageNumber, PageSize = pageSize };
        var players = new List<Player> { CreateValidPlayer(1), CreateValidPlayer(2) };
        var expectedPlayerDtos = players.Select(p => new PlayerDto
        {
            Id = p.Id,
            FullName = p.FullName
        }).ToList();

        _unitOfWorkMock.Setup(x => x.Players.GetAllAsync(pageNumber, pageSize))
            .ReturnsAsync(players);
        _playerMapperMock.Setup(x => x.ToDtoList(players))
            .Returns(expectedPlayerDtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Players.Should().NotBeNull();
        result.Players!.Count.Should().Be(2);
        result.Error.Should().BeNull();

        _unitOfWorkMock.Verify(x => x.Players.GetAllAsync(pageNumber, pageSize), Times.Once);
        _playerMapperMock.Verify(x => x.ToDtoList(players), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNationalityFilter_ReturnsFilteredPlayers()
    {
        // Arrange
        var nationality = "England";
        var query = new GetAllPlayersQuery { Nationality = nationality };
        var players = new List<Player>
        {
            CreateValidPlayer(1),
            CreateValidPlayer(2)
        };
        players.ForEach(p => p.Nationality = nationality);
        var expectedPlayerDtos = players.Select(p => new PlayerDto
        {
            Id = p.Id,
            Nationality = p.Nationality
        }).ToList();

        _unitOfWorkMock.Setup(x => x.Players.GetByNationalityAsync(nationality))
            .ReturnsAsync(players);
        _playerMapperMock.Setup(x => x.ToDtoList(players))
            .Returns(expectedPlayerDtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Players.Should().NotBeNull();
        result.Players!.Count.Should().Be(2);
        result.Players.All(p => p.Nationality == nationality).Should().BeTrue();
        result.Error.Should().BeNull();

        _unitOfWorkMock.Verify(x => x.Players.GetByNationalityAsync(nationality), Times.Once);
        _playerMapperMock.Verify(x => x.ToDtoList(players), Times.Once);
    }

    [Fact]
    public async Task Handle_WithPreferredFootFilter_ReturnsFilteredPlayers()
    {
        // Arrange
        var preferredFoot = "Right";
        var query = new GetAllPlayersQuery { PreferredFoot = preferredFoot };
        var players = new List<Player>
        {
            CreateValidPlayer(1),
            CreateValidPlayer(2)
        };
        players.ForEach(p => p.PreferredFoot = preferredFoot);
        var expectedPlayerDtos = players.Select(p => new PlayerDto
        {
            Id = p.Id,
            PreferredFoot = p.PreferredFoot
        }).ToList();

        _unitOfWorkMock.Setup(x => x.Players.GetByPreferredFootAsync(preferredFoot))
            .ReturnsAsync(players);
        _playerMapperMock.Setup(x => x.ToDtoList(players))
            .Returns(expectedPlayerDtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Players.Should().NotBeNull();
        result.Players!.Count.Should().Be(2);
        result.Players.All(p => p.PreferredFoot == preferredFoot).Should().BeTrue();
        result.Error.Should().BeNull();

        _unitOfWorkMock.Verify(x => x.Players.GetByPreferredFootAsync(preferredFoot), Times.Once);
        _playerMapperMock.Verify(x => x.ToDtoList(players), Times.Once);
    }

    [Fact]
    public async Task Handle_WithTeamIdFilter_ReturnsFilteredPlayers()
    {
        // Arrange
        var teamId = 5;
        var query = new GetAllPlayersQuery { TeamId = teamId };
        var players = new List<Player>
        {
            CreateValidPlayer(1),
            CreateValidPlayer(2)
        };
        players.ForEach(p => p.TeamId = teamId);
        var expectedPlayerDtos = players.Select(p => new PlayerDto
        {
            Id = p.Id,
            TeamId = p.TeamId
        }).ToList();

        _unitOfWorkMock.Setup(x => x.Players.FindAsync(It.IsAny<Expression<Func<Player, bool>>>()))
            .ReturnsAsync(players.First());
        _playerMapperMock.Setup(x => x.ToDtoList(players))
            .Returns(expectedPlayerDtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Players.Should().NotBeNull();
        result.Players!.Count.Should().Be(2);
        result.Players.All(p => p.TeamId == teamId).Should().BeTrue();
        result.Error.Should().BeNull();

        _unitOfWorkMock.Verify(x => x.Players.FindAsync(It.IsAny<Expression<Func<Player, bool>>>()), Times.Once);
        _playerMapperMock.Verify(x => x.ToDtoList(players), Times.Once);
    }

    [Fact]
    public async Task Handle_WithEmptyResult_ReturnsEmptyList()
    {
        // Arrange
        var query = new GetAllPlayersQuery();
        var players = new List<Player>();
        var expectedPlayerDtos = new List<PlayerDto>();

        _unitOfWorkMock.Setup(x => x.Players.GetAllAsync(null, null))
            .ReturnsAsync(players);
        _playerMapperMock.Setup(x => x.ToDtoList(players))
            .Returns(expectedPlayerDtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Players.Should().NotBeNull();
        result.Players!.Count.Should().Be(0);
        result.Error.Should().BeNull();

        _unitOfWorkMock.Verify(x => x.Players.GetAllAsync(null, null), Times.Once);
        _playerMapperMock.Verify(x => x.ToDtoList(players), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenExceptionThrown_ReturnsFailureResponse()
    {
        // Arrange
        var query = new GetAllPlayersQuery();
        var exceptionMessage = "Database connection failed";

        _unitOfWorkMock.Setup(x => x.Players.GetAllAsync(null, null))
            .ThrowsAsync(new Exception(exceptionMessage));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.Players.Should().BeNull();
        result.Error.Should().Be(exceptionMessage);

        _unitOfWorkMock.Verify(x => x.Players.GetAllAsync(null, null), Times.Once);
        _playerMapperMock.Verify(x => x.ToDtoList(It.IsAny<IEnumerable<Player>>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithNationalityException_ReturnsFailureResponse()
    {
        // Arrange
        var nationality = "Spain";
        var query = new GetAllPlayersQuery { Nationality = nationality };
        var exceptionMessage = "Nationality filter failed";

        _unitOfWorkMock.Setup(x => x.Players.GetByNationalityAsync(nationality))
            .ThrowsAsync(new Exception(exceptionMessage));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.Players.Should().BeNull();
        result.Error.Should().Be(exceptionMessage);

        _unitOfWorkMock.Verify(x => x.Players.GetByNationalityAsync(nationality), Times.Once);
        _playerMapperMock.Verify(x => x.ToDtoList(It.IsAny<IEnumerable<Player>>()), Times.Never);
    }

    [Fact]
    public async Task Handle_FilterPriorityTest_NationalityTakesPrecedence()
    {
        // Arrange
        var nationality = "Brazil";
        var preferredFoot = "Left";
        var teamId = 3;
        var query = new GetAllPlayersQuery
        {
            Nationality = nationality,
            PreferredFoot = preferredFoot,
            TeamId = teamId
        };
        var players = new List<Player> { CreateValidPlayer(1) };
        var expectedPlayerDtos = new List<PlayerDto> { new() { Id = 1 } };

        _unitOfWorkMock.Setup(x => x.Players.GetByNationalityAsync(nationality))
            .ReturnsAsync(players);
        _playerMapperMock.Setup(x => x.ToDtoList(players))
            .Returns(expectedPlayerDtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();

        // Verify only nationality filter was called (it has priority)
        _unitOfWorkMock.Verify(x => x.Players.GetByNationalityAsync(nationality), Times.Once);
        _unitOfWorkMock.Verify(x => x.Players.GetByPreferredFootAsync(It.IsAny<string>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.Players.FindAsync(It.IsAny<Expression<Func<Player, bool>>>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.Players.GetAllAsync(It.IsAny<int?>(), It.IsAny<int?>()), Times.Never);
    }

    [Fact]
    public async Task Handle_FilterPriorityTest_PreferredFootSecondPriority()
    {
        // Arrange
        var preferredFoot = "Left";
        var teamId = 3;
        var query = new GetAllPlayersQuery
        {
            PreferredFoot = preferredFoot,
            TeamId = teamId
        };
        var players = new List<Player> { CreateValidPlayer(1) };
        var expectedPlayerDtos = new List<PlayerDto> { new() { Id = 1 } };

        _unitOfWorkMock.Setup(x => x.Players.GetByPreferredFootAsync(preferredFoot))
            .ReturnsAsync(players);
        _playerMapperMock.Setup(x => x.ToDtoList(players))
            .Returns(expectedPlayerDtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();

        // Verify only preferred foot filter was called (it has priority over TeamId)
        _unitOfWorkMock.Verify(x => x.Players.GetByPreferredFootAsync(preferredFoot), Times.Once);
        _unitOfWorkMock.Verify(x => x.Players.FindAsync(It.IsAny<Expression<Func<Player, bool>>>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.Players.GetAllAsync(It.IsAny<int?>(), It.IsAny<int?>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithEmptyStringFilters_TreatedAsNoFilter()
    {
        // Arrange
        var query = new GetAllPlayersQuery
        {
            Nationality = "",
            PreferredFoot = "   " // whitespace only
        };
        var players = new List<Player> { CreateValidPlayer(1) };
        var expectedPlayerDtos = new List<PlayerDto> { new() { Id = 1 } };

        _unitOfWorkMock.Setup(x => x.Players.GetAllAsync(null, null))
            .ReturnsAsync(players);
        _playerMapperMock.Setup(x => x.ToDtoList(players))
            .Returns(expectedPlayerDtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();

        // Verify GetAllAsync was called instead of filter methods
        _unitOfWorkMock.Verify(x => x.Players.GetAllAsync(null, null), Times.Once);
        _unitOfWorkMock.Verify(x => x.Players.GetByNationalityAsync(It.IsAny<string>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.Players.GetByPreferredFootAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_MapperCorrectlyTransformsPlayers()
    {
        // Arrange
        var query = new GetAllPlayersQuery();
        var players = new List<Player> { CreateValidPlayer(1) };
        var expectedPlayerDtos = new List<PlayerDto>
        {
            new()
            {
                Id = 1,
                FullName = "Test Player",
                KnownName = "Test",
                Nationality = "England",
                Position = "Forward",
                PreferredFoot = "Right",
                TeamId = 1,
                ShirtNumber = 10
            }
        };

        _unitOfWorkMock.Setup(x => x.Players.GetAllAsync(null, null))
            .ReturnsAsync(players);
        _playerMapperMock.Setup(x => x.ToDtoList(players))
            .Returns(expectedPlayerDtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Players!.Count.Should().Be(1);

        var playerDto = result.Players.First();
        playerDto.Id.Should().Be(1);
        playerDto.FullName.Should().Be("Test Player");
        playerDto.KnownName.Should().Be("Test");
        playerDto.Nationality.Should().Be("England");
        playerDto.Position.Should().Be("Forward");
        playerDto.PreferredFoot.Should().Be("Right");
        playerDto.TeamId.Should().Be(1);
        playerDto.ShirtNumber.Should().Be(10);

        _playerMapperMock.Verify(x => x.ToDtoList(players), Times.Once);
    }

    private Player CreateValidPlayer(int id)
    {
        var player = _fixture.Create<Player>();
        player.Id = id;
        player.FullName = "John Doe";
        player.KnownName = "Johnny";
        player.Nationality = "England";
        player.Position = "Forward";
        player.PreferredFoot = "Right";
        player.TeamId = 1;
        player.ShirtNumber = 10;
        return player;
    }
}