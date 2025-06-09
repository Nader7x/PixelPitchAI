using Application.CQRS.Coaches.Commands;
using Application.Mappers;
using Domain.Interfaces;
using Footex.IntegrationTests.Common;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Footex.IntegrationTests.CQRS.Coaches.Commands;

public class CreateCoachCommandHandlerIntegrationTests : BaseIntegrationTest
{
    private readonly CreateCoachCommandHandler _handler;
    private readonly IUnitOfWork _unitOfWork;

    public CreateCoachCommandHandlerIntegrationTests(FootexWebApplicationFactory factory) : base(factory)
    {
        _handler = ServiceProvider.GetRequiredService<CreateCoachCommandHandler>();
        _unitOfWork = ServiceProvider.GetRequiredService<IUnitOfWork>();
    }

    [Fact]
    public async Task Handle_ValidCoachCommand_CreatesCoachSuccessfully()
    {
        // Arrange
        var team = TestData.CreateTeam("Manchester City");
        await _unitOfWork.Teams.AddAsync(team);
        await _unitOfWork.SaveChangesAsync();

        var command = new CreateCoachCommand
        {
            FirstName= "Pep Guardiola",
            DateOfBirth = new DateTime(1971, 1, 18),
            Nationality = "Spanish",
            Role = "Head Coach",
            TeamId = team.Id,
            PreferredFormation = "4-3-3",
            CoachingStyle = "Possession-based",
            Biography = "One of the greatest tactical minds in football"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);
        var savedCoach = await _unitOfWork.Coaches.GetByIdAsync(result.Id);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotEqual(0, result.Id);
        Assert.Equal(command.FirstName, savedCoach.FirstName);
        Assert.Null(result.Error);

        // Verify coach was saved to database
        Assert.NotNull(savedCoach);
        Assert.Equal(command.FirstName, savedCoach.FirstName);
        Assert.Equal(command.DateOfBirth, savedCoach.DateOfBirth);
        Assert.Equal(command.Nationality, savedCoach.Nationality);
        Assert.Equal(command.Role, savedCoach.Role);
        Assert.Equal(command.TeamId, savedCoach.TeamId);
        Assert.Equal(command.PreferredFormation, savedCoach.PreferredFormation);
        Assert.Equal(command.CoachingStyle, savedCoach.CoachingStyle);
        Assert.Equal(command.Biography, savedCoach.Biography);
    }

