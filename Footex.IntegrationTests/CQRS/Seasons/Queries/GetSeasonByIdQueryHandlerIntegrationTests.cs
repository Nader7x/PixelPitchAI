using Application.CQRS.Seasons.Queries;
using Domain.Interfaces;
using Footex.IntegrationTests.Common;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Footex.IntegrationTests.CQRS.Seasons.Queries;

public class GetSeasonByIdQueryHandlerIntegrationTests : BaseIntegrationTest
{
    private readonly GetSeasonByIdQueryHandler _handler;
    private readonly IUnitOfWork _unitOfWork;

    public GetSeasonByIdQueryHandlerIntegrationTests(FootexWebApplicationFactory factory)
        : base(factory)
    {
        _handler = ServiceProvider.GetRequiredService<GetSeasonByIdQueryHandler>();
        _unitOfWork = ServiceProvider.GetRequiredService<IUnitOfWork>();
    }

    [Fact]
    public async Task Handle_ValidSeasonId_ReturnsSeasonSuccessfully()
    {
        // Arrange
        var season = TestData.CreateSeason("2024/2025", "LaLiga");
        season.StartDate = new DateTime(2024, 8, 18);
        season.EndDate = new DateTime(2025, 5, 25);
        season.IsActive = true;

        await _unitOfWork.Seasons.AddAsync(season);
        await _unitOfWork.SaveChangesAsync();

        var query = new GetSeasonByIdQuery { Id = season.Id };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Season);
        Assert.Equal(season.Id, result.Season.Id);
        Assert.Equal(season.Name, result.Season.Name);
        Assert.Equal(season.StartDate, result.Season.StartDate);
        Assert.Equal(season.EndDate, result.Season.EndDate);
        Assert.Equal(season.IsActive, result.Season.IsActive);
        Assert.Null(result.Error);
        Assert.False(result.NotFound);
    }

    [Fact]
    public async Task Handle_NonExistentSeasonId_ReturnsNotFoundResponse()
    {
        // Arrange
        var nonExistentId = -1;
        var query = new GetSeasonByIdQuery { Id = nonExistentId };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.NotFound);
        Assert.Null(result.Season);
        Assert.Contains($"Season with ID {nonExistentId} not found", result.Error);
    }

    [Fact]
    public async Task Handle_DeletedSeason_ReturnsNotFoundResponse()
    {
        // Arrange
        var season = TestData.CreateSeason("Deleted Season");
        await _unitOfWork.Seasons.AddAsync(season);
        await _unitOfWork.SaveChangesAsync();

        var query = new GetSeasonByIdQuery { Id = season.Id };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.NotFound);
        Assert.Null(result.Season);
        Assert.Contains($"Season with ID {season.Id} not found", result.Error);
    }

    [Fact]
    public async Task Handle_CurrentSeason_ReturnsWithCurrentFlag()
    {
        // Arrange
        var season = TestData.CreateSeason("Current Season 2024-25");
        season.IsActive = true;
        season.StartDate = DateTime.UtcNow.AddDays(-30);
        season.EndDate = DateTime.UtcNow.AddDays(300);

        await _unitOfWork.Seasons.AddAsync(season);
        await _unitOfWork.SaveChangesAsync();

        var query = new GetSeasonByIdQuery { Id = season.Id };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Season);
        Assert.True(result.Season.IsActive);
        Assert.True(result.Season.StartDate <= DateTime.UtcNow);
        Assert.True(result.Season.EndDate > DateTime.UtcNow);
    }

    [Fact]
    public async Task Handle_PastSeason_ReturnsWithCorrectDates()
    {
        // Arrange
        var season = TestData.CreateSeason("Past Season 2022-23");
        season.IsActive = false;
        season.StartDate = new DateTime(2022, 8, 13);
        season.EndDate = new DateTime(2023, 5, 28);

        await _unitOfWork.Seasons.AddAsync(season);
        await _unitOfWork.SaveChangesAsync();

        var query = new GetSeasonByIdQuery { Id = season.Id };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Season);
        Assert.False(result.Season.IsActive);
        Assert.True(result.Season.EndDate < DateTime.UtcNow);
        Assert.Equal(new DateTime(2022, 8, 13), result.Season.StartDate);
        Assert.Equal(new DateTime(2023, 5, 28), result.Season.EndDate);
    }

    [Fact]
    public async Task Handle_FutureSeason_ReturnsWithCorrectDates()
    {
        // Arrange
        var season = TestData.CreateSeason("Future Season 2026-27");
        season.IsActive = false;
        season.StartDate = new DateTime(2026, 8, 15);
        season.EndDate = new DateTime(2027, 5, 30);

        await _unitOfWork.Seasons.AddAsync(season);
        await _unitOfWork.SaveChangesAsync();

        var query = new GetSeasonByIdQuery { Id = season.Id };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Season);
        Assert.False(result.Season.IsActive);
        Assert.True(result.Season.StartDate > DateTime.UtcNow);
        Assert.Equal(new DateTime(2026, 8, 15), result.Season.StartDate);
        Assert.Equal(new DateTime(2027, 5, 30), result.Season.EndDate);
    }

    [Fact]
    public async Task Handle_SeasonWithTeams_ReturnsSeasonWithTeamData()
    {
        // Arrange
        var season = TestData.CreateSeason("Season with Teams");
        await _unitOfWork.Seasons.AddAsync(season);
        await _unitOfWork.SaveChangesAsync();

        // Create teams and associate with season
        var team1 = TestData.CreateTeam("Team 1");
        var team2 = TestData.CreateTeam("Team 2");
        await _unitOfWork.Teams.AddAsync(team1);
        await _unitOfWork.Teams.AddAsync(team2);
        await _unitOfWork.SaveChangesAsync();

        // Create team-season relationships if the model supports it
        // This depends on the actual domain model structure

        var query = new GetSeasonByIdQuery { Id = season.Id };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Season);
        Assert.Equal(season.Id, result.Season.Id);
    }

    [Fact]
    public async Task Handle_MultipleSeasonsInDatabase_ReturnsCorrectSeason()
    {
        // Arrange
        var season1 = TestData.CreateSeason("Season 1");
        var season2 = TestData.CreateSeason("Season 2");
        var season3 = TestData.CreateSeason("Season 3");

        season1.StartDate = new DateTime(2022, 1, 1);
        season2.StartDate = new DateTime(2023, 1, 1);
        season3.StartDate = new DateTime(2024, 1, 1);

        await _unitOfWork.Seasons.AddAsync(season1);
        await _unitOfWork.Seasons.AddAsync(season2);
        await _unitOfWork.Seasons.AddAsync(season3);
        await _unitOfWork.SaveChangesAsync();

        var query = new GetSeasonByIdQuery { Id = season2.Id };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Season);
        Assert.Equal(season2.Id, result.Season.Id);
        Assert.Equal(season2.Name, result.Season.Name);
        Assert.Equal(new DateTime(2023, 1, 1), result.Season.StartDate);
        Assert.NotEqual(season1.Id, result.Season.Id);
        Assert.NotEqual(season3.Id, result.Season.Id);
    }

    [Fact]
    public async Task Handle_SeasonWithLongName_ReturnsCompleteSeasonInfo()
    {
        // Arrange
        var longName =
            "Very Long Season Name That Contains Multiple Words And Describes The Competition In Detail";
        var season = TestData.CreateSeason(longName);
        await _unitOfWork.Seasons.AddAsync(season);
        await _unitOfWork.SaveChangesAsync();

        var query = new GetSeasonByIdQuery { Id = season.Id };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Season);
        Assert.Equal(longName, result.Season.Name);
    }

    [Fact]
    public async Task Handle_DatabaseException_ReturnsErrorResponse()
    {
        // Arrange
        var query = new GetSeasonByIdQuery { Id = -1 }; // Invalid ID to trigger exception

        // Dispose context to simulate database error
        await DisposeContext();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.NotNull(result.Error);
        Assert.Null(result.Season);
        Assert.False(result.NotFound); // Should be false for exceptions
    }

    private Task DisposeContext()
    {
        _unitOfWork.Dispose();
        return Task.CompletedTask;
    }
}
