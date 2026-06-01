using Application.CQRS.Stadiums.Queries;
using Domain.Interfaces;
using FluentAssertions;
using Footex.IntegrationTests.Common;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Footex.IntegrationTests.CQRS.Stadiums.Queries;

public class GetStadiumByIdQueryHandlerIntegrationTests(FootexWebApplicationFactory factory)
    : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task Handle_ValidStadiumId_ReturnsStadiumSuccessfully()
    {
        // Arrange
        var stadium = TestData.CreateTestDbStadium("Santiago Bernabéu");
        stadium.City = "Madrid, Spain";
        stadium.Capacity = 81044;
        stadium.BuiltDate = new DateTime(1947, 1, 1);
        stadium.Description = "Home of Real Madrid";

        await UnitOfWork.Stadiums.AddAsync(stadium);
        await UnitOfWork.SaveChangesAsync();

        var query = new GetStadiumByIdQuery { Id = stadium.Id };

        // Act
        var result = await Mediator.Send(query, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Stadium.Should().NotBeNull();
        result.Stadium!.Id.Should().Be(stadium.Id);
        result.Stadium.Name.Should().Be(stadium.Name);
        result.Stadium.City.Should().Be(stadium.City);
        result.Stadium.Capacity.Should().Be(stadium.Capacity);
        result.Stadium.BuiltDate.Should().Be(stadium.BuiltDate);
        result.Stadium.Description.Should().Be(stadium.Description);
        result.Error.Should().BeNull();
        result.NotFound.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_NonExistentStadiumId_ReturnsNotFoundResponse()
    {
        // Arrange
        const int nonExistentId = -1;
        var query = new GetStadiumByIdQuery { Id = nonExistentId };

        // Act
        var result = await Mediator.Send(query, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.NotFound.Should().BeTrue();
        result.Stadium.Should().BeNull();
        result.Error.Should().Contain($"Stadium with ID {nonExistentId} not found");
    }

    [Fact]
    public async Task Handle_StadiumWithAllFields_ReturnsCompleteStadiumDto()
    {
        // Arrange
        var stadium = TestData.CreateTestDbStadium("Allianz Arena");
        stadium.City = "Munich, Germany";
        stadium.Capacity = 75000;
        stadium.BuiltDate = new DateTime(2005, 1, 1);
        stadium.Description = "Home of Bayern Munich";
        stadium.ImageUrl = "https://example.com/allianz-arena.jpg";
        stadium.SurfaceType = "Natural Grass";
        stadium.HasRoof = true;
        stadium.Architect = "Herzog & de Meuron";

        await UnitOfWork.Stadiums.AddAsync(stadium);
        await UnitOfWork.SaveChangesAsync();

        var query = new GetStadiumByIdQuery { Id = stadium.Id };

        // Act
        var result = await Mediator.Send(query, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Stadium.Should().NotBeNull();
        result.Stadium!.ImageUrl.Should().Be(stadium.ImageUrl);
        result.Stadium.SurfaceType.Should().Be(stadium.SurfaceType);
    }

    [Fact]
    public async Task Handle_DeletedStadium_ReturnsNotFoundResponse()
    {
        // Arrange
        var stadium = TestData.CreateTestDbStadium("Deleted Stadium");
        await UnitOfWork.Stadiums.AddAsync(stadium);
        await UnitOfWork.SaveChangesAsync();
        // Simulate deletion by removing it from the database
        UnitOfWork.Stadiums.Delete(stadium);
        await UnitOfWork.SaveChangesAsync();

        var query = new GetStadiumByIdQuery { Id = stadium.Id };

        // Act
        var result = await Mediator.Send(query, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.NotFound.Should().BeTrue();
        result.Stadium.Should().BeNull();
        result.Error.Should().Contain($"Stadium with ID {stadium.Id} not found");
    }

    [Fact]
    public async Task Handle_StadiumWithMinimalData_ReturnsStadiumSuccessfully()
    {
        // Arrange
        var stadium = TestData.CreateTestDbStadium("Simple Stadium");
        stadium.City = "Simple City";
        stadium.Capacity = 5000;
        // No optional fields set

        await UnitOfWork.Stadiums.AddAsync(stadium);
        await UnitOfWork.SaveChangesAsync();

        var query = new GetStadiumByIdQuery { Id = stadium.Id };

        // Act
        var result = await Mediator.Send(query, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Stadium.Should().NotBeNull();
        result.Stadium!.Name.Should().Be(stadium.Name);
        result.Stadium.City.Should().Be(stadium.City);
        result.Stadium.Capacity.Should().Be(stadium.Capacity);
        result.Stadium.BuiltDate.Should().BeNull();
        result.Stadium.Description.Should().BeNull();
        result.Stadium.ImageUrl.Should().BeNull();
    }

    [Fact]
    public async Task Handle_MultipleStadiumsInDatabase_ReturnsCorrectStadium()
    {
        // Arrange
        var stadium1 = TestData.CreateTestDbStadium("Stadium 1");
        var stadium2 = TestData.CreateTestDbStadium("Stadium 2");
        var stadium3 = TestData.CreateTestDbStadium("Stadium 3");

        await UnitOfWork.Stadiums.AddAsync(stadium1);
        await UnitOfWork.Stadiums.AddAsync(stadium2);
        await UnitOfWork.Stadiums.AddAsync(stadium3);
        await UnitOfWork.SaveChangesAsync();

        var query = new GetStadiumByIdQuery { Id = stadium2.Id };

        // Act
        var result = await Mediator.Send(query, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Stadium.Should().NotBeNull();
        result.Stadium!.Id.Should().Be(stadium2.Id);
        result.Stadium.Name.Should().Be(stadium2.Name);
        result.Stadium.Id.Should().NotBe(stadium1.Id);
        result.Stadium.Id.Should().NotBe(stadium3.Id);
    }

    [Fact]
    public async Task Handle_StadiumWithLargeCapacity_ReturnsCorrectCapacity()
    {
        // Arrange
        var stadium = TestData.CreateTestDbStadium("Huge Stadium");
        stadium.Capacity = 200000; // Very large capacity
        await UnitOfWork.Stadiums.AddAsync(stadium);
        await UnitOfWork.SaveChangesAsync();

        var query = new GetStadiumByIdQuery { Id = stadium.Id };

        // Act
        var result = await Mediator.Send(query, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Stadium.Should().NotBeNull();
        result.Stadium!.Capacity.Should().Be(200000);
    }

    [Fact]
    public async Task Handle_HistoricStadium_ReturnsCorrectYear()
    {
        // Arrange
        var stadium = TestData.CreateTestDbStadium("Historic Stadium");
        stadium.BuiltDate = new DateTime(1860, 1, 1); // Very old stadium
        await UnitOfWork.Stadiums.AddAsync(stadium);
        await UnitOfWork.SaveChangesAsync();

        var query = new GetStadiumByIdQuery { Id = stadium.Id };

        // Act
        var result = await Mediator.Send(query, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Stadium.Should().NotBeNull();
        result.Stadium?.BuiltDate.Should().Be(new DateTime(1860, 1, 1));
    }

    [Fact]
    public async Task Handle_DatabaseException_ReturnsErrorResponse()
    {
        // Arrange
        var query = new GetStadiumByIdQuery { Id = -1 };

        // Dispose the context to simulate database error
        await DisposeContext();

        // Act
        var result = await Mediator.Send(query, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Stadium.Should().BeNull();
        result.NotFound.Should().BeFalse(); // Should be false for exceptions, not not-found
    }

    private Task DisposeContext()
    {
        UnitOfWork.Dispose();
        return Task.CompletedTask;
    }
}
