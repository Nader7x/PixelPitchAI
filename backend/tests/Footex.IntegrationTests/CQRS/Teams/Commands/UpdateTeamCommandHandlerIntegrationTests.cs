using Application.CQRS.Teams.Commands;
using FluentAssertions;
using Footex.IntegrationTests.Common;
using Xunit;

namespace Footex.IntegrationTests.CQRS.Teams.Commands;

public class UpdateTeamCommandHandlerIntegrationTests(FootexWebApplicationFactory factory)
    : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task Handle_WithValidCommand_UpdatesTeamInDatabase()
    {
        // Arrange
        var team = TestData.CreateTestDbTeam();
        Context.Teams.Add(team);
        await Context.SaveChangesAsync();

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
        var response = await Mediator.Send(command);

        // Assert
        response.Should().NotBeNull();
        response.Succeeded.Should().BeTrue();
        response.Id.Should().Be(team.Id);
        response.Name.Should().Be(command.Name);

        var updatedTeam = await Context.Teams.FindAsync(team.Id);
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
        var response = await Mediator.Send(command);

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
        Context.Teams.AddRange(team1, team2);
        await Context.SaveChangesAsync();

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
        var response = await Mediator.Send(command);

        // Assert
        response.Should().NotBeNull();
        response.Succeeded.Should().BeFalse();
        response.Error.Should().Contain("already exists");
    }
}
