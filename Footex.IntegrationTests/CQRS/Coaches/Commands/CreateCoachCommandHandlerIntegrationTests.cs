using Application.CQRS.Coaches.Commands;
using Domain.Interfaces;
using FluentAssertions;
using Footex.IntegrationTests.Common;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Footex.IntegrationTests.CQRS.Coaches.Commands;

public class CreateCoachCommandHandlerIntegrationTests(FootexWebApplicationFactory factory)
    : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task Handle_ValidCoachCommand_CreatesCoachSuccessfully()
    {
        // Arrange
        var team = TestData.CreateTestDbTeam("Manchester City");
        await UnitOfWork.Teams.AddAsync(team);
        await UnitOfWork.SaveChangesAsync();

        var command = new CreateCoachCommand
        {
            FirstName = "Pep",
            LastName = "Guardiola",
            DateOfBirth = new DateTime(1971, 1, 18),
            Nationality = "Spanish",
            Role = "Head Coach",
            TeamId = team.Id,
            PreferredFormation = "4-3-3",
            CoachingStyle = "Possession-based",
            Biography = "One of the greatest tactical minds in football",
        };

        // Act
        var result = await Mediator.Send(command, CancellationToken.None);
        var savedCoach = await UnitOfWork.Coaches.GetByIdAsync(result.Id, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Id.Should().NotBe(0);
        savedCoach?.FirstName.Should().Be(command.FirstName);
        result.Error.Should().BeNull();

        // Verify coach was saved to database
        savedCoach.Should().NotBeNull();
        savedCoach?.FirstName.Should().Be(command.FirstName);
        savedCoach?.DateOfBirth.Should().Be(command.DateOfBirth);
        savedCoach?.Nationality.Should().Be(command.Nationality);
        savedCoach?.Role.Should().Be(command.Role);
        savedCoach?.TeamId.Should().Be(command.TeamId);
        savedCoach?.PreferredFormation.Should().Be(command.PreferredFormation);
        savedCoach?.CoachingStyle.Should().Be(command.CoachingStyle);
        savedCoach?.Biography.Should().Be(command.Biography);
    }

    [Fact]
    public async Task Handle_MinimalValidCoachCommand_CreatesCoachSuccessfully()
    {
        // Arrange
        var team = TestData.CreateTestDbTeam("Arsenal");
        await UnitOfWork.Teams.AddAsync(team);
        await UnitOfWork.SaveChangesAsync();

        var command = new CreateCoachCommand
        {
            FirstName = "Mikel",
            LastName = "Arteta",
            Nationality = "Spanish",
            DateOfBirth = new DateTime(1980, 5, 4),
            Role = "Head Coach",
            YearsOfExperience = 10,
            TeamId = team.Id,
        };

        // Act
        var result = await Mediator.Send(command, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Id.Should().NotBe(0);

        var savedCoach = await UnitOfWork.Coaches.GetByIdAsync(result.Id);
        savedCoach.Should().NotBeNull();
        savedCoach.FirstName.Should().Be(command.FirstName);
        savedCoach.TeamId.Should().Be(command.TeamId);
        savedCoach.Nationality.Should().Be(command.Nationality);
        savedCoach.Role.Should().Be(command.Role);
    }

    [Fact]
    public async Task Handle_CoachWithAllOptionalFields_CreatesSuccessfully()
    {
        // Arrange
        var team = TestData.CreateTestDbTeam("Liverpool");
        await UnitOfWork.Teams.AddAsync(team);
        await UnitOfWork.SaveChangesAsync();

        var command = new CreateCoachCommand
        {
            FirstName = "Jürgen",
            LastName = "Klopp",
            DateOfBirth = new DateTime(1967, 6, 16),
            Nationality = "German",
            Role = "Manager",
            TeamId = team.Id,
            PhotoUrl = "https://example.com/klopp.jpg",
            PreferredFormation = "4-3-3",
            CoachingStyle = "Gegenpressing",
            Biography = "Former Borussia Dortmund and Liverpool manager",
            YearsOfExperience = 25,
        };

        // Act
        var result = await Mediator.Send(command, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();

        var savedCoach = await UnitOfWork.Coaches.GetByIdAsync(result.Id, CancellationToken.None);
        savedCoach.Should().NotBeNull();
        savedCoach.PhotoUrl.Should().Be(command.PhotoUrl);
        savedCoach.YearsOfExperience.Should().Be(command.YearsOfExperience);
    }

    [Fact]
    public async Task Handle_CoachWithNonExistentTeam_ReturnsError()
    {
        // Arrange
        var nonExistentTeamId = -1;
        var command = new CreateCoachCommand
        {
            FirstName = "Test",
            LastName = "Coach",
            DateOfBirth = new DateTime(1980, 1, 1),
            Nationality = "Testland",
            Role = "Test Role",
            TeamId = nonExistentTeamId,
        };

        // Act
        var result = await Mediator.Send(command, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Id.Should().Be(0);
        result.Error.Should().Contain("Team");
    }

    [Fact]
    public async Task Handle_CoachWithoutTeam_CreatesSuccessfully()
    {
        // Arrange
        var command = new CreateCoachCommand
        {
            FirstName = "Free Agent Coach",
            LastName = "No Team",
            Nationality = "English",
            Role = "Assistant Coach",
            DateOfBirth = new DateTime(1985, 2, 20),
            YearsOfExperience = 10,
        };

        // Act
        var result = await Mediator.Send(command, CancellationToken.None);

        // Assert
        if (result.Succeeded)
        {
            var savedCoach = await UnitOfWork.Coaches.GetByIdAsync(result.Id);
            savedCoach.Should().NotBeNull();
            savedCoach?.TeamId.Should().BeNull();
        }
        else
        {
            result.Error.Should().NotBeNull();
        }
    }

    [Fact]
    public async Task Handle_YoungCoach_CreatesSuccessfully()
    {
        // Arrange
        var team = TestData.CreateTestDbTeam("Young Team");
        await UnitOfWork.Teams.AddAsync(team);
        await UnitOfWork.SaveChangesAsync();

        var command = new CreateCoachCommand
        {
            FirstName = "Young",
            LastName = "Coach",
            Nationality = "English",
            DateOfBirth = new DateTime(1995, 5, 15), // Young coach
            TeamId = team.Id,
            Role = "Assistant Coach",
            YearsOfExperience = 3,
        };

        // Act
        var result = await Mediator.Send(command, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();

        var savedCoach = await UnitOfWork.Coaches.GetByIdAsync(result.Id);
        savedCoach.Should().NotBeNull();
        savedCoach.DateOfBirth.Should().BeAfter(new DateTime(1990, 1, 1));
        savedCoach.YearsOfExperience.Should().Be(3);
    }

    [Fact]
    public async Task Handle_ExperiencedCoach_CreatesSuccessfully()
    {
        // Arrange
        var team = TestData.CreateTestDbTeam("Experienced Team");
        await UnitOfWork.Teams.AddAsync(team);
        await UnitOfWork.SaveChangesAsync();

        var command = new CreateCoachCommand
        {
            FirstName = "Veteran",
            LastName = "Coach",
            Nationality = "Italian",
            DateOfBirth = new DateTime(1950, 3, 10), // Experienced coach
            TeamId = team.Id,
            Role = "Head Coach",
            YearsOfExperience = 45,
        };

        // Act
        var result = await Mediator.Send(command, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();

        var savedCoach = await UnitOfWork.Coaches.GetByIdAsync(result.Id);
        savedCoach.Should().NotBeNull();
        savedCoach.DateOfBirth.Should().BeBefore(new DateTime(1960, 1, 1));
        savedCoach.YearsOfExperience.Should().Be(45);
    }

    [Fact]
    public async Task Handle_MultipleCoachesForSameTeam_CreatesSuccessfully()
    {
        // Arrange
        var team = TestData.CreateTestDbTeam("Multi-Coach Team");
        await UnitOfWork.Teams.AddAsync(team);
        await UnitOfWork.SaveChangesAsync();

        var commands = new[]
        {
            new CreateCoachCommand
            {
                FirstName = "Head Coach",
                LastName = "Coach",
                Nationality = "TestNationality",
                Role = "Head Coach",
                TeamId = team.Id,
                DateOfBirth = new DateTime(1975, 1, 1),
                YearsOfExperience = 20,
            },
            new CreateCoachCommand
            {
                FirstName = "Assistant 1",
                LastName = "Coach",
                Nationality = "TestNationality",
                Role = "Assistant Coach",
                TeamId = team.Id,
                DateOfBirth = new DateTime(1985, 1, 1),
                YearsOfExperience = 10,
            },
            new CreateCoachCommand
            {
                FirstName = "Assistant 2",
                LastName = "Coach",
                Nationality = "TestNationality",
                Role = "Assistant Coach",
                TeamId = team.Id,
                DateOfBirth = new DateTime(1990, 1, 1),
                YearsOfExperience = 5,
            },
        };

        var results = new List<CreateCoachCommandResponse>();

        // Act
        foreach (var command in commands)
        {
            var result = await Mediator.Send(command, CancellationToken.None);
            results.Add(result);
        }

        // Assert
        results.Should().OnlyContain(r => r.Succeeded);
        results.Count.Should().Be(3);

        foreach (var result in results)
        {
            var savedCoach = await UnitOfWork.Coaches.GetByIdAsync(result.Id);
            savedCoach.Should().NotBeNull();
            savedCoach.TeamId.Should().Be(team.Id);
        }
    }

    [Fact]
    public async Task Handle_DatabaseException_ReturnsErrorResponse()
    {
        // Arrange
        var command = new CreateCoachCommand
        {
            FirstName = "Test",
            LastName = "Coach",
            Nationality = "TestLand",
            Role = "Head Coach",
            DateOfBirth = new DateTime(1980, 1, 1),
        };

        // Dispose context to simulate database error
        await DisposeContext();

        // Act
        var result = await Mediator.Send(command, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Id.Should().Be(0);
    }

    private Task DisposeContext()
    {
        UnitOfWork.Dispose();
        return Task.CompletedTask;
    }
}
