using Application.CQRS.Stadiums.Commands;
using Domain.Interfaces;
using Footex.IntegrationTests.Common;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Footex.IntegrationTests.CQRS.Stadiums.Commands;

public class CreateStadiumCommandHandlerIntegrationTests : BaseIntegrationTest
{
    private readonly CreateStadiumCommandHandler _handler;
    private readonly IUnitOfWork _unitOfWork;

    public CreateStadiumCommandHandlerIntegrationTests(FootexWebApplicationFactory factory)
        : base(factory)
    {
        _handler = ServiceProvider.GetRequiredService<CreateStadiumCommandHandler>();
        _unitOfWork = ServiceProvider.GetRequiredService<IUnitOfWork>();
    }

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
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotEqual(-1, result.Id);
        Assert.Equal(command.Name, result.Name);
        Assert.Null(result.Error);

        // Verify stadium was saved to database
        var savedStadium = await _unitOfWork.Stadiums.GetByIdAsync(result.Id);
        Assert.NotNull(savedStadium);
        Assert.Equal(command.Name, savedStadium.Name);
        Assert.Equal(command.City, savedStadium.City);
        Assert.Equal(command.Capacity, savedStadium.Capacity);
        Assert.Equal(command.BuiltDate, savedStadium.BuiltDate);
        Assert.Equal(command.Description, savedStadium.Description);
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
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotEqual(-1, result.Id);
        Assert.Equal(command.Name, result.Name);

        var savedStadium = await _unitOfWork.Stadiums.GetByIdAsync(result.Id);
        Assert.NotNull(savedStadium);
        Assert.Equal(command.Name, savedStadium.Name);
        Assert.Equal(command.City, savedStadium.City);
        Assert.Equal(command.Capacity, savedStadium.Capacity);
        Assert.Null(savedStadium.BuiltDate);
        Assert.Null(savedStadium.Description);
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
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotEqual(-1, result.Id);

        var savedStadium = await _unitOfWork.Stadiums.GetByIdAsync(result.Id);
        Assert.NotNull(savedStadium);
        Assert.Equal(command.ImageUrl, savedStadium.ImageUrl);
        Assert.Equal(command.SurfaceType, savedStadium.SurfaceType);
    }

    [Fact]
    public async Task Handle_DuplicateStadiumName_HandlesGracefully()
    {
        // Arrange
        var stadium = TestData.CreateStadium("Wembley Stadium");
        await _unitOfWork.Stadiums.AddAsync(stadium);
        await _unitOfWork.SaveChangesAsync();

        var command = new CreateStadiumCommand
        {
            Name = "Wembley Stadium",
            City = "London, England",
            Capacity = 90000,
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        // The implementation should handle this scenario
        // Either allow duplicate names or return appropriate error
        if (result.Succeeded)
        {
            Assert.NotEqual(-1, result.Id);
            Assert.NotEqual(stadium.Id, result.Id);
        }
        else
        {
            Assert.NotNull(result.Error);
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
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotEqual(-1, result.Id);

        var savedStadium = await _unitOfWork.Stadiums.GetByIdAsync(result.Id);
        Assert.NotNull(savedStadium);
        Assert.Equal(114000, savedStadium.Capacity);
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
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);

        var savedStadium = await _unitOfWork.Stadiums.GetByIdAsync(result.Id);
        Assert.NotNull(savedStadium);
        Assert.Equal(new DateTime(1888, 1, 1), savedStadium.BuiltDate);
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
            var result = await _handler.Handle(command, CancellationToken.None);
            results.Add(result);
        }

        // Assert
        Assert.All(results, r => Assert.True(r.Succeeded));
        Assert.Equal(3, results.Count);

        // Verify all have unique IDs
        var ids = results.Select(r => r.Id).ToList();
        Assert.Equal(3, ids.Distinct().Count());

        // Verify all are saved in database
        foreach (var result in results)
        {
            var savedStadium = await _unitOfWork.Stadiums.GetByIdAsync(result.Id);
            Assert.NotNull(savedStadium);
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
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.NotNull(result.Error);
        Assert.Equal(-1, result.Id);
    }

    private Task DisposeContext()
    {
        _unitOfWork.Dispose();
        return Task.CompletedTask;
    }
}
