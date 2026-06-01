using Application.CQRS.Coaches.Commands;
using Domain.Interfaces;
using FluentAssertions;
using Footex.IntegrationTests.Common;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Footex.IntegrationTests.CQRS.Coaches.Commands;

public class DeleteCoachCommandHandlerIntegrationTests(FootexWebApplicationFactory factory)
    : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task Handle_ValidCoachId_DeletesCoachSuccessfully()
    {
        // Arrange
        var coach = TestData.CreateTestDbCoach(firstName: "Mauricio", lastName: "Pochettino");
        await UnitOfWork.Coaches.AddAsync(coach);
        await UnitOfWork.SaveChangesAsync();
        var command = new DeleteCoachCommand { Id = coach.Id };

        // Act
        var result = await Mediator.Send(command);
        var deletedCoach = await UnitOfWork.Coaches.GetByIdAsync(coach.Id, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();
        deletedCoach.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ValidCoachId_DeletesCoach_Failed_AssignedTo_aTeam()
    {
        // Arrange
        var team = TestData.CreateTestDbTeam("Chelsea");
        await UnitOfWork.Teams.AddAsync(team);
        await UnitOfWork.SaveChangesAsync();
        var coach = TestData.CreateTestDbCoach(team.Id, "Mauricio", "Pochettino");
        await UnitOfWork.Coaches.AddAsync(coach);
        await UnitOfWork.SaveChangesAsync();
        var command = new DeleteCoachCommand { Id = coach.Id };

        // Act
        var result = await Mediator.Send(command);
        var deletedCoach = await UnitOfWork.Coaches.GetByIdAsync(coach.Id, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeFalse();
        deletedCoach.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_NonExistentCoachId_ReturnsError()
    {
        // Arrange
        var command = new DeleteCoachCommand { Id = -1 };

        // Act
        var result = await Mediator.Send(command);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Error.Should().NotBeNull();
    }
}
