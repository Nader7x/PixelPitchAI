using Application.CQRS.Stadiums.Commands;
using Domain.Interfaces;
using FluentAssertions;
using Footex.IntegrationTests.Common;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Footex.IntegrationTests.CQRS.Stadiums.Commands;

public class CreateStadiumCommandHandlerIntegrationTests(FootexWebApplicationFactory factory)
    : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task Handle_ValidStadiumCommand_CreatesStadiumSuccessfully()
    {
        // Arrange
        var command = new CreateStadiumCommand
        {
            Name = "Old Trafford",
            City = "Manchester, England",
            Capacity = 74879,
            BuiltDate = new DateTime(1910, 1, 1),
            Description = "The Theatre of Dreams",
        };

        // Act
        var result = await Mediator.Send(command, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Id.Should().NotBe(-1);
        result.Name.Should().Be(command.Name);
        result.Error.Should().BeNull();

        var savedStadium = await UnitOfWork.Stadiums.GetByIdAsync(result.Id);
        savedStadium.Should().NotBeNull();
        savedStadium!.Name.Should().Be(command.Name);
        savedStadium.City.Should().Be(command.City);
        savedStadium.Capacity.Should().Be(command.Capacity);
        savedStadium.BuiltDate.Should().Be(command.BuiltDate);
        savedStadium.Description.Should().Be(command.Description);
    }

    [Fact]
    public async Task Handle_MinimalValidStadiumCommand_CreatesStadiumSuccessfully()
    {
        // Arrange
        var command = new CreateStadiumCommand
        {
            Name = "Simple Stadium",
            City = "Test City",
            Capacity = 1000,
        };

        // Act
        var result = await Mediator.Send(command, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Id.Should().NotBe(-1);
        result.Name.Should().Be(command.Name);

        var savedStadium = await UnitOfWork.Stadiums.GetByIdAsync(result.Id);
        savedStadium.Should().NotBeNull();
        savedStadium!.Name.Should().Be(command.Name);
        savedStadium.City.Should().Be(command.City);
        savedStadium.Capacity.Should().Be(command.Capacity);
        savedStadium.BuiltDate.Should().Be(default);
        savedStadium.Description.Should().BeNull();
    }

    [Fact]
    public async Task Handle_StadiumWithAllOptionalFields_CreatesStadiumSuccessfully()
    {
        // Arrange
        var command = new CreateStadiumCommand
        {
            Name = "Camp Nou",
            City = "Barcelona, Spain",
            Capacity = 99354,
            BuiltDate = new DateTime(1957, 1, 1),
            Description = "Home of FC Barcelona",
            ImageUrl = "https://example.com/camp-nou.jpg",
            SurfaceType = "Natural Grass",
        };

        // Act
        var result = await Mediator.Send(command, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Id.Should().NotBe(-1);

        var savedStadium = await UnitOfWork.Stadiums.GetByIdAsync(result.Id);
        savedStadium.Should().NotBeNull();
        savedStadium!.ImageUrl.Should().Be(command.ImageUrl);
        savedStadium.SurfaceType.Should().Be(command.SurfaceType);
    }

    [Fact]
    public async Task Handle_DuplicateStadiumName_HandlesGracefully()
    {
        // Arrange
        var stadium = TestData.CreateTestDbStadium("Wembley Stadium");
        await UnitOfWork.Stadiums.AddAsync(stadium);
        await UnitOfWork.SaveChangesAsync();

        var command = new CreateStadiumCommand
        {
            Name = "Wembley Stadium",
            City = "London, England",
            Capacity = 90000,
        };

        // Act
        var result = await Mediator.Send(command, CancellationToken.None);

        // Assert
        // The implementation should handle this scenario
        // Either allow duplicate names or return appropriate error
        if (result.Succeeded)
        {
            result.Id.Should().NotBe(-1);
            result.Id.Should().NotBe(stadium.Id);
        }
        else
        {
            result.Error.Should().NotBeNull();
        }
    }

    [Fact]
    public async Task Handle_StadiumWithLargeCapacity_CreatesSuccessfully()
    {
        // Arrange
        var command = new CreateStadiumCommand
        {
            Name = "Rungrado 1st of May Stadium",
            City = "Pyongyang, North Korea",
            Capacity = 114000, // World's largest stadium by capacity
            BuiltDate = new DateTime(1989, 1, 1),
        };

        // Act
        var result = await Mediator.Send(command, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Id.Should().NotBe(-1);

        var savedStadium = await UnitOfWork.Stadiums.GetByIdAsync(result.Id);
        savedStadium.Should().NotBeNull();
        savedStadium!.Capacity.Should().Be(114000);
    }

    [Fact]
    public async Task Handle_StadiumWithHistoricalYear_CreatesSuccessfully()
    {
        // Arrange
        var command = new CreateStadiumCommand
        {
            Name = "Historic Stadium",
            City = "Old City",
            Capacity = 25000,
            BuiltDate = new DateTime(1888, 1, 1), // Very old stadium
        };

        // Act
        var result = await Mediator.Send(command, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();

        var savedStadium = await UnitOfWork.Stadiums.GetByIdAsync(result.Id);
        savedStadium.Should().NotBeNull();
        savedStadium!.BuiltDate.Should().Be(new DateTime(1888, 1, 1));
    }

    [Fact]
    public async Task Handle_MultipleStadiums_CreatesAllSuccessfully()
    {
        // Arrange
        var commands = new[]
        {
            new CreateStadiumCommand
            {
                Name = "Stadium 1",
                City = "City 1",
                Capacity = 50000,
            },
            new CreateStadiumCommand
            {
                Name = "Stadium 2",
                City = "City 2",
                Capacity = 45000,
            },
            new CreateStadiumCommand
            {
                Name = "Stadium 3",
                City = "City 3",
                Capacity = 40000,
            },
        };

        var results = new List<CreateStadiumCommandResponse>();

        // Act
        foreach (var command in commands)
        {
            var result = await Mediator.Send(command, CancellationToken.None);
            results.Add(result);
        }

        // Assert
        results.Should().OnlyContain(r => r.Succeeded);
        results.Should().HaveCount(3);

        // Verify all have unique IDs
        var ids = results.Select(r => r.Id).ToList();
        ids.Distinct().Should().HaveCount(3);

        // Verify all are saved in database
        foreach (var result in results)
        {
            var savedStadium = await UnitOfWork.Stadiums.GetByIdAsync(result.Id);
            savedStadium.Should().NotBeNull();
        }
    }

    [Fact]
    public async Task Handle_DatabaseException_ReturnsErrorResponse()
    {
        // Arrange
        var command = new CreateStadiumCommand
        {
            Name = "Test Stadium",
            City = "Test City",
            Capacity = 30000,
        };

        // Dispose the context to simulate database error
        await DisposeContext();

        // Act
        var result = await Mediator.Send(command, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Id.Should().Be(0);
    }

    private Task DisposeContext()
    {
        UnitOfWork.Dispose();
        return Task.CompletedTask;
    }
}
