using Application.CQRS.Players.Queries;
using Application.Dtos;
using Application.Interfaces;
using AutoFixture;
using Domain.Interfaces;
using Domain.Models;
using FluentAssertions;
using Footex.UnitTests.Common;
using Moq;
using Xunit;

namespace Footex.UnitTests.CQRS.Players.Queries;

public class GetPlayerByIdQueryHandlerTests
{
    private readonly NoRecursionFixture _fixture;
    private readonly GetPlayerByIdQueryHandler _handler;
    private readonly Mock<IPlayerMapper> _iPlayerMapperMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;

    public GetPlayerByIdQueryHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _iPlayerMapperMock = new Mock<IPlayerMapper>();
        _handler = new GetPlayerByIdQueryHandler(_unitOfWorkMock.Object, _iPlayerMapperMock.Object);

        _fixture = new NoRecursionFixture();
        _fixture
            .Behaviors.OfType<ThrowingRecursionBehavior>()
            .ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }

    [Fact]
    public async Task Handle_WithValidId_ReturnsSuccessResponse()
    {
        // Arrange
        var playerId = 1;
        var query = new GetPlayerByIdQuery { Id = playerId };
        var player = CreateValidPlayer(playerId);
        var expectedPlayerDto = new PlayerDto
        {
            Id = playerId,
            FullName = player.FullName,
            Nationality = player.Nationality,
            Position = player.Position,
        };

        _unitOfWorkMock.Setup(x => x.Players.GetByIdAsync(playerId)).ReturnsAsync(player);
        _iPlayerMapperMock.Setup(x => x.ToDto(player)).Returns(expectedPlayerDto);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.NotFound.Should().BeFalse();
        result.Player.Should().NotBeNull();
        result.Player!.Id.Should().Be(playerId);
        result.Error.Should().BeNull();

        _unitOfWorkMock.Verify(x => x.Players.GetByIdAsync(playerId), Times.Once);
        _iPlayerMapperMock.Verify(x => x.ToDto(player), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistentId_ReturnsNotFoundResponse()
    {
        // Arrange
        var playerId = 999;
        var query = new GetPlayerByIdQuery { Id = playerId };

        _unitOfWorkMock.Setup(x => x.Players.GetByIdAsync(playerId)).ReturnsAsync((Player?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.NotFound.Should().BeTrue();
        result.Player.Should().BeNull();
        result.Error.Should().Be($"Player with ID {playerId} not found");

        _unitOfWorkMock.Verify(x => x.Players.GetByIdAsync(playerId), Times.Once);
        _iPlayerMapperMock.Verify(x => x.ToDto(It.IsAny<Player>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenExceptionThrown_ReturnsFailureResponse()
    {
        // Arrange
        var playerId = 1;
        var query = new GetPlayerByIdQuery { Id = playerId };
        var exceptionMessage = "Database connection failed";

        _unitOfWorkMock
            .Setup(x => x.Players.GetByIdAsync(playerId))
            .ThrowsAsync(new Exception(exceptionMessage));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.NotFound.Should().BeFalse();
        result.Player.Should().BeNull();
        result.Error.Should().Be(exceptionMessage);

        _unitOfWorkMock.Verify(x => x.Players.GetByIdAsync(playerId), Times.Once);
        _iPlayerMapperMock.Verify(x => x.ToDto(It.IsAny<Player>()), Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-999)]
    public async Task Handle_WithInvalidId_ReturnsNotFoundResponse(int invalidId)
    {
        // Arrange
        var query = new GetPlayerByIdQuery { Id = invalidId };

        _unitOfWorkMock.Setup(x => x.Players.GetByIdAsync(invalidId)).ReturnsAsync((Player?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.NotFound.Should().BeTrue();
        result.Player.Should().BeNull();
        result.Error.Should().Be($"Player with ID {invalidId} not found");

        _unitOfWorkMock.Verify(x => x.Players.GetByIdAsync(invalidId), Times.Once);
        _iPlayerMapperMock.Verify(x => x.ToDto(It.IsAny<Player>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithValidPlayer_MapsCorrectly()
    {
        // Arrange
        var playerId = 1;
        var query = new GetPlayerByIdQuery { Id = playerId };
        var player = CreateValidPlayer(playerId);
        var expectedPlayerDto = new PlayerDto
        {
            Id = playerId,
            FullName = player.FullName,
            KnownName = player.KnownName,
            Nationality = player.Nationality,
            Position = player.Position,
            PreferredFoot = player.PreferredFoot,
            TeamId = player.TeamId,
            ShirtNumber = player.ShirtNumber,
        };

        _unitOfWorkMock.Setup(x => x.Players.GetByIdAsync(playerId)).ReturnsAsync(player);
        _iPlayerMapperMock.Setup(x => x.ToDto(player)).Returns(expectedPlayerDto);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Player.Should().NotBeNull();
        result.Player!.Id.Should().Be(expectedPlayerDto.Id);
        result.Player.FullName.Should().Be(expectedPlayerDto.FullName);
        result.Player.KnownName.Should().Be(expectedPlayerDto.KnownName);
        result.Player.Nationality.Should().Be(expectedPlayerDto.Nationality);
        result.Player.Position.Should().Be(expectedPlayerDto.Position);
        result.Player.PreferredFoot.Should().Be(expectedPlayerDto.PreferredFoot);
        result.Player.TeamId.Should().Be(expectedPlayerDto.TeamId);
        result.Player.ShirtNumber.Should().Be(expectedPlayerDto.ShirtNumber);
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
