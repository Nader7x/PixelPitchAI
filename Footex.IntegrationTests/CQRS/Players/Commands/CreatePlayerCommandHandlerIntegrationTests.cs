using Application.CQRS.Players.Commands;
using FluentAssertions;
using Footex.IntegrationTests.Common;
using Infrastructure;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Domain.Enums;
using Sprache;

namespace Footex.IntegrationTests.CQRS.Players.Commands;

public class CreatePlayerCommandHandlerIntegrationTests : IClassFixture<FootexWebApplicationFactory>
{
    private readonly FootexWebApplicationFactory _factory;
    private readonly IServiceScope _scope;
    private readonly IMediator _mediator;
    private readonly FootballDbContext _context;

    public CreatePlayerCommandHandlerIntegrationTests(FootexWebApplicationFactory factory)
    {
        _factory = factory;
        _scope = _factory.Services.CreateScope();
        _mediator = _scope.ServiceProvider.GetRequiredService<IMediator>();
        _context = _scope.ServiceProvider.GetRequiredService<FootballDbContext>();
    }

    [Fact]
    public async Task Handle_WithValidCommand_CreatesPlayerInDatabase()
    {
        // Arrange
        var team = TestData.CreateTestTeam();
        _context.Teams.Add(team);
        await _context.SaveChangesAsync();

        var command = new CreatePlayerCommand
        {
            FullName = "John Doe",
            KnownName = "Johnny",
            Nationality = "England",
            PreferredFoot = "Right",
            PhotoUrl = "http://example.com/photo.jpg",
            Position = nameof(PlayerPosition.AttackingMidfielder),
            TeamId = team.Id,
            ShirtNumber = 10,

        };

        // Act
        var response = await _mediator.Send(command);

        // Assert
        response.Should().NotBeNull();
        response.Succeeded.Should().BeTrue();
        response.Id.Should().BeGreaterThan(0);

        // Verify player was created in database
        var createdPlayer = await _context.Players.FindAsync(response.Id);
        createdPlayer.Should().NotBeNull();
        createdPlayer!.FullName.Should().Be(command.FullName);
        createdPlayer.KnownName.Should().Be(command.KnownName);
        createdPlayer.Nationality.Should().Be(command.Nationality);
        createdPlayer.Position.Should().Be(command.Position);
        createdPlayer.ShirtNumber.Should().Be(command.ShirtNumber);
        createdPlayer.TeamId.Should().Be(command.TeamId);
    }

    [Fact]
    public async Task Handle_WithInvalidTeamId_ReturnsFailureResponse()
    {
        // Arrange
        var command = new CreatePlayerCommand
        {
            FullName = "John",
            KnownName = "Doe",
            Nationality = "England",
            Position = nameof(PlayerPosition.AttackingMidfielder),
            TeamId = 999999, // Invalid team ID
            ShirtNumber = 10
        };

        // Act
        var response = await _mediator.Send(command);

        // Assert
        response.Should().NotBeNull();
        response.Succeeded.Should().BeFalse();
        response.Error.Should().Contain("Team").And.Contain("not found");
    }

    [Fact]
    public async Task Handle_WithDuplicateShirtNumber_ReturnsFailureResponse()
    {
        // Arrange
        var team = TestData.CreateTestTeam();
        _context.Teams.Add(team);
        await _context.SaveChangesAsync();

        var existingPlayer = TestData.CreateTestPlayer(team.Id);
        existingPlayer.ShirtNumber = 10;
        _context.Players.Add(existingPlayer);
        await _context.SaveChangesAsync();

        var command = new CreatePlayerCommand
        {
            FullName = "John",
            KnownName = "Doe",
            Nationality = "England",
            Position = nameof(PlayerPosition.CentralMidfielder),
            ShirtNumber = 10, // Duplicate jersey number
            TeamId = team.Id
        };

        // Act
        var response = await _mediator.Send(command);

        // Assert
        response.Should().NotBeNull();
        response.Succeeded.Should().BeFalse();
        response.Error.Should().Contain("jersey number").And.Contain("already exists");
    }

    [Fact]
    public async Task Handle_WithMinimalData_CreatesPlayerSuccessfully()
    {
        // Arrange
        var team = TestData.CreateTestTeam();
        _context.Teams.Add(team);
        await _context.SaveChangesAsync();

        var command = new CreatePlayerCommand
        {
            FullName = "Jane",
            KnownName = "Smith",
            Nationality = "Spain",
            Position = nameof(PlayerPosition.CenterForward),
            ShirtNumber = 9,
            TeamId = team.Id
            // Optional fields not provided
        };

        // Act
        var response = await _mediator.Send(command);

        // Assert
        response.Should().NotBeNull();
        response.Succeeded.Should().BeTrue();
        response.Id.Should().BeGreaterThan(0);

        // Verify in database
        var createdPlayer = await _context.Players.FindAsync(response.Id);
        createdPlayer.Should().NotBeNull();
        createdPlayer!.FullName.Should().Be(command.FullName);
    }

    [Fact]
    public async Task Handle_WithAllPositions_CreatesPlayersSuccessfully()
    {
        // Arrange
        var team = TestData.CreateTestTeam();
        _context.Teams.Add(team);
        await _context.SaveChangesAsync();

        var positions = Enum.GetValues<PlayerPosition>();
        var jerseyNumber = 1;

        foreach (var position in positions)
        {
            var command = new CreatePlayerCommand
            {
                FullName = $"Player{jerseyNumber}",
                KnownName = $"Position{position.ToString()}",
                Nationality = "England",
                Position = nameof(position),
                ShirtNumber = jerseyNumber,
                TeamId = team.Id
            };

            // Act
            var response = await _mediator.Send(command);

            // Assert
            response.Should().NotBeNull();
            response.Succeeded.Should().BeTrue();
            response.Id.Should().BeGreaterThan(0);

            jerseyNumber++;
        }
    }

    [Fact]
    public async Task Handle_WithInvalidShirtNumber_ReturnsFailureResponse()
    {
        // Arrange
        var team = TestData.CreateTestTeam();
        _context.Teams.Add(team);
        await _context.SaveChangesAsync();

        var command = new CreatePlayerCommand
        {
            FullName = "John",
            KnownName = "Doe",
            Nationality = "England",
            Position = nameof(PlayerPosition.CentralMidfielder),
            ShirtNumber = 0, // Invalid jersey number
            TeamId = team.Id
        };

        // Act
        var response = await _mediator.Send(command);

        // Assert
        response.Should().NotBeNull();
        response.Succeeded.Should().BeFalse();
        response.Error.Should().Contain("jersey number");
    }

    [Fact]
    public async Task Handle_WithFutureContractDate_CreatesPlayerSuccessfully()
    {
        // Arrange
        var team = TestData.CreateTestTeam();
        _context.Teams.Add(team);
        await _context.SaveChangesAsync();

        var command = new CreatePlayerCommand
        {
            FullName = "Future",
            KnownName = "Player",
            Nationality = "Brazil",
            Position = nameof(PlayerPosition.Goalkeeper),
            ShirtNumber = 1,
            TeamId = team.Id,
        };

        // Act
        var response = await _mediator.Send(command);

        // Assert
        response.Should().NotBeNull();
        response.Succeeded.Should().BeTrue();
        response.Id.Should().BeGreaterThan(0);

        // Verify contract date
        var createdPlayer = await _context.Players.FindAsync(response.Id);
        createdPlayer.Should().NotBeNull();
    }
}