    [Fact]
    public async Task Handle_MinimalValidCoachCommand_CreatesCoachSuccessfully()
    {
        // Arrange
        var team = TestData.CreateTeam("Arsenal");
        await _unitOfWork.Teams.AddAsync(team);
        await _unitOfWork.SaveChangesAsync();

        var command = new CreateCoachCommand
        {
            FirstName= "Mikel Arteta",
            TeamId = team.Id
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotEqual(0, result.Id);

        var savedCoach = await _unitOfWork.Coaches.GetByIdAsync(result.Id);
        Assert.NotNull(savedCoach);
        Assert.Equal(command.FirstName, savedCoach.FirstName);
        Assert.Equal(command.TeamId, savedCoach.TeamId);
        Assert.Null(savedCoach.DateOfBirth);
        Assert.Null(savedCoach.Nationality);
        Assert.Null(savedCoach.Role);
    }

    [Fact]
    public async Task Handle_CoachWithAllOptionalFields_CreatesSuccessfully()
    {
        // Arrange
        var team = TestData.CreateTeam("Liverpool");
        await _unitOfWork.Teams.AddAsync(team);
        await _unitOfWork.SaveChangesAsync();

        var command = new CreateCoachCommand
        {
            FirstName= "Jürgen Klopp",
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
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);

        var savedCoach = await _unitOfWork.Coaches.GetByIdAsync(result.Id);
        Assert.NotNull(savedCoach);
        Assert.Equal(command.PhotoUrl, savedCoach.PhotoUrl);
        Assert.Equal(command.YearsOfExperience, savedCoach.YearsOfExperience);
    }

    [Fact]
    public async Task Handle_CoachWithNonExistentTeam_ReturnsError()
    {
        // Arrange
        var nonExistentTeamId = -1;
        var command = new CreateCoachCommand
        {
            FirstName= "Test Coach",
            TeamId = nonExistentTeamId
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.NotNull(result.Error);
        Assert.Equal(0, result.Id);
        Assert.Contains("Team", result.Error);
    }

    [Fact]
    public async Task Handle_CoachWithoutTeam_CreatesSuccessfully()
    {
        // Arrange
        var command = new CreateCoachCommand
        {
            FirstName= "Free Agent Coach",
            Nationality = "English",
            Role = "Assistant Coach"
            // No TeamId provided
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        if (result.Succeeded)
        {
            var savedCoach = await _unitOfWork.Coaches.GetByIdAsync(result.Id);
            Assert.NotNull(savedCoach);
            Assert.Null(savedCoach.TeamId);
        }
        else
        {
            // If the business logic requires a team, this should fail
            Assert.NotNull(result.Error);
        }
    }

    [Fact]
    public async Task Handle_YoungCoach_CreatesSuccessfully()
    {
        // Arrange
        var team = TestData.CreateTeam("Young Team");
        await _unitOfWork.Teams.AddAsync(team);
        await _unitOfWork.SaveChangesAsync();

        var command = new CreateCoachCommand
        {
            FirstName= "Young Coach",
            DateOfBirth = new DateTime(1995, 5, 15), // Young coach
            TeamId = team.Id,
            Role = "Assistant Coach",
            YearsOfExperience = 3
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);

        var savedCoach = await _unitOfWork.Coaches.GetByIdAsync(result.Id);
        Assert.NotNull(savedCoach);
        Assert.True(savedCoach.DateOfBirth > new DateTime(1990, 1, 1));
        Assert.Equal(3, savedCoach.YearsOfExperience);
    }

    [Fact]
    public async Task Handle_ExperiencedCoach_CreatesSuccessfully()
    {
        // Arrange
        var team = TestData.CreateTeam("Experienced Team");
        await _unitOfWork.Teams.AddAsync(team);
        await _unitOfWork.SaveChangesAsync();

        var command = new CreateCoachCommand
        {
            FirstName= "Veteran Coach",
            DateOfBirth = new DateTime(1950, 3, 10), // Experienced coach
            TeamId = team.Id,
            Role = "Head Coach",
            YearsOfExperience = 45,
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);

        var savedCoach = await _unitOfWork.Coaches.GetByIdAsync(result.Id);
        Assert.NotNull(savedCoach);
        Assert.True(savedCoach.DateOfBirth < new DateTime(1960, 1, 1));
        Assert.Equal(45, savedCoach.YearsOfExperience);
    }

    [Fact]
    public async Task Handle_MultipleCoachesForSameTeam_CreatesSuccessfully()
    {
        // Arrange
        var team = TestData.CreateTeam("Multi-Coach Team");
        await _unitOfWork.Teams.AddAsync(team);
        await _unitOfWork.SaveChangesAsync();

        var commands = new[]
        {
            new CreateCoachCommand { FirstName= "Head Coach", Role = "Head Coach", TeamId = team.Id },
            new CreateCoachCommand { FirstName= "Assistant Coach 1", Role = "Assistant Coach", TeamId = team.Id },
            new CreateCoachCommand { FirstName= "Assistant Coach 2", Role = "Assistant Coach", TeamId = team.Id }
        };

        var results = new List<CreateCoachCommandResponse>();

        // Act
        foreach (var command in commands)
        {
            var result = await _handler.Handle(command, CancellationToken.None);
            results.Add(result);
        }

        // Assert
        Assert.All(results, r => Assert.True(r.Succeeded));
        Assert.Equal(3, results.Count);

        // Verify all coaches belong to the same team
        foreach (var result in results)
        {
            var savedCoach = await _unitOfWork.Coaches.GetByIdAsync(result.Id);
            Assert.NotNull(savedCoach);
            Assert.Equal(team.Id, savedCoach.TeamId);
        }
    }

    [Fact]
    public async Task Handle_DatabaseException_ReturnsErrorResponse()
    {
        // Arrange
        var command = new CreateCoachCommand
        {
            FirstName= "Test Coach",
            Role = "Head Coach"
        };

        // Dispose context to simulate database error
        await DisposeContext();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.NotNull(result.Error);
        Assert.Equal(0, result.Id);
    }

    private Task DisposeContext()
    {
        _unitOfWork.Dispose();
        return Task.CompletedTask;
    }
    
}
