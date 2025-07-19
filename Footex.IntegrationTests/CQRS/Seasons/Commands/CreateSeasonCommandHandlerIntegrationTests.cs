using Application.CQRS.Seasons.Commands;
using Domain.Interfaces;
using Domain.Models;
using FluentAssertions;
using Footex.IntegrationTests.Common;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Footex.IntegrationTests.CQRS.Seasons.Commands;

public class CreateSeasonCommandHandlerIntegrationTests(FootexWebApplicationFactory factory)
    : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task Handle_ValidSeasonCommand_CreatesSeasonSuccessfully()
    {
        // Arrange
        var competition = TestData.CreateTestCompetition();
        await UnitOfWork.Competitions.AddAsync(competition);
        await UnitOfWork.SaveChangesAsync();
        var command = new CreateSeasonCommand
        {
            Name = "2024/2025",
            LeagueName = "Premier League",
            StartDate = new DateTime(2024, 8, 17),
            EndDate = new DateTime(2025, 5, 25),
            IsActive = false,
            CompetitionId = competition.Id,
        };

        // Act
        var result = await Mediator.Send(command, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Id.Should().NotBe(0);
        result.Name.Should().Be(command.Name);
        result.Error.Should().BeNull();

        // Verify season was saved to database
        var savedSeason = await UnitOfWork.Seasons.GetByIdAsync(result.Id);
        savedSeason.Should().NotBeNull();
        savedSeason!.Name.Should().Be(command.Name);
        savedSeason.StartDate.Should().Be(command.StartDate);
        savedSeason.EndDate.Should().Be(command.EndDate);
        savedSeason.IsActive.Should().Be(command.IsActive);
    }

    [Fact]
    public async Task Handle_SeasonWithValidDateRange_CreatesSuccessfully()
    {
        var competition = TestData.CreateTestCompetition();
        await UnitOfWork.Competitions.AddAsync(competition);
        await UnitOfWork.SaveChangesAsync();
        // Arrange
        var command = new CreateSeasonCommand
        {
            Name = "2023-24 Season",
            StartDate = new DateTime(2023, 7, 1),
            EndDate = new DateTime(2024, 6, 30),
            IsActive = false,
            CompetitionId = competition.Id,
        };

        // Act
        var result = await Mediator.Send(command, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Id.Should().NotBe(0);

        var savedSeason = await UnitOfWork.Seasons.GetByIdAsync(result.Id);
        savedSeason.Should().NotBeNull();
        savedSeason!.StartDate.Should().Be(command.StartDate);
        savedSeason.EndDate.Should().Be(command.EndDate);
        savedSeason.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_MultipleCurrentSeasons_OnlyOneCanBeCurrent()
    {
        var competition = TestData.CreateTestCompetition();
        await UnitOfWork.Competitions.AddAsync(competition);
        await UnitOfWork.SaveChangesAsync();
        // Arrange
        // Create first current season
        var existingSeason = new Season
        {
            Name = "Current Season",
            StartDate = DateTime.UtcNow.AddDays(-30),
            EndDate = DateTime.UtcNow.AddDays(300),
            IsActive = false,
            LeagueName = "Premier League",
            Country = "England",
            CompetitionId = competition.Id,
        };
        await UnitOfWork.Seasons.AddAsync(existingSeason);
        await UnitOfWork.SaveChangesAsync();

        var command = new CreateSeasonCommand
        {
            Name = "Current Season",
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(365),
            IsActive = true,
            CompetitionId = competition.Id,
        };

        // Act
        var result = await Mediator.Send(command, CancellationToken.None);

        // Assert
        result.Succeeded.Should().Be(false);

        // The exact behavior depends on business requirements
        var currentSeasons = await UnitOfWork.Seasons.GetAllAsync();
        var activeCurrent = currentSeasons.Where(s => s.IsActive).ToList();

        // Should have only one current
        activeCurrent.Count.Should().BeLessThanOrEqualTo(1);
    }

    [Fact]
    public async Task Handle_SeasonWithFutureStartDate_CreatesSuccessfully()
    {
        // Arrange
        var competition = TestData.CreateTestCompetition();
        await UnitOfWork.Competitions.AddAsync(competition);
        await UnitOfWork.SaveChangesAsync();
        var command = new CreateSeasonCommand
        {
            Name = "Future Season 2026-27",
            StartDate = new DateTime(2026, 8, 1),
            EndDate = new DateTime(2027, 7, 31),
            IsActive = false,
            CompetitionId = competition.Id,
        };

        // Act
        var result = await Mediator.Send(command, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Id.Should().NotBe(-1);

        var savedSeason = await UnitOfWork.Seasons.GetByIdAsync(result.Id);
        savedSeason.Should().NotBeNull();
        savedSeason!.StartDate.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task Handle_SeasonWithPastDates_CreatesSuccessfully()
    {
        var competition = TestData.CreateTestCompetition();
        await UnitOfWork.Competitions.AddAsync(competition);
        await UnitOfWork.SaveChangesAsync();
        // Arrange
        var command = new CreateSeasonCommand
        {
            Name = "Historical Season 2020-21",
            StartDate = new DateTime(2020, 9, 12),
            EndDate = new DateTime(2021, 5, 23),
            IsActive = false,
            CompetitionId = competition.Id,
        };

        // Act
        var result = await Mediator.Send(command, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();

        var savedSeason = await UnitOfWork.Seasons.GetByIdAsync(result.Id);
        savedSeason.Should().NotBeNull();
        savedSeason!.EndDate.Should().BeBefore(DateTime.UtcNow);
        savedSeason.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_LongSeasonName_CreatesSuccessfully()
    {
        // Arrange
        var competition = TestData.CreateTestCompetition();
        await UnitOfWork.Competitions.AddAsync(competition);
        await UnitOfWork.SaveChangesAsync();
        const string longName =
            "Very Long Season Name That Exceeds Normal Length But Should Still Be Valid For Database Storage";
        var command = new CreateSeasonCommand
        {
            Name = longName,
            LeagueName = "Premier League",
            StartDate = DateTime.UtcNow,
            CompetitionId = competition.Id,
            EndDate = DateTime.UtcNow.AddYears(1),
            IsActive = false,
        };

        // Act
        var result = await Mediator.Send(command, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();

        var savedSeason = await UnitOfWork.Seasons.GetByIdAsync(result.Id);
        savedSeason.Should().NotBeNull();
        savedSeason?.Name.Should().Be(longName);
    }

    [Fact]
    public async Task Handle_MultipleSeasonsCreation_AllCreateSuccessfully()
    {
        var competition = TestData.CreateTestCompetition();
        await UnitOfWork.Competitions.AddAsync(competition);
        await UnitOfWork.SaveChangesAsync();
        // Arrange
        var commands = new[]
        {
            new CreateSeasonCommand
            {
                Name = "Season 1",
                StartDate = new DateTime(2024, 1, 1),
                EndDate = new DateTime(2024, 12, 31),
                IsActive = false,
                CompetitionId = competition.Id,
            },
            new CreateSeasonCommand
            {
                Name = "Season 2",
                StartDate = new DateTime(2025, 1, 1),
                EndDate = new DateTime(2025, 12, 31),
                IsActive = false,
                CompetitionId = competition.Id,
            },
            new CreateSeasonCommand
            {
                Name = "Season 3",
                StartDate = new DateTime(2026, 1, 1),
                EndDate = new DateTime(2026, 12, 31),
                IsActive = false,
                CompetitionId = competition.Id,
            },
        };

        var results = new List<CreateSeasonCommandResponse>();

        // Act
        foreach (var command in commands)
        {
            var result = await Mediator.Send(command, CancellationToken.None);
            results.Add(result);
        }

        // Assert
        results.Should().OnlyContain(r => r.Succeeded);
        results.Should().HaveCount(3);

        // Verify unique IDs
        var ids = results.Select(r => r.Id).ToList();
        ids.Should().OnlyHaveUniqueItems();
        ids.Should().HaveCount(3);

        // Verify all saved in database
        foreach (var result in results)
        {
            var savedSeason = await UnitOfWork.Seasons.GetByIdAsync(result.Id);
            savedSeason.Should().NotBeNull();
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
            IsActive = false,
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
