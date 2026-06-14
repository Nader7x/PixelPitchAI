using Application.CQRS.Coaches.Queries;
using Domain.Interfaces;
using FluentAssertions;
using Footex.IntegrationTests.Common;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Footex.IntegrationTests.CQRS.Coaches.Queries;

public class GetCoachByIdQueryHandlerIntegrationTests
    : BaseIntegrationTest
{
    public GetCoachByIdQueryHandlerIntegrationTests(FootexWebApplicationFactory factory) : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await FreeDbAsync(Context.Matches, Context.Coaches, Context.Players, Context.Teams);
    }

    [Fact]
    public async Task Handle_ValidCoachId_ReturnsCoachSuccessfully()
    {
        // Arrange
        var team = TestData.CreateTestDbTeam(name: "Real Madrid");
        await UnitOfWork.Teams.AddAsync(team);
        await UnitOfWork.SaveChangesAsync();
        var coach = TestData.CreateTestDbCoach(team.Id, "Carlo", "Ancelotti");
        coach.DateOfBirth = new DateTime(1959, 6, 10);
        coach.Nationality = "Italian";
        coach.Role = "Head Coach";
        coach.PreferredFormation = "4-3-3";
        coach.CoachingStyle = "Adaptive";
        coach.Biography = "Multiple Champions League winner";
        await UnitOfWork.Coaches.AddAsync(coach);
        await UnitOfWork.SaveChangesAsync();

        var query = new GetCoachByIdQuery { Id = coach.Id };

        // Act
        var result = await Mediator.Send(query, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Coach.Should().NotBeNull();
        result.Coach?.Id.Should().Be(coach.Id);
        result.Coach?.FirstName.Should().Be(coach.FirstName);
        result.Coach?.DateOfBirth.Should().Be(coach.DateOfBirth);
        result.Coach?.Nationality.Should().Be(coach.Nationality);
        result.Coach?.Role.Should().Be(coach.Role);
        result.Coach?.PreferredFormation.Should().Be(coach.PreferredFormation);
        result.Coach?.CoachingStyle.Should().Be(coach.CoachingStyle);
        result.Coach?.Biography.Should().Be(coach.Biography);
        result.Coach?.TeamId.Should().Be(team.Id);
        result.Error.Should().BeNull();
        result.NotFound.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_NonExistentCoachId_ReturnsNotFoundResponse()
    {
        // Arrange
        const int nonExistentId = -1;
        var query = new GetCoachByIdQuery { Id = nonExistentId };

        // Act
        var result = await Mediator.Send(query, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.NotFound.Should().BeTrue();
        result.Coach.Should().BeNull();
        result.Error.Should().Contain($"Coach not found");
    }

    [Fact]
    public async Task Handle_DeletedCoach_ReturnsNotFoundResponse()
    {
        // Arrange
        var coach = TestData.CreateTestDbCoach();
        await UnitOfWork.Coaches.AddAsync(coach);
        await UnitOfWork.SaveChangesAsync(CancellationToken.None);

        UnitOfWork.Coaches.Delete(coach);
        await UnitOfWork.SaveChangesAsync(CancellationToken.None);

        var query = new GetCoachByIdQuery { Id = coach.Id };

        // Act
        var result = await Mediator.Send(query, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.NotFound.Should().BeTrue();
        result.Coach.Should().BeNull();
        result.Error.Should().Contain("Coach not found");
    }

    [Fact]
    public async Task Handle_CoachWithCompleteProfile_ReturnsAllInformation()
    {
        // Arrange
        var team = TestData.CreateTestDbTeam();
        await UnitOfWork.Teams.AddAsync(team);
        await UnitOfWork.SaveChangesAsync();

        var coach = TestData.CreateTestDbCoach(team.Id, "Xavi", "Hernández");
        coach.DateOfBirth = new DateTime(1980, 1, 25);
        coach.Nationality = "Spanish";
        coach.Role = "Head Coach";
        coach.PhotoUrl = "https://example.com/xavi.jpg";
        coach.PreferredFormation = "4-3-3";
        coach.CoachingStyle = "Tiki-taka";
        coach.Biography = "Former Barcelona legend turned coach";
        coach.YearsOfExperience = 5;

        await UnitOfWork.Coaches.AddAsync(coach);
        await UnitOfWork.SaveChangesAsync();

        var query = new GetCoachByIdQuery { Id = coach.Id };

        // Act
        var result = await Mediator.Send(query, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Coach.Should().NotBeNull();
        result.Coach.PhotoUrl.Should().Be(coach.PhotoUrl);
        result.Coach.YearsOfExperience.Should().Be(coach.YearsOfExperience);
    }

    [Fact]
    public async Task Handle_CoachWithMinimalData_ReturnsBasicInformation()
    {
        // Arrange
        var coach = TestData.CreateTestDbCoach();
        // Only name set, all other fields are null/default

        await UnitOfWork.Coaches.AddAsync(coach);
        await UnitOfWork.SaveChangesAsync();

        var query = new GetCoachByIdQuery { Id = coach.Id };

        // Act
        var result = await Mediator.Send(query, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Coach.Should().NotBeNull();
        result.Coach.FirstName.Should().Be(coach.FirstName);
        result.Coach.TeamId.Should().BeNull();
        result.Coach.DateOfBirth.Should().Be(coach.DateOfBirth);
        result.Coach.Nationality.Should().Be(coach.Nationality);
        result.Coach.Role.Should().Be(coach.Role);
        result.Coach.PhotoUrl.Should().BeNull();
    }

    [Fact]
    public async Task Handle_CoachWithTeamInformation_ReturnsCoachWithTeamDetails()
    {
        // Arrange
        var team = TestData.CreateTestDbTeam();
        team.Logo = "https://example.com/chelsea-logo.png";
        await UnitOfWork.Teams.AddAsync(team);
        await UnitOfWork.SaveChangesAsync();

        var coach = TestData.CreateTestDbCoach(team.Id, "Frank Lampard");
        await UnitOfWork.Coaches.AddAsync(coach);
        await UnitOfWork.SaveChangesAsync();

        var query = new GetCoachByIdQuery { Id = coach.Id };

        // Act
        var result = await Mediator.Send(query, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Coach.Should().NotBeNull();
        result.Coach.TeamId.Should().Be(team.Id);
    }

    [Fact]
    public async Task Handle_YoungCoach_ReturnsWithCorrectAge()
    {
        // Arrange
        var team = TestData.CreateTestDbTeam("Young Team");
        await UnitOfWork.Teams.AddAsync(team);
        await UnitOfWork.SaveChangesAsync();

        var coach = TestData.CreateTestDbCoach(team.Id, "Young Coach");
        coach.DateOfBirth = new DateTime(1995, 6, 15); // Young coach
        coach.YearsOfExperience = 2;

        await UnitOfWork.Coaches.AddAsync(coach);
        await UnitOfWork.SaveChangesAsync();

        var query = new GetCoachByIdQuery { Id = coach.Id };

        // Act
        var result = await Mediator.Send(query, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Coach.Should().NotBeNull();
        result.Coach.DateOfBirth.Should().BeAfter(new DateTime(1990, 1, 1));
        result.Coach.YearsOfExperience.Should().Be(2);
    }

    [Fact]
    public async Task Handle_ExperiencedCoach_ReturnsWithCorrectExperience()
    {
        // Arrange
        var team = TestData.CreateTestDbTeam("Veteran Team");
        await UnitOfWork.Teams.AddAsync(team);
        await UnitOfWork.SaveChangesAsync();

        var coach = TestData.CreateTestDbCoach(team.Id, "Veteran Coach");
        coach.DateOfBirth = new DateTime(1955, 4, 20);
        coach.YearsOfExperience = 40;

        await UnitOfWork.Coaches.AddAsync(coach);
        await UnitOfWork.SaveChangesAsync();

        var query = new GetCoachByIdQuery { Id = coach.Id };

        // Act
        var result = await Mediator.Send(query, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Coach.Should().NotBeNull();
        result.Coach.DateOfBirth.Should().BeBefore(new DateTime(1960, 1, 1));
        result.Coach.YearsOfExperience.Should().Be(40);
    }

    [Fact]
    public async Task Handle_MultipleCoachesInDatabase_ReturnsCorrectCoach()
    {
        // Arrange
        var team1 = TestData.CreateTestDbTeam("Team 1");
        var team2 = TestData.CreateTestDbTeam("Team 2");
        await UnitOfWork.Teams.AddAsync(team1);
        await UnitOfWork.Teams.AddAsync(team2);
        await UnitOfWork.SaveChangesAsync();

        var coach1 = TestData.CreateTestDbCoach(team1.Id, "Coach 1");
        var coach2 = TestData.CreateTestDbCoach(team2.Id, "Coach 2");
        var coach3 = TestData.CreateTestDbCoach(team1.Id, "Coach 3");

        await UnitOfWork.Coaches.AddAsync(coach1);
        await UnitOfWork.Coaches.AddAsync(coach2);
        await UnitOfWork.Coaches.AddAsync(coach3);
        await UnitOfWork.SaveChangesAsync();

        var query = new GetCoachByIdQuery { Id = coach2.Id };

        // Act
        var result = await Mediator.Send(query, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Coach.Should().NotBeNull();
        result.Coach.Id.Should().Be(coach2.Id);
        result.Coach.FirstName.Should().Be(coach2.FirstName);
        result.Coach.TeamId.Should().Be(team2.Id);
        result.Coach.Id.Should().NotBe(coach1.Id);
        result.Coach.Id.Should().NotBe(coach3.Id);
    }

    [Fact]
    public async Task Handle_DatabaseException_ReturnsErrorResponse()
    {
        // Arrange
        var query = new GetCoachByIdQuery { Id = -1 };

        // Dispose context to simulate database error
        await DisposeContext();

        // Act
        var result = await Mediator.Send(query, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Coach.Should().BeNull();
        result.NotFound.Should().BeFalse();
    }

    private Task DisposeContext()
    {
        UnitOfWork.Dispose();
        return Task.CompletedTask;
    }
}
