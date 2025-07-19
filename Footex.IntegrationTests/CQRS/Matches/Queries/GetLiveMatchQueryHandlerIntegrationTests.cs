using Application.CQRS.Matches.Queries;
using FluentAssertions;
using Footex.IntegrationTests.Common;
using Infrastructure;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Footex.IntegrationTests.CQRS.Matches.Queries;

public class GetLiveMatchQueryHandlerIntegrationTests(FootexWebApplicationFactory factory)
    : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task Handle_WithValidUserId_ReturnsLiveMatchOrNotFound()
    {
        // Arrange
        var user = TestData.CreateTestUser(true);
        await Context.Users.AddAsync(user);
        var homeTeam = TestData.CreateTestDbTeam();
        var awayTeam = TestData.CreateTestDbTeam();
        var season = TestData.CreateTestDbSeason();
        await Context.Teams.AddRangeAsync(homeTeam, awayTeam);
        await Context.Seasons.AddAsync(season);
        await Context.SaveChangesAsync();

        var homeSeasonTeam = TestData.CreateTestDbSeasonTeam(season.Id, homeTeam.Id);
        var awaySeasonTeam = TestData.CreateTestDbSeasonTeam(season.Id, awayTeam.Id);
        await Context.TeamSeasons.AddRangeAsync(homeSeasonTeam, awaySeasonTeam);
        await Context.SaveChangesAsync();

        var match = TestData.CreateTestMatch(homeTeam.Id, awayTeam.Id, season.Id, true, user.Id);
        match.MatchStatus = "Live";
        match.IsLive = true;
        await Context.Matches.AddAsync(match);
        await Context.SaveChangesAsync();

        var query = new GetLiveMatchQuery { UserId = user.Id };

        // Act
        var response = await Mediator.Send(query);

        // Assert
        response.Should().NotBeNull();
        response.Succeeded.Should().BeTrue();
        response.HasLiveMatch.Should().BeTrue();
        response.LiveMatch.Should().NotBeNull();
        response.LiveMatch!.Id.Should().Be(match.Id);
    }
}
