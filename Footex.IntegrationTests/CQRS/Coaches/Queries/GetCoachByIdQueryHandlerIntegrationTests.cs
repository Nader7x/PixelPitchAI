using Application.CQRS.Coaches.Queries;
using Domain.Interfaces;
using Footex.IntegrationTests.Common;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Footex.IntegrationTests.CQRS.Coaches.Queries;

public class GetCoachByIdQueryHandlerIntegrationTests : BaseIntegrationTest
{
    private readonly GetCoachByIdQueryHandler _handler;
    private readonly IUnitOfWork _unitOfWork;

    public GetCoachByIdQueryHandlerIntegrationTests(FootexWebApplicationFactory factory)
        : base(factory)
    {
        _handler = ServiceProvider.GetRequiredService<GetCoachByIdQueryHandler>();
        _unitOfWork = ServiceProvider.GetRequiredService<IUnitOfWork>();
    }

    [Fact]
    public async Task Handle_ValidCoachId_ReturnsCoachSuccessfully()
    {
        // Arrange
        var team = TestData.CreateTeam("Real Madrid");
        await _unitOfWork.Teams.AddAsync(team);
        await _unitOfWork.SaveChangesAsync();

        var coach = TestData.CreateCoach("Carlo Ancelotti", team.Id);
        coach.DateOfBirth = new DateTime(1959, 6, 10);
        coach.Nationality = "Italian";
        coach.Role = "Head Coach";
        coach.PreferredFormation = "4-3-3";
        coach.CoachingStyle = "Adaptive";
        coach.Biography = "Multiple Champions League winner";

        await _unitOfWork.Coaches.AddAsync(coach);
        await _unitOfWork.SaveChangesAsync();

        var query = new GetCoachByIdQuery { Id = coach.Id };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Coach);
        Assert.Equal(coach.Id, result.Coach.Id);
        Assert.Equal(coach.FirstName, result.Coach.FirstName);
        Assert.Equal(coach.DateOfBirth, result.Coach.DateOfBirth);
        Assert.Equal(coach.Nationality, result.Coach.Nationality);
        Assert.Equal(coach.Role, result.Coach.Role);
        Assert.Equal(coach.PreferredFormation, result.Coach.PreferredFormation);
        Assert.Equal(coach.CoachingStyle, result.Coach.CoachingStyle);
        Assert.Equal(coach.Biography, result.Coach.Biography);
        Assert.Equal(team.Id, result.Coach.TeamId);
        Assert.Null(result.Error);
        Assert.False(result.NotFound);
    }

    [Fact]
    public async Task Handle_NonExistentCoachId_ReturnsNotFoundResponse()
    {
        // Arrange
        var nonExistentId = -1;
        var query = new GetCoachByIdQuery { Id = nonExistentId };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.NotFound);
        Assert.Null(result.Coach);
        Assert.Contains($"Coach with ID {nonExistentId} not found", result.Error);
    }

    [Fact]
    public async Task Handle_DeletedCoach_ReturnsNotFoundResponse()
    {
        // Arrange
        var team = TestData.CreateTeam("Test Team");
        await _unitOfWork.Teams.AddAsync(team);
        await _unitOfWork.SaveChangesAsync();

        var coach = TestData.CreateCoach("Deleted Coach", team.Id);
        await _unitOfWork.Coaches.AddAsync(coach);
        await _unitOfWork.SaveChangesAsync();

        var query = new GetCoachByIdQuery { Id = coach.Id };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.NotFound);
        Assert.Null(result.Coach);
        Assert.Contains($"Coach with ID {coach.Id} not found", result.Error);
    }

    [Fact]
    public async Task Handle_CoachWithCompleteProfile_ReturnsAllInformation()
    {
        // Arrange
        var team = TestData.CreateTeam("Barcelona");
        await _unitOfWork.Teams.AddAsync(team);
        await _unitOfWork.SaveChangesAsync();

        var coach = TestData.CreateCoach("Xavi Hernández", team.Id);
        coach.DateOfBirth = new DateTime(1980, 1, 25);
        coach.Nationality = "Spanish";
        coach.Role = "Head Coach";
        coach.PhotoUrl = "https://example.com/xavi.jpg";
        coach.PreferredFormation = "4-3-3";
        coach.CoachingStyle = "Tiki-taka";
        coach.Biography = "Former Barcelona legend turned coach";
        coach.YearsOfExperience = 5;

        await _unitOfWork.Coaches.AddAsync(coach);
        await _unitOfWork.SaveChangesAsync();

        var query = new GetCoachByIdQuery { Id = coach.Id };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Coach);
        Assert.Equal(coach.PhotoUrl, result.Coach.PhotoUrl);
        Assert.Equal(coach.YearsOfExperience, result.Coach.YearsOfExperience);
    }

    [Fact]
    public async Task Handle_CoachWithMinimalData_ReturnsBasicInformation()
    {
        // Arrange
        var coach = TestData.CreateCoach("Simple Coach", 0);
        // Only name set, all other fields are null/default

        await _unitOfWork.Coaches.AddAsync(coach);
        await _unitOfWork.SaveChangesAsync();

        var query = new GetCoachByIdQuery { Id = coach.Id };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Coach);
        Assert.Equal(coach.FirstName, result.Coach.FirstName);
        Assert.Null(result.Coach.TeamId);
        Assert.Null(result.Coach.DateOfBirth);
        Assert.Null(result.Coach.Nationality);
        Assert.Null(result.Coach.Role);
        Assert.Null(result.Coach.PhotoUrl);
    }

    [Fact]
    public async Task Handle_CoachWithTeamInformation_ReturnsCoachWithTeamDetails()
    {
        // Arrange
        var team = TestData.CreateTeam("Chelsea");
        team.Logo = "https://example.com/chelsea-logo.png";
        await _unitOfWork.Teams.AddAsync(team);
        await _unitOfWork.SaveChangesAsync();

        var coach = TestData.CreateCoach("Frank Lampard", team.Id);
        await _unitOfWork.Coaches.AddAsync(coach);
        await _unitOfWork.SaveChangesAsync();

        var query = new GetCoachByIdQuery { Id = coach.Id };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Coach);
        Assert.Equal(team.Id, result.Coach.TeamId);
    }

    [Fact]
    public async Task Handle_YoungCoach_ReturnsWithCorrectAge()
    {
        // Arrange
        var team = TestData.CreateTeam("Young Team");
        await _unitOfWork.Teams.AddAsync(team);
        await _unitOfWork.SaveChangesAsync();

        var coach = TestData.CreateCoach("Young Coach", team.Id);
        coach.DateOfBirth = new DateTime(1995, 6, 15); // Young coach
        coach.YearsOfExperience = 2;

        await _unitOfWork.Coaches.AddAsync(coach);
        await _unitOfWork.SaveChangesAsync();

        var query = new GetCoachByIdQuery { Id = coach.Id };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Coach);
        Assert.True(result.Coach.DateOfBirth > new DateTime(1990, 1, 1));
        Assert.Equal(2, result.Coach.YearsOfExperience);
    }

    [Fact]
    public async Task Handle_ExperiencedCoach_ReturnsWithCorrectExperience()
    {
        // Arrange
        var team = TestData.CreateTeam("Veteran Team");
        await _unitOfWork.Teams.AddAsync(team);
        await _unitOfWork.SaveChangesAsync();

        var coach = TestData.CreateCoach("Veteran Coach", team.Id);
        coach.DateOfBirth = new DateTime(1955, 4, 20);
        coach.YearsOfExperience = 40;

        await _unitOfWork.Coaches.AddAsync(coach);
        await _unitOfWork.SaveChangesAsync();

        var query = new GetCoachByIdQuery { Id = coach.Id };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Coach);
        Assert.True(result.Coach.DateOfBirth < new DateTime(1960, 1, 1));
        Assert.Equal(40, result.Coach.YearsOfExperience);
    }

    [Fact]
    public async Task Handle_MultipleCoachesInDatabase_ReturnsCorrectCoach()
    {
        // Arrange
        var team1 = TestData.CreateTeam("Team 1");
        var team2 = TestData.CreateTeam("Team 2");
        await _unitOfWork.Teams.AddAsync(team1);
        await _unitOfWork.Teams.AddAsync(team2);
        await _unitOfWork.SaveChangesAsync();

        var coach1 = TestData.CreateCoach("Coach 1", team1.Id);
        var coach2 = TestData.CreateCoach("Coach 2", team2.Id);
        var coach3 = TestData.CreateCoach("Coach 3", team1.Id);

        await _unitOfWork.Coaches.AddAsync(coach1);
        await _unitOfWork.Coaches.AddAsync(coach2);
        await _unitOfWork.Coaches.AddAsync(coach3);
        await _unitOfWork.SaveChangesAsync();

        var query = new GetCoachByIdQuery { Id = coach2.Id };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Coach);
        Assert.Equal(coach2.Id, result.Coach.Id);
        Assert.Equal(coach2.FirstName, result.Coach.FirstName);
        Assert.Equal(team2.Id, result.Coach.TeamId);
        Assert.NotEqual(coach1.Id, result.Coach.Id);
        Assert.NotEqual(coach3.Id, result.Coach.Id);
    }

    [Fact]
    public async Task Handle_DatabaseException_ReturnsErrorResponse()
    {
        // Arrange
        var query = new GetCoachByIdQuery { Id = -1 };

        // Dispose context to simulate database error
        await DisposeContext();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.NotNull(result.Error);
        Assert.Null(result.Coach);
        Assert.False(result.NotFound);
    }

    private Task DisposeContext()
    {
        _unitOfWork.Dispose();
        return Task.CompletedTask;
    }
}
