using Application.CQRS.Matches.Queries;
using FluentAssertions;
using Footex.IntegrationTests.Common;
using Infrastructure;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Footex.IntegrationTests.CQRS.Matches.Queries;

public class GetUserMatchesQueryHandlerIntegrationTests(FootexWebApplicationFactory factory)
    : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task Handle_WithValidUserId_ReturnsUserMatches()
    {
        // Arrange
        var user = TestData.CreateTestUser(true);
        var homeTeam = TestData.CreateTestDbTeam();
        var awayTeam = TestData.CreateTestDbTeam();
        var season = TestData.CreateTestDbSeason();
        await Context.Users.AddAsync(user);
        await Context.Teams.AddRangeAsync(homeTeam, awayTeam);
        await Context.Seasons.AddAsync(season);
        await Context.SaveChangesAsync();

        var homeSeasonTeam = TestData.CreateTestDbSeasonTeam(season.Id, homeTeam.Id);
        var awaySeasonTeam = TestData.CreateTestDbSeasonTeam(season.Id, awayTeam.Id);
        await Context.TeamSeasons.AddRangeAsync(homeSeasonTeam, awaySeasonTeam);
        await Context.SaveChangesAsync();

        var match = TestData.CreateTestMatch(homeTeam.Id, awayTeam.Id, season.Id, true, user.Id);
        await Context.Matches.AddAsync(match);
        await Context.SaveChangesAsync();

        var query = new GetUserMatchesQuery { UserId = user.Id };

        // Act
        var response = await Mediator.Send(query);

        // Assert
        response.Should().NotBeNull();
        response.Succeeded.Should().BeTrue();
        response.Matches.Should().NotBeNull();
        response.Matches.Should().Contain(m => m.Id == match.Id);
    }
}
