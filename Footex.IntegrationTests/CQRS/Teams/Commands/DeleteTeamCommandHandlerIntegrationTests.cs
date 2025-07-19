using Application.CQRS.Teams.Commands;
using FluentAssertions;
using Footex.IntegrationTests.Common;
using Infrastructure;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Footex.IntegrationTests.CQRS.Teams.Commands;

public class DeleteTeamCommandHandlerIntegrationTests
    : IClassFixture<FootexWebApplicationFactory>,
        IDisposable
{
    private readonly FootballDbContext _context;
    private readonly FootexWebApplicationFactory _factory;
    private readonly IMediator _mediator;
    private readonly IServiceScope _scope;

    public DeleteTeamCommandHandlerIntegrationTests(FootexWebApplicationFactory factory)
    {
        _factory = factory;
        _scope = _factory.Services.CreateScope();
        _mediator = _scope.ServiceProvider.GetRequiredService<IMediator>();
        _context = _scope.ServiceProvider.GetRequiredService<FootballDbContext>();
    }

    [Fact]
    public async Task Handle_WithValidCommand_DeletesTeamFromDatabase()
    {
        // Arrange
        var team = TestData.CreateTestDbTeam();
        _context.Teams.Add(team);
        await _context.SaveChangesAsync();

        var command = new DeleteTeamCommand { Id = team.Id };

        // Act
        var response = await _mediator.Send(command);

        // Assert
        response.Should().NotBeNull();
        response.Succeeded.Should().BeTrue();
        var deletedTeam = await _context.Teams.FindAsync(team.Id);
        deletedTeam.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithNonExistentTeam_ReturnsFailure()
    {
        // Arrange
        var command = new DeleteTeamCommand { Id = 99999 };

        // Act
        var response = await _mediator.Send(command);

        // Assert
        response.Should().NotBeNull();
        response.Succeeded.Should().BeFalse();
        response.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_DeletesTeamAndUnlinksCoaches()
    {
        // Arrange
        var team = TestData.CreateTestDbTeam();
        _context.Teams.Add(team);
        await _context.SaveChangesAsync();

        var coach = TestData.CreateTestDbCoach(team.Id);
        coach.TeamId = team.Id;
        _context.Coaches.Add(coach);
        await _context.SaveChangesAsync();

        var command = new DeleteTeamCommand { Id = team.Id };

        // Act
        var response = await _mediator.Send(command);

        // Assert
        response.Should().NotBeNull();
        response.Succeeded.Should().BeTrue();
        var deletedTeam = await _context.Teams.FindAsync(team.Id);
        deletedTeam.Should().BeNull();
        var updatedCoach = await _context.Coaches.FindAsync(coach.Id);
        updatedCoach.Should().NotBeNull();
        updatedCoach!.TeamId.Should().BeNull();
    }

    public void Dispose()
    {
        _scope?.Dispose();
    }
}
