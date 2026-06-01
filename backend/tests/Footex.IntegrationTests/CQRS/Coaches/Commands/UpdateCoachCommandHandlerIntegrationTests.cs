using Application.CQRS.Coaches.Commands;
using Domain.Interfaces;
using FluentAssertions;
using Footex.IntegrationTests.Common;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Footex.IntegrationTests.CQRS.Coaches.Commands;

public class UpdateCoachCommandHandlerIntegrationTests(FootexWebApplicationFactory factory)
    : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task Handle_ValidUpdate_UpdatesCoachSuccessfully()
    {
        // Arrange
        var team = TestData.CreateTestDbTeam("Tottenham");
        await UnitOfWork.Teams.AddAsync(team);
        await UnitOfWork.SaveChangesAsync();
        var coach = TestData.CreateTestDbCoach(team.Id, "Ange", "Pochettino");
        await UnitOfWork.Coaches.AddAsync(coach);
        await UnitOfWork.SaveChangesAsync();
        var command = new UpdateCoachCommand
        {
            Id = coach.Id,
            FirstName = "Ange Updated",
            Nationality = "Australian",
            Role = "Head Coach",
            TeamId = team.Id,
            PreferredFormation = "4-3-3",
            CoachingStyle = "Attacking",
            Biography = "Updated bio",
            YearsOfExperience = 10,
        };

        // Act
        var result = await Mediator.Send(command);
        var updatedCoach = await UnitOfWork.Coaches.GetByIdAsync(coach.Id);

        // Assert
        result.Succeeded.Should().BeTrue();
        updatedCoach.FirstName.Should().Be(command.FirstName);
        updatedCoach.Nationality.Should().Be(command.Nationality);
        updatedCoach.Role.Should().Be(command.Role);
        updatedCoach.PreferredFormation.Should().Be(command.PreferredFormation);
        updatedCoach.CoachingStyle.Should().Be(command.CoachingStyle);
        updatedCoach.Biography.Should().Be(command.Biography);
        updatedCoach.YearsOfExperience.Should().Be(command.YearsOfExperience);
    }

    [Fact]
    public async Task Handle_NonExistentCoachId_ReturnsError()
    {
        // Arrange
        var command = new UpdateCoachCommand { Id = -1, FirstName = "Ghost Coach" };

        // Act
        var result = await Mediator.Send(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.NotNull(result.Error);
    }
}
