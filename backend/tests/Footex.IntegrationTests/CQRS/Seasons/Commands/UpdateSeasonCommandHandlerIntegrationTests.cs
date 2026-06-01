using Application.CQRS.Seasons.Commands;
using FluentAssertions;
using Footex.IntegrationTests.Common;
using Infrastructure;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Footex.IntegrationTests.CQRS.Seasons.Commands;

public class UpdateSeasonCommandHandlerIntegrationTests(FootexWebApplicationFactory factory)
    : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task Handle_WithValidCommand_UpdatesSeasonInDatabase()
    {
        // Arrange
        var season = TestData.CreateTestDbSeason();
        Context.Seasons.Add(season);
        await Context.SaveChangesAsync();

        var command = new UpdateSeasonCommand
        {
            Id = season.Id,
            Name = "Updated Season Name",
            StartDate = new DateTime(2025, 8, 1),
            EndDate = new DateTime(2026, 5, 31),
            IsActive = false,
        };

        // Act
        var response = await Mediator.Send(command);

        // Assert
        response.Should().NotBeNull();
        response.Succeeded.Should().BeTrue();
        response.Name.Should().Be(command.Name);
        response.Id.Should().Be(season.Id);

        var updatedSeason = await Context.Seasons.FindAsync(season.Id);
        updatedSeason.Should().NotBeNull();
        updatedSeason!.Name.Should().Be(command.Name);
        updatedSeason.StartDate.Should().Be(command.StartDate);
        updatedSeason.EndDate.Should().Be(command.EndDate);
        updatedSeason.IsActive.Should().Be(command.IsActive);
    }

    [Fact]
    public async Task Handle_WithInvalidSeasonId_ReturnsFailureResponse()
    {
        // Arrange
        var command = new UpdateSeasonCommand
        {
            Id = 999999,
            Name = "Nonexistent Season",
            StartDate = new DateTime(2025, 8, 1),
            EndDate = new DateTime(2026, 5, 31),
            IsActive = false,
        };

        // Act
        var response = await Mediator.Send(command);

        // Assert
        response.Should().NotBeNull();
        response.Succeeded.Should().BeFalse();
    }
}
