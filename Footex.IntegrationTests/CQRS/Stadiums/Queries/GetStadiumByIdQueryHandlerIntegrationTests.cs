using Application.CQRS.Stadiums.Queries;
using Domain.Interfaces;
using Footex.IntegrationTests.Common;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Footex.IntegrationTests.CQRS.Stadiums.Queries;

public class GetStadiumByIdQueryHandlerIntegrationTests : BaseIntegrationTest
{
    private readonly GetStadiumByIdQueryHandler _handler;
    private readonly IUnitOfWork _unitOfWork;

    public GetStadiumByIdQueryHandlerIntegrationTests(FootexWebApplicationFactory factory) : base(factory)
    {
        _handler = ServiceProvider.GetRequiredService<GetStadiumByIdQueryHandler>();
        _unitOfWork = ServiceProvider.GetRequiredService<IUnitOfWork>();
    }

    [Fact]
    public async Task Handle_ValidStadiumId_ReturnsStadiumSuccessfully()
    {
        // Arrange
        var stadium = TestData.CreateStadium("Santiago Bernabéu");
        stadium.City = "Madrid, Spain";
        stadium.Capacity = 81044;
        stadium.BuiltDate = new DateTime(1947, 1, 1);
        stadium.Description = "Home of Real Madrid";

        await _unitOfWork.Stadiums.AddAsync(stadium);
        await _unitOfWork.SaveChangesAsync();

        var query = new GetStadiumByIdQuery { Id = stadium.Id };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Stadium);
        Assert.Equal(stadium.Id, result.Stadium.Id);
        Assert.Equal(stadium.Name, result.Stadium.Name);
        Assert.Equal(stadium.City, result.Stadium.City);
        Assert.Equal(stadium.Capacity, result.Stadium.Capacity);
        Assert.Equal(stadium.BuiltDate, result.Stadium.BuiltDate);
        Assert.Equal(stadium.Description, result.Stadium.Description);
        Assert.Null(result.Error);
        Assert.False(result.NotFound);
    }

    [Fact]
    public async Task Handle_NonExistentStadiumId_ReturnsNotFoundResponse()
    {
        // Arrange
        var nonExistentId = -1;
        var query = new GetStadiumByIdQuery { Id = nonExistentId };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.NotFound);
        Assert.Null(result.Stadium);
        Assert.Contains($"Stadium with ID {nonExistentId} not found", result.Error);
    }

    [Fact]
    public async Task Handle_StadiumWithAllFields_ReturnsCompleteStadiumDto()
    {
        // Arrange
        var stadium = TestData.CreateStadium("Allianz Arena");
        stadium.City = "Munich, Germany";
        stadium.Capacity = 75000;
        stadium.BuiltDate = new DateTime(2005, 1, 1);
        stadium.Description = "Home of Bayern Munich";
        stadium.ImageUrl = "https://example.com/allianz-arena.jpg";
        stadium.SurfaceType = "Natural Grass";
        stadium.HasRoof = true;
        stadium.Architect = "Herzog & de Meuron";

        await _unitOfWork.Stadiums.AddAsync(stadium);
        await _unitOfWork.SaveChangesAsync();

        var query = new GetStadiumByIdQuery { Id = stadium.Id };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Stadium);
        Assert.Equal(stadium.ImageUrl, result.Stadium.ImageUrl);
        Assert.Equal(stadium.SurfaceType, result.Stadium.SurfaceType);
    }

    [Fact]
    public async Task Handle_DeletedStadium_ReturnsNotFoundResponse()
    {
        // Arrange
        var stadium = TestData.CreateStadium("Deleted Stadium");
        await _unitOfWork.Stadiums.AddAsync(stadium);
        await _unitOfWork.SaveChangesAsync();

        var query = new GetStadiumByIdQuery { Id = stadium.Id };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.NotFound);
        Assert.Null(result.Stadium);
        Assert.Contains($"Stadium with ID {stadium.Id} not found", result.Error);
    }

    [Fact]
    public async Task Handle_StadiumWithMinimalData_ReturnsStadiumSuccessfully()
    {
        // Arrange
        var stadium = TestData.CreateStadium("Simple Stadium");
        stadium.City = "Simple City";
        stadium.Capacity = 5000;
        // No optional fields set

        await _unitOfWork.Stadiums.AddAsync(stadium);
        await _unitOfWork.SaveChangesAsync();

        var query = new GetStadiumByIdQuery { Id = stadium.Id };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Stadium);
        Assert.Equal(stadium.Name, result.Stadium.Name);
        Assert.Equal(stadium.City, result.Stadium.City);
        Assert.Equal(stadium.Capacity, result.Stadium.Capacity);
        Assert.Null(result.Stadium.BuiltDate);
        Assert.Null(result.Stadium.Description);
        Assert.Null(result.Stadium.ImageUrl);
    }

    [Fact]
    public async Task Handle_MultipleStadiumsInDatabase_ReturnsCorrectStadium()
    {
        // Arrange
        var stadium1 = TestData.CreateStadium("Stadium 1");
        var stadium2 = TestData.CreateStadium("Stadium 2");
        var stadium3 = TestData.CreateStadium("Stadium 3");

        await _unitOfWork.Stadiums.AddAsync(stadium1);
        await _unitOfWork.Stadiums.AddAsync(stadium2);
        await _unitOfWork.Stadiums.AddAsync(stadium3);
        await _unitOfWork.SaveChangesAsync();

        var query = new GetStadiumByIdQuery { Id = stadium2.Id };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Stadium);
        Assert.Equal(stadium2.Id, result.Stadium.Id);
        Assert.Equal(stadium2.Name, result.Stadium.Name);
        Assert.NotEqual(stadium1.Id, result.Stadium.Id);
        Assert.NotEqual(stadium3.Id, result.Stadium.Id);
    }

    [Fact]
    public async Task Handle_StadiumWithLargeCapacity_ReturnsCorrectCapacity()
    {
        // Arrange
        var stadium = TestData.CreateStadium("Huge Stadium");
        stadium.Capacity = 200000; // Very large capacity
        await _unitOfWork.Stadiums.AddAsync(stadium);
        await _unitOfWork.SaveChangesAsync();

        var query = new GetStadiumByIdQuery { Id = stadium.Id };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Stadium);
        Assert.Equal(200000, result.Stadium.Capacity);
    }

    [Fact]
    public async Task Handle_HistoricStadium_ReturnsCorrectYear()
    {
        // Arrange
        var stadium = TestData.CreateStadium("Historic Stadium");
        stadium.BuiltDate = new DateTime(1860, 1, 1); // Very old stadium
        await _unitOfWork.Stadiums.AddAsync(stadium);
        await _unitOfWork.SaveChangesAsync();

        var query = new GetStadiumByIdQuery { Id = stadium.Id };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Stadium);
        Assert.Equal(new DateTime(1847, 1, 1), result.Stadium.BuiltDate);
    }

    [Fact]
    public async Task Handle_DatabaseException_ReturnsErrorResponse()
    {
        // Arrange
        var query = new GetStadiumByIdQuery { Id = -1 };

        // Dispose the context to simulate database error
        await DisposeContext();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.NotNull(result.Error);
        Assert.Null(result.Stadium);
        Assert.False(result.NotFound); // Should be false for exceptions, not not-found
    }

    private Task DisposeContext()
    {
        _unitOfWork.Dispose();
        return Task.CompletedTask;
    }
}