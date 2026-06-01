using Application.CQRS.Matches.Queries;
using FluentAssertions;
using Footex.IntegrationTests.Common;
using Infrastructure;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Footex.IntegrationTests.CQRS.Matches.Queries;

public class GetMatchByIdWithDetailsQueryHandlerIntegrationTests(
    FootexWebApplicationFactory factory
) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task Handle_WithValidId_ReturnsMatchDetails()
    {
        // Arrange
        var user = TestData.CreateTestUser(true);
        var homeTeam = TestData.CreateTestDbTeam();
        var awayTeam = TestData.CreateTestDbTeam();
        var season = TestData.CreateTestDbSeason();
        await Context.Teams.AddRangeAsync(homeTeam, awayTeam);
        await Context.Seasons.AddAsync(season);
        await Context.Users.AddAsync(user);
        await Context.SaveChangesAsync();

        var homeSeasonTeam = TestData.CreateTestDbSeasonTeam(season.Id, homeTeam.Id);
        var awaySeasonTeam = TestData.CreateTestDbSeasonTeam(season.Id, awayTeam.Id);
        await Context.TeamSeasons.AddRangeAsync(homeSeasonTeam, awaySeasonTeam);
        await Context.SaveChangesAsync();

        var match = TestData.CreateTestMatch(homeTeam.Id, awayTeam.Id, season.Id, true, user.Id);
        await Context.Matches.AddAsync(match);
        await Context.SaveChangesAsync();

        var query = new GetMatchByIdWithDetailsQuery { MatchId = match.Id };

        // Act
        var response = await Mediator.Send(query);

        // Assert
        response.Should().NotBeNull();
        response.Succeeded.Should().BeTrue();
        response.Match.Should().NotBeNull();
        response.Match!.Id.Should().Be(match.Id);
    }

    [Fact]
    public async Task Handle_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var query = new GetMatchByIdWithDetailsQuery { MatchId = 999999 };

        // Act
        var response = await Mediator.Send(query);

        // Assert
        response.Should().NotBeNull();
        response.Succeeded.Should().BeFalse();
        response.NotFound.Should().BeTrue();
        response.Match.Should().BeNull();
    }
}
