using Application.CQRS.Teams.Queries;
using FluentAssertions;
using Footex.IntegrationTests.Common;
using Infrastructure;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Footex.IntegrationTests.CQRS.Teams.Queries;

public class GetTeamSeasonsQueryHandlerIntegrationTests
    : IClassFixture<FootexWebApplicationFactory>,
        IDisposable
{
    private readonly FootballDbContext _context;
    private readonly FootexWebApplicationFactory _factory;
    private readonly IMediator _mediator;
    private readonly IServiceScope _scope;

    public GetTeamSeasonsQueryHandlerIntegrationTests(FootexWebApplicationFactory factory)
    {
        _factory = factory;
        _scope = _factory.Services.CreateScope();
        _mediator = _scope.ServiceProvider.GetRequiredService<IMediator>();
        _context = _scope.ServiceProvider.GetRequiredService<FootballDbContext>();
    }

    [Fact]
    public async Task Handle_WithValidTeamId_ReturnsSeasons()
    {
        // Arrange
        var team = TestData.CreateTestDbTeam();
        _context.Teams.Add(team);
        await _context.SaveChangesAsync();

        var season = TestData.CreateTestDbSeason();
        _context.Seasons.Add(season);
        await _context.SaveChangesAsync();

        var teamSeason = TestData.CreateTestDbSeasonTeam(season.Id , team.Id);
        _context.TeamSeasons.Add(teamSeason);
        await _context.SaveChangesAsync();

        var query = new GetTeamSeasonsQuery { TeamId = team.Id };

        // Act
        var response = await _mediator.Send(query);

        // Assert
        response.Should().NotBeNull();
        response.Succeeded.Should().BeTrue();
        response.TeamId.Should().Be(team.Id);
        response.TeamName.Should().Be(team.Name);
        response.Seasons.Should().NotBeNull();
        response.Seasons.Should().ContainSingle();
        response.Seasons![0].SeasonId.Should().Be(season.Id);
        response.Seasons[0].SeasonName.Should().Be(season.Name);
    }

    [Fact]
    public async Task Handle_WithInvalidTeamId_ReturnsNotFound()
    {
        // Arrange
        var query = new GetTeamSeasonsQuery { TeamId = 999999 };

        // Act
        var response = await _mediator.Send(query);

        // Assert
        response.Should().NotBeNull();
        response.Succeeded.Should().BeFalse();
        response.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_WithNoSeasons_ReturnsEmptySeasonsList()
    {
        // Arrange
        var team = TestData.CreateTestDbTeam();
        _context.Teams.Add(team);
        await _context.SaveChangesAsync();

        var query = new GetTeamSeasonsQuery { TeamId = team.Id };

        // Act
        var response = await _mediator.Send(query);

        // Assert
        response.Should().NotBeNull();
        response.Succeeded.Should().BeTrue();
        response.TeamId.Should().Be(team.Id);
        response.Seasons.Should().NotBeNull();
        response.Seasons.Should().BeEmpty();
    }

    public void Dispose()
    {
        _scope?.Dispose();
    }
}
