using Application.CQRS.Teams.Queries;
using FluentAssertions;
using Footex.IntegrationTests.Common;
using Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Footex.IntegrationTests.CQRS.Teams.Queries;

public class GetTeamByIdQueryHandlerIntegrationTests(FootexWebApplicationFactory factory)
    : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task Handle_WithValidId_ReturnsTeamFromDatabase()
    {
        // Arrange
        var testTeam = TestData.CreateTestDbTeam();
        Context.Teams.Add(testTeam);
        await Context.SaveChangesAsync();

        var query = new GetTeamByIdQuery { Id = testTeam.Id };

        // Act
        var response = await Mediator.Send(query);

        // Assert
        response.Should().NotBeNull();
        response.Succeeded.Should().BeTrue();
        response.Team.Should().NotBeNull();
        response.Team!.Id.Should().Be(testTeam.Id);
        response.Team.Name.Should().Be(testTeam.Name);
        response.Team.ShortName.Should().Be(testTeam.ShortName);
        response.Team.FoundationDate.Should().Be(testTeam.FoundationDate);
        response.Team.City.Should().Be(testTeam.City);
        response.Team.Country.Should().Be(testTeam.Country);
    }

    [Fact]
    public async Task Handle_WithInvalidId_ReturnsNotFoundResponse()
    {
        // Arrange
        var query = new GetTeamByIdQuery { Id = 999999 };

        // Act
        var response = await Mediator.Send(query);

        // Assert
        response.Should().NotBeNull();
        response.Succeeded.Should().BeFalse();
        response.Team.Should().BeNull();
        response.Error.Should().Contain("Team not found");
    }

    [Fact]
    public async Task Handle_WithDeletedTeam_ReturnsNotFoundResponse()
    {
        // Arrange
        var testTeam = TestData.CreateTestDbTeam(prefix: "Deleted_");
        Context.Teams.Add(testTeam);
        await Context.SaveChangesAsync();
        await Context.Teams.Where(t => t.Id == testTeam.Id).ExecuteDeleteAsync();

        var query = new GetTeamByIdQuery { Id = testTeam.Id };

        // Act
        var response = await Mediator.Send(query);

        // Assert
        response.Should().NotBeNull();
        response.Succeeded.Should().BeFalse();
        response.Team.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ReturnsCompleteTeamData()
    {
        // Arrange
        var testTeam = TestData.CreateTestDbTeam();
        testTeam.PrimaryColor = "#FF0000";
        testTeam.SecondaryColor = "#0000FF";
        testTeam.Logo = "https://example.com/logo.png";

        Context.Teams.Add(testTeam);
        await Context.SaveChangesAsync();

        var query = new GetTeamByIdQuery { Id = testTeam.Id };

        // Act
        var response = await Mediator.Send(query);

        // Assert
        response.Should().NotBeNull();
        response.Succeeded.Should().BeTrue();
        response.Team.Should().NotBeNull();
        response.Team.PrimaryColor.Should().Be(testTeam.PrimaryColor);
        response.Team.SecondaryColor.Should().Be(testTeam.SecondaryColor);
        response.Team.Logo.Should().Be(testTeam.Logo);
    }
}
