using Application.CQRS.Teams.Queries;
using FluentAssertions;
using Footex.IntegrationTests.Common;
using Infrastructure;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Footex.IntegrationTests.CQRS.Teams.Queries;

public class GetAllTeamsQueryHandlerIntegrationTests
    : IClassFixture<FootexWebApplicationFactory>,
        IDisposable
{
    private readonly FootballDbContext _context;
    private readonly FootexWebApplicationFactory _factory;
    private readonly IMediator _mediator;
    private readonly IServiceScope _scope;

    public GetAllTeamsQueryHandlerIntegrationTests(FootexWebApplicationFactory factory)
    {
        _factory = factory;
        _scope = _factory.Services.CreateScope();
        _mediator = _scope.ServiceProvider.GetRequiredService<IMediator>();
        _context = _scope.ServiceProvider.GetRequiredService<FootballDbContext>();
    }

    [Fact]
    public async Task Handle_ReturnsAllTeams()
    {
        // Arrange
        _context.Teams.RemoveRange(_context.Teams);
        await _context.SaveChangesAsync();
        var team1 = TestData.CreateTestDbTeam();
        var team2 = TestData.CreateTestDbTeam();
        team2.Name = "Second Team";
        team2.ShortName = "ST2";
        await _context.Teams.AddRangeAsync(team1, team2);
        await _context.SaveChangesAsync();

        // Act
        var response = await _mediator.Send(new GetAllTeamsQuery());

        // Assert
        response.Should().NotBeNull();
        response.Teams.Should().HaveCount(2);
        response.Teams.Select(t => t.Name).Should().Contain(new[] { team1.Name, team2.Name });
    }

    [Fact]
    public async Task Handle_WhenNoTeamsExist_ReturnsEmptyList()
    {
        // Arrange
        _context.Teams.RemoveRange(_context.Teams);
        await _context.SaveChangesAsync();

        // Act
        var response = await _mediator.Send(new GetAllTeamsQuery());

        // Assert
        response.Should().NotBeNull();
        response.Teams.Should().BeEmpty();
    }

    public void Dispose()
    {
        _scope?.Dispose();
    }
}
