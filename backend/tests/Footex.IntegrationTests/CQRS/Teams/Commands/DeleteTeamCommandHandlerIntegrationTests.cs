using Application.CQRS.Teams.Commands;
using FluentAssertions;
using Footex.IntegrationTests.Common;
using Infrastructure;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Footex.IntegrationTests.CQRS.Teams.Commands;

public class DeleteTeamCommandHandlerIntegrationTests(FootexWebApplicationFactory factory)
    : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task Handle_WithValidCommand_DeletesTeamFromDatabase()
    {
        // Arrange
        var team = TestData.CreateTestDbTeam();
        Context.Teams.Add(team);
        await Context.SaveChangesAsync();

        var command = new DeleteTeamCommand { Id = team.Id };

        // Act
        var response = await Mediator.Send(command);

        // Assert
        response.Should().NotBeNull();
        response.Succeeded.Should().BeTrue();
        var deletedTeam = await Context.Teams.FindAsync(team.Id);
        deletedTeam.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithNonExistentTeam_ReturnsFailure()
    {
        // Arrange
        var command = new DeleteTeamCommand { Id = 99999 };

        // Act
        var response = await Mediator.Send(command);

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
        Context.Teams.Add(team);
        await Context.SaveChangesAsync();

        var coach = TestData.CreateTestDbCoach(team.Id);
        coach.TeamId = team.Id;
        Context.Coaches.Add(coach);
        await Context.SaveChangesAsync();

        var command = new DeleteTeamCommand { Id = team.Id };

        // Act
        var response = await Mediator.Send(command);

        // Assert
        response.Should().NotBeNull();
        response.Succeeded.Should().BeTrue();
        var deletedTeam = await Context.Teams.FindAsync(team.Id);
        deletedTeam.Should().BeNull();
        var updatedCoach = await Context.Coaches.FindAsync(coach.Id);
        updatedCoach.Should().NotBeNull();
        updatedCoach!.TeamId.Should().BeNull();
    }
}
