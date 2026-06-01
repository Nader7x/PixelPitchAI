using Application.CQRS.Seasons.Commands;
using FluentAssertions;
using Footex.IntegrationTests.Common;
using Infrastructure;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Footex.IntegrationTests.CQRS.Seasons.Commands;

public class DeleteSeasonCommandHandlerIntegrationTests(FootexWebApplicationFactory factory)
    : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task Handle_WithValidId_DeletesSeason()
    {
        // Arrange
        var season = TestData.CreateTestDbSeason();
        Context.Seasons.Add(season);
        await Context.SaveChangesAsync();

        var command = new DeleteSeasonCommand { Id = season.Id };

        // Act
        var response = await Mediator.Send(command);

        // Assert
        response.Should().NotBeNull();
        response.Succeeded.Should().BeTrue();
        (await Context.Seasons.FindAsync(season.Id)).Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithInvalidId_ReturnsFailureResponse()
    {
        // Arrange
        var command = new DeleteSeasonCommand { Id = 999999 };

        // Act
        var response = await Mediator.Send(command);

        // Assert
        response.Should().NotBeNull();
        response.Succeeded.Should().BeFalse();
    }
}
