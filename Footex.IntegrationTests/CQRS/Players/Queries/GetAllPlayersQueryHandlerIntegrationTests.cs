using Application.CQRS.Players.Queries;
using FluentAssertions;
using Footex.IntegrationTests.Common;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Footex.IntegrationTests.CQRS.Players.Queries;

public class GetAllPlayersQueryHandlerIntegrationTests : IClassFixture<FootexWebApplicationFactory>
{
    private readonly FootexWebApplicationFactory _factory;
    private readonly IMediator _mediator;

    public GetAllPlayersQueryHandlerIntegrationTests(FootexWebApplicationFactory factory)
    {
        _factory = factory;
        _mediator = factory.Services.GetRequiredService<IMediator>();
    }

    [Fact]
    public async Task Handle_WithNoFilters_ReturnsAllPlayers()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        await TestData.SeedTestData(scope.ServiceProvider);

        var query = new GetAllPlayersQuery();

        // Act
        var result = await _mediator.Send(query);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Players.Should().NotBeNull();
        result.Players!.Count.Should().BeGreaterThan(0);
        result.Error.Should().BeNull();

        // Verify player data structure
        var firstPlayer = result.Players.First();
        firstPlayer.Id.Should().BeGreaterThan(0);
        firstPlayer.FullName.Should().NotBeNullOrEmpty();
        firstPlayer.Position.Should().NotBeNullOrEmpty();
        firstPlayer.Nationality.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Handle_WithPagination_ReturnsPagedResults()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        await TestData.SeedTestData(scope.ServiceProvider);

        var pageNumber = 1;
        var pageSize = 2;
        var query = new GetAllPlayersQuery
        {
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        // Act
        var result = await _mediator.Send(query);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Players.Should().NotBeNull();
        result.Players!.Count.Should().BeLessOrEqualTo(pageSize);
        result.Error.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithNationalityFilter_ReturnsFilteredPlayers()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        await TestData.SeedTestData(scope.ServiceProvider);

        // Create players with specific nationality
        var testNationality = "Brazil";
        await TestData.SeedPlayersWithNationality(scope.ServiceProvider, testNationality, 2);

        var query = new GetAllPlayersQuery
        {
            Nationality = testNationality
        };

        // Act
        var result = await _mediator.Send(query);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Players.Should().NotBeNull();
        result.Players!.Count.Should().BeGreaterThan(0);
        result.Players.All(p => p.Nationality == testNationality).Should().BeTrue();
        result.Error.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithPreferredFootFilter_ReturnsFilteredPlayers()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        await TestData.SeedTestData(scope.ServiceProvider);

        // Create players with specific preferred foot
        var testPreferredFoot = "Left";
        await TestData.SeedPlayersWithPreferredFoot(scope.ServiceProvider, testPreferredFoot, 2);

        var query = new GetAllPlayersQuery
        {
            PreferredFoot = testPreferredFoot
        };

        // Act
        var result = await _mediator.Send(query);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Players.Should().NotBeNull();
        result.Players!.Count.Should().BeGreaterThan(0);
        result.Players.All(p => p.PreferredFoot == testPreferredFoot).Should().BeTrue();
        result.Error.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithTeamIdFilter_ReturnsPlayersFromSpecificTeam()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        await TestData.SeedTestData(scope.ServiceProvider);

        // Get existing team ID from seeded data
        var testTeamId = 1;
        await TestData.SeedPlayersForTeam(scope.ServiceProvider, testTeamId, 3);

        var query = new GetAllPlayersQuery
        {
            TeamId = testTeamId
        };

        // Act
        var result = await _mediator.Send(query);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Players.Should().NotBeNull();
        result.Players!.Count.Should().BeGreaterThan(0);
        result.Players.All(p => p.TeamId == testTeamId).Should().BeTrue();
        result.Error.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithNonExistentNationality_ReturnsEmptyList()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        await TestData.SeedTestData(scope.ServiceProvider);

        var query = new GetAllPlayersQuery
        {
            Nationality = "NonExistentCountry"
        };

        // Act
        var result = await _mediator.Send(query);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Players.Should().NotBeNull();
        result.Players!.Count.Should().Be(0);
        result.Error.Should().BeNull();
    }

    [Fact]
    public async Task Handle_FilterPriority_NationalityTakesPrecedence()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        await TestData.SeedTestData(scope.ServiceProvider);

        var testNationality = "Spain";
        var testPreferredFoot = "Right";
        await TestData.SeedPlayersWithNationality(scope.ServiceProvider, testNationality, 2);

        var query = new GetAllPlayersQuery
        {
            Nationality = testNationality,
            PreferredFoot = testPreferredFoot,
            TeamId = 1
        };

        // Act
        var result = await _mediator.Send(query);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Players.Should().NotBeNull();
        result.Players!.Count.Should().BeGreaterThan(0);

        // Should filter by nationality only (highest priority)
        result.Players.All(p => p.Nationality == testNationality).Should().BeTrue();
        result.Error.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithEmptyStringFilters_TreatedAsNoFilter()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        await TestData.SeedTestData(scope.ServiceProvider);

        var query = new GetAllPlayersQuery
        {
            Nationality = "",
            PreferredFoot = "   " // whitespace only
        };

        // Act
        var result = await _mediator.Send(query);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Players.Should().NotBeNull();
        result.Players!.Count.Should().BeGreaterThan(0); // Should return all players
        result.Error.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithMultipleFiltersApplied_ReturnsCorrectResults()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        await TestData.SeedTestData(scope.ServiceProvider);

        // Test that when no nationality is provided, preferred foot filter works
        var testPreferredFoot = "Left";
        await TestData.SeedPlayersWithPreferredFoot(scope.ServiceProvider, testPreferredFoot, 2);

        var query = new GetAllPlayersQuery
        {
            PreferredFoot = testPreferredFoot,
            TeamId = 1 // This should be ignored since PreferredFoot has higher priority
        };

        // Act
        var result = await _mediator.Send(query);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Players.Should().NotBeNull();
        result.Players!.Count.Should().BeGreaterThan(0);
        result.Players.All(p => p.PreferredFoot == testPreferredFoot).Should().BeTrue();
        result.Error.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithValidData_ReturnsProperlyMappedDtos()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        await TestData.SeedTestData(scope.ServiceProvider);

        var query = new GetAllPlayersQuery
        {
            PageNumber = 1,
            PageSize = 1
        };

        // Act
        var result = await _mediator.Send(query);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Players.Should().NotBeNull();
        result.Players!.Count.Should().Be(1);

        var player = result.Players.First();
        player.Id.Should().BeGreaterThan(0);
        player.FullName.Should().NotBeNullOrEmpty();
        player.KnownName.Should().NotBeNull();
        player.Nationality.Should().NotBeNullOrEmpty();
        player.Position.Should().NotBeNullOrEmpty();
        player.PreferredFoot.Should().NotBeNullOrEmpty();
        player.PhotoUrl.Should().NotBeNull();
        result.Error.Should().BeNull();
    }
}