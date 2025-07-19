using Application.CQRS.Teams.Commands;
using FluentAssertions;
using Footex.IntegrationTests.Common;
using Infrastructure;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Footex.IntegrationTests.CQRS.Teams.Commands;

public class CreateTeamCommandHandlerIntegrationTests(FootexWebApplicationFactory factory)
    : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task Handle_WithValidCommand_CreatesTeamInDatabase()
    {
        // Arrange
        var command = new CreateTeamCommand
        {
            Name = "Test Team FC",
            ShortName = "TTF",
            FoundationDate = new DateTime(1900, 1, 1),
            City = "Test City",
            Country = "Test Country",
            PrimaryColor = "#FF0000",
            SecondaryColor = "#0000FF",
        };

        // Act
        var response = await Mediator.Send(command);

        // Assert
        response.Should().NotBeNull();
        response.Succeeded.Should().BeTrue();
        response.Id.Should().BeGreaterThan(0);

        // Verify team was created in database
        var createdTeam = await Context.Teams.FindAsync(response.Id);
        createdTeam.Should().NotBeNull();
        createdTeam!.Name.Should().Be(command.Name);
        createdTeam.ShortName.Should().Be(command.ShortName);
        createdTeam.FoundationDate.Should().Be(command.FoundationDate);
        createdTeam.City.Should().Be(command.City);
        createdTeam.Country.Should().Be(command.Country);
        createdTeam.PrimaryColor.Should().Be(command.PrimaryColor);
        createdTeam.SecondaryColor.Should().Be(command.SecondaryColor);
    }

    [Fact]
    public async Task Handle_WithDuplicateName_ReturnsFailureResponse()
    {
        // Arrange
        var existingTeam = TestData.CreateTestDbTeam();
        Context.Teams.Add(existingTeam);
        await Context.SaveChangesAsync();

        var command = new CreateTeamCommand
        {
            Name = existingTeam.Name, // Duplicate name
            ShortName = "DUP",
            FoundationDate = new DateTime(1900, 1, 1),
            City = "Test City",
            Country = "Test Country",
        };

        // Act
        var response = await Mediator.Send(command);

        // Assert
        response.Should().NotBeNull();
        response.Succeeded.Should().BeFalse();
        response.Error.Should().Contain("already exists");
    }

    [Fact]
    public async Task Handle_WithInvalidData_ReturnsFailureResponse()
    {
        // Arrange
        var command = new CreateTeamCommand
        {
            Name = "", // Invalid: empty name
            ShortName = "TST",
            FoundationDate = new DateTime(1900, 1, 1),
            City = "Test City",
            Country = "Test Country",
        };

        // Act
        var response = await Mediator.Send(command);

        // Assert
        response.Should().NotBeNull();
        response.Succeeded.Should().BeFalse();
        response.Error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Handle_ValidatesUniqueShortName()
    {
        // Arrange
        var existingTeam = TestData.CreateTestDbTeam();
        Context.Teams.Add(existingTeam);
        await Context.SaveChangesAsync();

        var command = new CreateTeamCommand
        {
            Name = "Another Team",
            ShortName = existingTeam.ShortName, // Duplicate short name
            FoundationDate = new DateTime(1900, 1, 1),
            City = "Test City",
            Country = "Test Country",
        };

        // Act
        var response = await Mediator.Send(command);

        // Assert
        response.Should().NotBeNull();
        response.Succeeded.Should().BeFalse();
        response.Error.Should().Contain("short name");
    }

    [Fact]
    public async Task Handle_WithMinimalData_CreatesTeamSuccessfully()
    {
        // Arrange
        var command = new CreateTeamCommand
        {
            Name = "Minimal Team",
            ShortName = "MIN",
            FoundationDate = new DateTime(2000, 1, 1),
            City = "City",
            Country = "Country",
            // Optional fields not provided
        };

        // Act
        var response = await Mediator.Send(command);

        // Assert
        response.Should().NotBeNull();
        response.Succeeded.Should().BeTrue();
        response.Id.Should().BeGreaterThan(0);

        // Verify in database
        var createdTeam = await Context.Teams.FindAsync(response.Id);
        createdTeam.Should().NotBeNull();
        createdTeam!.Name.Should().Be(command.Name);
        createdTeam.PrimaryColor.Should().BeNull();
        createdTeam.SecondaryColor.Should().BeNull();
    }
}
