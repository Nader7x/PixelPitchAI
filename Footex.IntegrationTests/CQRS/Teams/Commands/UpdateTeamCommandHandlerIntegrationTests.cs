using Application.CQRS.Teams.Commands;
using FluentAssertions;
using Footex.IntegrationTests.Common;
using Infrastructure;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Footex.IntegrationTests.CQRS.Teams.Commands;

public class UpdateTeamCommandHandlerIntegrationTests
    : IClassFixture<FootexWebApplicationFactory>,
        IDisposable
{
    private readonly FootballDbContext _context;
    private readonly FootexWebApplicationFactory _factory;
    private readonly IMediator _mediator;
    private readonly IServiceScope _scope;

    public UpdateTeamCommandHandlerIntegrationTests(FootexWebApplicationFactory factory)
    {
        _factory = factory;
        _scope = _factory.Services.CreateScope();
        _mediator = _scope.ServiceProvider.GetRequiredService<IMediator>();
        _context = _scope.ServiceProvider.GetRequiredService<FootballDbContext>();
    }

    [Fact]
    public async Task Handle_WithValidCommand_UpdatesTeamInDatabase()
    {
        // Arrange
        var team = TestData.CreateTestDbTeam();
        _context.Teams.Add(team);
        await _context.SaveChangesAsync();

        var command = new UpdateTeamCommand
        {
            Id = team.Id,
            Name = "Updated Name",
            ShortName = team.ShortName,
            FoundationDate = team.FoundationDate,
            City = team.City,
            Country = team.Country,
            PrimaryColor = "#00FF00",
            SecondaryColor = "#000000",
        };

        // Act
        var response = await _mediator.Send(command);

        // Assert
        response.Should().NotBeNull();
        response.Succeeded.Should().BeTrue();
        response.Id.Should().Be(team.Id);
        response.Name.Should().Be(command.Name);

        var updatedTeam = await _context.Teams.FindAsync(team.Id);
        updatedTeam.Should().NotBeNull();
        updatedTeam!.Name.Should().Be(command.Name);
        updatedTeam.PrimaryColor.Should().Be(command.PrimaryColor);
        updatedTeam.SecondaryColor.Should().Be(command.SecondaryColor);
    }

    [Fact]
    public async Task Handle_WithNonExistentTeam_ReturnsNotFound()
    {
        // Arrange
        var command = new UpdateTeamCommand
        {
            Id = 99999,
            Name = "Nonexistent",
            ShortName = "NON",
            FoundationDate = new DateTime(2000, 1, 1),
            City = "City",
            Country = "Country",
        };

        // Act
        var response = await _mediator.Send(command);

        // Assert
        response.Should().NotBeNull();
        response.Succeeded.Should().BeFalse();
        response.NotFound.Should().BeTrue();
        response.Error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Handle_WithDuplicateName_ReturnsFailure()
    {
        // Arrange
        var team1 = TestData.CreateTestDbTeam();
        var team2 = TestData.CreateTestDbTeam();
        team2.Name = "Another Team";
        team2.ShortName = "ANT";
        _context.Teams.AddRange(team1, team2);
        await _context.SaveChangesAsync();

        var command = new UpdateTeamCommand
        {
            Id = team2.Id,
            Name = team1.Name, // Duplicate name
            ShortName = team2.ShortName,
            FoundationDate = team2.FoundationDate,
            City = team2.City,
            Country = team2.Country,
        };

        // Act
        var response = await _mediator.Send(command);

        // Assert
        response.Should().NotBeNull();
        response.Succeeded.Should().BeFalse();
        response.Error.Should().Contain("already exists");
    }

    public void Dispose()
    {
        _scope?.Dispose();
    }
}
