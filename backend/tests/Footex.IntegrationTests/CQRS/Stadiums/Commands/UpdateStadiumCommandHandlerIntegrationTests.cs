using Application.CQRS.Stadiums.Commands;
using Domain.Interfaces;
using FluentAssertions;
using Footex.IntegrationTests.Common;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Footex.IntegrationTests.CQRS.Stadiums.Commands;

public class UpdateStadiumCommandHandlerIntegrationTests(FootexWebApplicationFactory factory)
    : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task Handle_ValidUpdateCommand_UpdatesStadiumSuccessfully()
    {
        // Arrange
        var stadium = TestData.CreateTestDbStadium("UpdateTest");
        await UnitOfWork.Stadiums.AddAsync(stadium);
        await UnitOfWork.SaveChangesAsync();

        var command = new UpdateStadiumCommand
        {
            Id = stadium.Id,
            Name = "Updated Stadium Name",
            City = "Updated City",
            Country = "Updated Country",
            Capacity = 12345,
            BuiltDate = new DateTime(2001, 1, 1),
            Description = "Updated Description",
        };

        // Act
        var result = await Mediator.Send(command, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Id.Should().Be(stadium.Id);
        result.Name.Should().Be(command.Name);
        result.Error.Should().BeNull();

        var updatedStadium = await UnitOfWork.Stadiums.GetByIdAsync(stadium.Id);
        updatedStadium.Should().NotBeNull();
        updatedStadium!.Name.Should().Be(command.Name);
        updatedStadium.City.Should().Be(command.City);
        updatedStadium.Country.Should().Be(command.Country);
        updatedStadium.Capacity.Should().Be(command.Capacity);
        updatedStadium.BuiltDate.Should().Be(command.BuiltDate);
        updatedStadium.Description.Should().Be(command.Description);
    }

    [Fact]
    public async Task Handle_NonExistentId_ReturnsNotFound()
    {
        // Arrange
        var command = new UpdateStadiumCommand
        {
            Id = 999999,
            Name = "Does Not Exist",
            City = "Nowhere",
            Country = "Nowhere",
            Capacity = 1,
            BuiltDate = DateTime.UtcNow,
        };

        // Act
        var result = await Mediator.Send(command, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.NotFound.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_DuplicateName_ReturnsError()
    {
        // Arrange
        var stadium1 = TestData.CreateTestDbStadium("Stadium1");
        var stadium2 = TestData.CreateTestDbStadium("Stadium2");
        await UnitOfWork.Stadiums.AddAsync(stadium1);
        await UnitOfWork.Stadiums.AddAsync(stadium2);
        await UnitOfWork.SaveChangesAsync();

        var command = new UpdateStadiumCommand
        {
            Id = stadium2.Id,
            Name = stadium1.Name, // duplicate name
            City = stadium2.City,
            Country = stadium2.Country,
            Capacity = stadium2.Capacity,
            BuiltDate = stadium2.BuiltDate ?? DateTime.UtcNow,
        };

        // Act
        var result = await Mediator.Send(command, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Error.Should().Contain("already exists");
    }

    [Fact]
    public async Task Handle_InvalidData_ReturnsError()
    {
        // Arrange
        var stadium = TestData.CreateTestDbStadium("InvalidData");
        await UnitOfWork.Stadiums.AddAsync(stadium);
        await UnitOfWork.SaveChangesAsync();

        var command = new UpdateStadiumCommand
        {
            Id = stadium.Id,
            Name = "", // Invalid: Name required
            City = "",
            Country = "",
            Capacity = -1, // Invalid: Capacity must be positive
            BuiltDate = DateTime.MinValue,
        };

        // Act
        var result = await Mediator.Send(command, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Error.Should().NotBeNull();
    }
}
