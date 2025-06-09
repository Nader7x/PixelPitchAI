using Application.CQRS.Seasons.Commands;
using Domain.Interfaces;
using Domain.Models;
using Footex.IntegrationTests.Common;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Footex.IntegrationTests.CQRS.Seasons.Commands;

public class CreateSeasonCommandHandlerIntegrationTests : BaseIntegrationTest
{
    private readonly CreateSeasonCommandHandler _handler;
    private readonly IUnitOfWork _unitOfWork;

    public CreateSeasonCommandHandlerIntegrationTests(FootexWebApplicationFactory factory) : base(factory)
    {
        _handler = ServiceProvider.GetRequiredService<CreateSeasonCommandHandler>();
        _unitOfWork = ServiceProvider.GetRequiredService<IUnitOfWork>();
    }

    [Fact]
    public async Task Handle_ValidSeasonCommand_CreatesSeasonSuccessfully()
    {
        // Arrange
        var command = new CreateSeasonCommand
        {
            Name = "2024-25 Premier League",
            StartDate = new DateTime(2024, 8, 17),
            EndDate = new DateTime(2025, 5, 25),
            IsActive = true
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotEqual(-1, result.Id);
        Assert.Equal(command.Name, result.Name);
        Assert.Null(result.Error);

        // Verify season was saved to database
        var savedSeason = await _unitOfWork.Seasons.GetByIdAsync(result.Id);
        Assert.NotNull(savedSeason);
        Assert.Equal(command.Name, savedSeason.Name);
        Assert.Equal(command.StartDate, savedSeason.StartDate);
        Assert.Equal(command.EndDate, savedSeason.EndDate);
        Assert.Equal(command.IsActive, savedSeason.IsActive);
    }

    [Fact]
    public async Task Handle_SeasonWithValidDateRange_CreatesSuccessfully()
    {
        // Arrange
        var command = new CreateSeasonCommand
        {
            Name = "2023-24 Season",
            StartDate = new DateTime(2023, 7, 1),
            EndDate = new DateTime(2024, 6, 30),
            IsActive = false
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotEqual(-1, result.Id);

        var savedSeason = await _unitOfWork.Seasons.GetByIdAsync(result.Id);
        Assert.NotNull(savedSeason);
        Assert.Equal(command.StartDate, savedSeason.StartDate);
        Assert.Equal(command.EndDate, savedSeason.EndDate);
        Assert.False(savedSeason.IsActive);
    }

    [Fact]
    public async Task Handle_MultipleCurrentSeasons_OnlyOneCanBeCurrent()
    {
        // Arrange
        // Create first current season
        var existingSeason = new Season
        {
            Id = 0,
            Name = "Existing Current Season",
            StartDate = DateTime.UtcNow.AddDays(-30),
            EndDate = DateTime.UtcNow.AddDays(300),
            IsActive = true,
            LeagueName = "Premier League",
            Country = "England",
        };
        await _unitOfWork.Seasons.AddAsync(existingSeason);
        await _unitOfWork.SaveChangesAsync();

        var command = new CreateSeasonCommand
        {
            Name = "New Current Season",
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(365),
            IsActive = true
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);

        // Verify the business logic for handling multiple current seasons
        var newSeason = await _unitOfWork.Seasons.GetByIdAsync(result.Id);
        var existingSeasonUpdated = await _unitOfWork.Seasons.GetByIdAsync(existingSeason.Id);
        
        // Either the new season is current and existing is not, or vice versa
        // The exact behavior depends on business requirements
        var currentSeasons = await _unitOfWork.Seasons.GetAllAsync();
        var activeCurrent = currentSeasons.Where(s => s.IsActive).ToList();
        
        // Should have only one current season
        Assert.True(activeCurrent.Count <= 1, "Only one season should be current at a time");
    }

    [Fact]
    public async Task Handle_SeasonWithFutureStartDate_CreatesSuccessfully()
    {
        // Arrange
        var command = new CreateSeasonCommand
        {
            Name = "Future Season 2026-27",
            StartDate = new DateTime(2026, 8, 1),
            EndDate = new DateTime(2027, 7, 31),
            IsActive = false
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotEqual(-1, result.Id);

        var savedSeason = await _unitOfWork.Seasons.GetByIdAsync(result.Id);
        Assert.NotNull(savedSeason);
        Assert.True(savedSeason.StartDate > DateTime.UtcNow);
    }

    [Fact]
    public async Task Handle_SeasonWithPastDates_CreatesSuccessfully()
    {
        // Arrange
        var command = new CreateSeasonCommand
        {
            Name = "Historical Season 2020-21",
            StartDate = new DateTime(2020, 9, 12),
            EndDate = new DateTime(2021, 5, 23),
            IsActive = false
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);

        var savedSeason = await _unitOfWork.Seasons.GetByIdAsync(result.Id);
        Assert.NotNull(savedSeason);
        Assert.True(savedSeason.EndDate < DateTime.UtcNow);
        Assert.False(savedSeason.IsActive);
    }

    [Fact]
    public async Task Handle_LongSeasonName_CreatesSuccessfully()
    {
        // Arrange
        var longName = "Very Long Season Name That Exceeds Normal Length But Should Still Be Valid For Database Storage";
        var command = new CreateSeasonCommand
        {
            Name = longName,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddYears(1),
            IsActive = false
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);

        var savedSeason = await _unitOfWork.Seasons.GetByIdAsync(result.Id);
        Assert.NotNull(savedSeason);
        Assert.Equal(longName, savedSeason.Name);
    }

    [Fact]
    public async Task Handle_MultipleSeasonsCreation_AllCreateSuccessfully()
    {
        // Arrange
        var commands = new[]
        {
            new CreateSeasonCommand { Name = "Season 1", StartDate = new DateTime(2024, 1, 1), EndDate = new DateTime(2024, 12, 31), IsActive = false },
            new CreateSeasonCommand { Name = "Season 2", StartDate = new DateTime(2025, 1, 1), EndDate = new DateTime(2025, 12, 31), IsActive = false },
            new CreateSeasonCommand { Name = "Season 3", StartDate = new DateTime(2026, 1, 1), EndDate = new DateTime(2026, 12, 31), IsActive = true }
        };

        var results = new List<CreateSeasonCommandResponse>();

        // Act
        foreach (var command in commands)
        {
            var result = await _handler.Handle(command, CancellationToken.None);
            results.Add(result);
        }

        // Assert
        Assert.All(results, r => Assert.True(r.Succeeded));
        Assert.Equal(3, results.Count);

        // Verify unique IDs
        var ids = results.Select(r => r.Id).ToList();
        Assert.Equal(3, ids.Distinct().Count());

        // Verify all saved in database
        foreach (var result in results)
        {
            var savedSeason = await _unitOfWork.Seasons.GetByIdAsync(result.Id);
            Assert.NotNull(savedSeason);
        }
    }

    [Fact]
    public async Task Handle_DatabaseException_ReturnsErrorResponse()
    {
        // Arrange
        var command = new CreateSeasonCommand
        {
            Name = "Test Season",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddMonths(10),
            IsActive = false
        };

        // Dispose context to simulate database error
        await DisposeContext();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.NotNull(result.Error);
        Assert.Equal(-1, result.Id);
    }

    private Task DisposeContext()
    {
        _unitOfWork.Dispose();
        return Task.CompletedTask;
    }
}
