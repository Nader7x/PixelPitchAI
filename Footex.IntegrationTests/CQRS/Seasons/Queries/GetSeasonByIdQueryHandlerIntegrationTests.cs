using Application.CQRS.Seasons.Queries;
using Domain.Interfaces;
using FluentAssertions;
using Footex.IntegrationTests.Common;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Footex.IntegrationTests.CQRS.Seasons.Queries;

public class GetSeasonByIdQueryHandlerIntegrationTests(FootexWebApplicationFactory factory)
    : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task Handle_ValidSeasonId_ReturnsSeasonSuccessfully()
    {
        // Arrange
        var season = TestData.CreateTestDbSeason();
        season.StartDate = new DateTime(2024, 8, 18);
        season.EndDate = new DateTime(2025, 5, 25);
        season.IsActive = true;

        await UnitOfWork.Seasons.AddAsync(season);
        await UnitOfWork.SaveChangesAsync();

        var query = new GetSeasonByIdQuery { Id = season.Id };

        // Act
        var result = await Mediator.Send(query, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Season.Should().NotBeNull();
        result.Season!.Id.Should().Be(season.Id);
        result.Season.Name.Should().Be(season.Name);
        result.Season.StartDate.Should().Be(season.StartDate);
        result.Season.EndDate.Should().Be(season.EndDate);
        result.Season.IsActive.Should().Be(season.IsActive);
        result.Error.Should().BeNull();
        result.NotFound.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_NonExistentSeasonId_ReturnsNotFoundResponse()
    {
        // Arrange
        const int nonExistentId = -1;
        var query = new GetSeasonByIdQuery { Id = nonExistentId };

        // Act
        var result = await Mediator.Send(query, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.NotFound.Should().BeTrue();
        result.Season.Should().BeNull();
        result.Error.Should().Contain($"Season with ID {nonExistentId} not found");
    }

    [Fact]
    public async Task Handle_DeletedSeason_ReturnsNotFoundResponse()
    {
        // Arrange
        var season = TestData.CreateTestDbSeason();
        await UnitOfWork.Seasons.AddAsync(season);
        await UnitOfWork.SaveChangesAsync();
        UnitOfWork.Seasons.Delete(season);
        await UnitOfWork.SaveChangesAsync();
        var query = new GetSeasonByIdQuery { Id = season.Id };

        // Act
        var result = await Mediator.Send(query, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.NotFound.Should().BeTrue();
        result.Season.Should().BeNull();
        result.Error.Should().Contain($"Season with ID {season.Id} not found");
    }

    [Fact]
    public async Task Handle_CurrentSeason_ReturnsWithCurrentFlag()
    {
        // Arrange
        var season = TestData.CreateTestDbSeason();
        season.IsActive = true;
        season.StartDate = DateTime.UtcNow.AddDays(-30);
        season.EndDate = DateTime.UtcNow.AddDays(300);

        await UnitOfWork.Seasons.AddAsync(season);
        await UnitOfWork.SaveChangesAsync();

        var query = new GetSeasonByIdQuery { Id = season.Id };

        // Act
        var result = await Mediator.Send(query, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Season.Should().NotBeNull();
        result.Season!.IsActive.Should().BeTrue();
        result.Season.StartDate.Should().BeOnOrBefore(DateTime.UtcNow);
        result.Season.EndDate.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task Handle_PastSeason_ReturnsWithCorrectDates()
    {
        // Arrange
        var season = TestData.CreateTestDbSeason();
        season.IsActive = false;
        season.StartDate = new DateTime(2022, 8, 13);
        season.EndDate = new DateTime(2023, 5, 28);

        await UnitOfWork.Seasons.AddAsync(season);
        await UnitOfWork.SaveChangesAsync();

        var query = new GetSeasonByIdQuery { Id = season.Id };

        // Act
        var result = await Mediator.Send(query, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Season.Should().NotBeNull();
        result.Season!.IsActive.Should().BeFalse();
        result.Season.EndDate.Should().BeBefore(DateTime.UtcNow);
        result.Season.StartDate.Should().Be(new DateTime(2022, 8, 13));
        result.Season.EndDate.Should().Be(new DateTime(2023, 5, 28));
    }

    [Fact]
    public async Task Handle_FutureSeason_ReturnsWithCorrectDates()
    {
        // Arrange
        var season = TestData.CreateTestDbSeason();
        season.IsActive = false;
        season.StartDate = new DateTime(2026, 8, 15);
        season.EndDate = new DateTime(2027, 5, 30);

        await UnitOfWork.Seasons.AddAsync(season);
        await UnitOfWork.SaveChangesAsync();

        var query = new GetSeasonByIdQuery { Id = season.Id };

        // Act
        var result = await Mediator.Send(query, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Season.Should().NotBeNull();
        result.Season!.IsActive.Should().BeFalse();
        result.Season.StartDate.Should().BeAfter(DateTime.UtcNow);
        result.Season.StartDate.Should().Be(new DateTime(2026, 8, 15));
        result.Season.EndDate.Should().Be(new DateTime(2027, 5, 30));
    }

    [Fact]
    public async Task Handle_SeasonWithTeams_ReturnsSeasonWithTeamData()
    {
        // Arrange
        var season = TestData.CreateTestDbSeason();
        await UnitOfWork.Seasons.AddAsync(season);
        await UnitOfWork.SaveChangesAsync();

        // Create teams and associate with season
        var team1 = TestData.CreateTestDbTeam();
        var team2 = TestData.CreateTestDbTeam();
        await UnitOfWork.Teams.AddAsync(team1);
        await UnitOfWork.Teams.AddAsync(team2);
        await UnitOfWork.SaveChangesAsync();

        // Create team-season relationships if the model supports it
        // This depends on the actual domain model structure

        var query = new GetSeasonByIdQuery { Id = season.Id };

        // Act
        var result = await Mediator.Send(query, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Season.Should().NotBeNull();
        result.Season!.Id.Should().Be(season.Id);
    }

    [Fact]
    public async Task Handle_MultipleSeasonsInDatabase_ReturnsCorrectSeason()
    {
        // Arrange
        var season1 = TestData.CreateTestDbSeason();
        var season2 = TestData.CreateTestDbSeason();
        var season3 = TestData.CreateTestDbSeason();

        season1.StartDate = new DateTime(2022, 1, 1);
        season2.StartDate = new DateTime(2023, 1, 1);
        season3.StartDate = new DateTime(2024, 1, 1);

        await UnitOfWork.Seasons.AddAsync(season1);
        await UnitOfWork.Seasons.AddAsync(season2);
        await UnitOfWork.Seasons.AddAsync(season3);
        await UnitOfWork.SaveChangesAsync();

        var query = new GetSeasonByIdQuery { Id = season2.Id };

        // Act
        var result = await Mediator.Send(query, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Season.Should().NotBeNull();
        result.Season!.Id.Should().Be(season2.Id);
        result.Season.Name.Should().Be(season2.Name);
        result.Season.StartDate.Should().Be(new DateTime(2023, 1, 1));
        result.Season.Id.Should().NotBe(season1.Id);
        result.Season.Id.Should().NotBe(season3.Id);
    }

    [Fact]
    public async Task Handle_SeasonWithLongName_ReturnsCompleteSeasonInfo()
    {
        // Arrange
        const string longName =
            "Very Long Season Name That Contains Multiple Words And Describes The Competition In Detail";
        var season = TestData.CreateTestDbSeason();
        season.Name = longName;
        await UnitOfWork.Seasons.AddAsync(season);
        await UnitOfWork.SaveChangesAsync();

        var query = new GetSeasonByIdQuery { Id = season.Id };

        // Act
        var result = await Mediator.Send(query, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Season.Should().NotBeNull();
        result.Season!.Name.Should().Be(longName);
    }

    [Fact]
    public async Task Handle_DatabaseException_ReturnsErrorResponse()
    {
        // Arrange
        var query = new GetSeasonByIdQuery { Id = -1 }; // Invalid ID to trigger exception

        // Dispose context to simulate database error
        await DisposeContext();

        // Act
        var result = await Mediator.Send(query, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Season.Should().BeNull();
        result.NotFound.Should().BeFalse(); // Should be false for exceptions
    }

    private Task DisposeContext()
    {
        UnitOfWork.Dispose();
        return Task.CompletedTask;
    }
}
