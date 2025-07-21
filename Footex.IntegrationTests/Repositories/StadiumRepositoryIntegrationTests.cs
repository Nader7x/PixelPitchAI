using Domain.Interfaces;
using Domain.Models;
using Domain.Repositories;
using FluentAssertions;
using Footex.IntegrationTests.Common;
using Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Footex.IntegrationTests.Repositories;

public class StadiumRepositoryIntegrationTests : BaseIntegrationTest
{
    private readonly IStadiumsRepository _stadiumRepository;

    private readonly FootexWebApplicationFactory _factory;

    public StadiumRepositoryIntegrationTests(FootexWebApplicationFactory factory)
        : base(factory)
    {
        _factory = factory;
        _stadiumRepository =
            FactoryServiceScope.ServiceProvider.GetRequiredService<IStadiumsRepository>();
        FreeDbAsync(Context.Stadiums).Wait();
    }

    [Fact]
    public async Task AddAsync_WithValidStadium_SavesSuccessfully()
    {
        // Arrange
        var stadium = new Stadium
        {
            Name = "Test Stadium",
            Capacity = 50000,
            City = "Test City",
            Country = "Test Country",
            BuiltDate = new DateTime(1990, 1, 1),
            Description = "A test stadium for integration testing",
        };

        // Act
        await _stadiumRepository.AddAsync(stadium);
        await UnitOfWork.SaveChangesAsync();

        // Assert
        stadium.Id.Should().BeGreaterThan(0);

        var retrievedStadium = await _stadiumRepository.GetByIdAsync(stadium.Id);
        retrievedStadium.Should().NotBeNull();
        retrievedStadium!.Name.Should().Be("Test Stadium");
        retrievedStadium.Capacity.Should().Be(50000);
        retrievedStadium.City.Should().Be("Test City");
        retrievedStadium.Country.Should().Be("Test Country");
        retrievedStadium.BuiltDate.Should().Be(new DateTime(1990, 1, 1));
        retrievedStadium.Description.Should().Be("A test stadium for integration testing");
    }

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsStadium()
    {
        // Arrange
        var stadium = new Stadium
        {
            Name = "Retrieved Stadium",
            Capacity = 75000,
            City = "Stadium City",
            Country = "Stadium Country",
        };

        await _stadiumRepository.AddAsync(stadium);
        await UnitOfWork.SaveChangesAsync();

        // Act
        var result = await _stadiumRepository.GetByIdAsync(stadium.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(stadium.Id);
        result.Name.Should().Be("Retrieved Stadium");
        result.Capacity.Should().Be(75000);
        result.City.Should().Be("Stadium City");
        result.Country.Should().Be("Stadium Country");
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentId_ReturnsNull()
    {
        // Act
        var result = await _stadiumRepository.GetByIdAsync(999999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_WithMultipleStadiums_ReturnsAllStadiums()
    {
        // Arrange
        var stadium1 = new Stadium
        {
            Name = "Stadium One",
            Capacity = 40000,
            City = "City One",
            Country = "Country One",
        };

        var stadium2 = new Stadium
        {
            Name = "Stadium Two",
            Capacity = 60000,
            City = "City Two",
            Country = "Country Two",
        };

        var stadium3 = new Stadium
        {
            Name = "Stadium Three",
            Capacity = 80000,
            City = "City Three",
            Country = "Country Three",
        };

        await _stadiumRepository.AddAsync(stadium1);
        await _stadiumRepository.AddAsync(stadium2);
        await _stadiumRepository.AddAsync(stadium3);
        await UnitOfWork.SaveChangesAsync();

        // Act
        var allStadiums = await _stadiumRepository.GetAllAsync();

        // Assert
        allStadiums.Should().HaveCountGreaterOrEqualTo(3);
        allStadiums.Should().Contain(s => s.Name == "Stadium One");
        allStadiums.Should().Contain(s => s.Name == "Stadium Two");
        allStadiums.Should().Contain(s => s.Name == "Stadium Three");
    }

    [Fact]
    public async Task GetAllAsync_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var stadiums = new List<Stadium>();
        for (var i = 1; i <= 10; i++)
            stadiums.Add(
                new Stadium
                {
                    Name = $"Stadium {i:D2}",
                    Capacity = 30000 + i * 1000,
                    City = $"City {i}",
                    Country = "Test Country",
                }
            );

        foreach (var stadium in stadiums)
            await _stadiumRepository.AddAsync(stadium);
        await UnitOfWork.SaveChangesAsync();

        // Act
        var firstPage = await _stadiumRepository.GetAllAsync(1, 3);
        var secondPage = await _stadiumRepository.GetAllAsync(2, 3);

        // Assert
        firstPage.Should().HaveCount(3);
        secondPage.Should().HaveCount(3);
    }

    [Fact]
    public async Task UpdateAsync_WithValidStadium_UpdatesSuccessfully()
    {
        // Arrange
        var stadium = new Stadium
        {
            Name = "Original Stadium",
            Capacity = 45000,
            City = "Original City",
            Country = "Original Country",
            BuiltDate = new DateTime(1985, 1, 1),
        };

        await _stadiumRepository.AddAsync(stadium);
        await UnitOfWork.SaveChangesAsync();

        // Act
        stadium.Name = "Updated Stadium";
        stadium.Capacity = 55000;
        stadium.City = "Updated City";
        stadium.BuiltDate = new DateTime(1995, 1, 1);
        stadium.Description = "Updated description";

        _stadiumRepository.Update(stadium);
        await UnitOfWork.SaveChangesAsync();

        // Assert
        var updatedStadium = await _stadiumRepository.GetByIdAsync(stadium.Id);
        updatedStadium.Should().NotBeNull();
        updatedStadium!.Name.Should().Be("Updated Stadium");
        updatedStadium.Capacity.Should().Be(55000);
        updatedStadium.City.Should().Be("Updated City");
        updatedStadium.Country.Should().Be("Original Country"); // Should remain unchanged
        updatedStadium.BuiltDate.Should().Be(new DateTime(1995, 1, 1));
        updatedStadium.Description.Should().Be("Updated description");
    }

    [Fact]
    public async Task DeleteAsync_WithValidStadium_RemovesSuccessfully()
    {
        // Arrange
        var stadium = new Stadium
        {
            Name = "Stadium To Delete",
            Capacity = 35000,
            City = "Delete City",
            Country = "Delete Country",
        };

        await _stadiumRepository.AddAsync(stadium);
        await UnitOfWork.SaveChangesAsync();

        var stadiumId = stadium.Id;

        // Act
        _stadiumRepository.Delete(stadium);
        await UnitOfWork.SaveChangesAsync();

        // Assert
        var deletedStadium = await _stadiumRepository.GetByIdAsync(stadiumId);
        deletedStadium.Should().BeNull();
    }

    [Fact]
    public async Task FindAsync_WithPredicate_ReturnsMatchingStadiums()
    {
        // Arrange
        var largeStadium1 = new Stadium
        {
            Name = "Large Stadium 1",
            Capacity = 70000,
            City = "Big City 1",
            Country = "Test Country",
        };

        var largeStadium2 = new Stadium
        {
            Name = "Large Stadium 2",
            Capacity = 80000,
            City = "Big City 2",
            Country = "Test Country",
        };

        var smallStadium = new Stadium
        {
            Name = "Small Stadium",
            Capacity = 25000,
            City = "Small City",
            Country = "Test Country",
        };

        await _stadiumRepository.AddAsync(largeStadium1);
        await _stadiumRepository.AddAsync(largeStadium2);
        await _stadiumRepository.AddAsync(smallStadium);
        await UnitOfWork.SaveChangesAsync();

        // Act
        var largeStadiums = await _stadiumRepository.GetAsync(s => s.Capacity >= 60000);

        // Assert
        largeStadiums.Should().HaveCount(2);
        largeStadiums.Should().Contain(s => s.Name == "Large Stadium 1");
        largeStadiums.Should().Contain(s => s.Name == "Large Stadium 2");
        largeStadiums.Should().NotContain(s => s.Name == "Small Stadium");
    }

    [Fact]
    public async Task CountAsync_WithPredicate_ReturnsCorrectCount()
    {
        // Arrange
        var stadium1 = new Stadium
        {
            Name = "UK Stadium 1",
            Capacity = 50000,
            City = "London",
            Country = "United Kingdom",
        };

        var stadium2 = new Stadium
        {
            Name = "UK Stadium 2",
            Capacity = 60000,
            City = "Manchester",
            Country = "United Kingdom",
        };

        var stadium3 = new Stadium
        {
            Name = "US Stadium",
            Capacity = 70000,
            City = "New York",
            Country = "United States",
        };

        await _stadiumRepository.AddAsync(stadium1);
        await _stadiumRepository.AddAsync(stadium2);
        await _stadiumRepository.AddAsync(stadium3);
        await UnitOfWork.SaveChangesAsync();

        // Act
        var ukStadiumsCount = await _stadiumRepository.CountAsync(s =>
            s.Country == "United Kingdom"
        );
        var largeStadiumsCount = await _stadiumRepository.CountAsync(s => s.Capacity >= 60000);

        // Assert
        ukStadiumsCount.Should().Be(2);
        largeStadiumsCount.Should().Be(2);
    }

    [Fact]
    public async Task Repository_WithTransactions_HandlesCommitCorrectly()
    {
        // Arrange
        var stadium = new Stadium
        {
            Name = "Transaction Stadium",
            Capacity = 45000,
            City = "Transaction City",
            Country = "Transaction Country",
        };

        // Act
        await UnitOfWork.BeginTransactionAsync();

        await _stadiumRepository.AddAsync(stadium);
        await UnitOfWork.SaveChangesAsync();

        await UnitOfWork.CommitTransactionAsync();

        // Assert
        var committedStadium = await _stadiumRepository.GetByIdAsync(stadium.Id);
        committedStadium.Should().NotBeNull();
        committedStadium!.Name.Should().Be("Transaction Stadium");
        committedStadium.Capacity.Should().Be(45000);
    }

    [Fact]
    public async Task Repository_WithTransactions_HandlesRollbackCorrectly()
    {
        // Arrange
        // The factory should be available if you are using IClassFixture
        // Ensure you have a private field to hold it from the constructor.
        // private readonly FootexWebApplicationFactory _factory;

        var stadium = new Stadium
        {
            Name = "Rollback Stadium",
            Capacity = 55000,
            City = "Rollback City",
            Country = "Rollback Country",
        };

        // Act
        // Use a using statement for the scope to ensure it's disposed
        using (var scope = _factory.Services.CreateScope())
        {
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var stadiumRepository = scope.ServiceProvider.GetRequiredService<IStadiumsRepository>();

            await unitOfWork.BeginTransactionAsync();

            await stadiumRepository.AddAsync(stadium);
            await unitOfWork.SaveChangesAsync();

            // Verify stadium exists within transaction
            var stadiumInTransaction = await stadiumRepository.GetByIdAsync(stadium.Id);
            stadiumInTransaction.Should().NotBeNull();

            // Rollback
            await unitOfWork.RollbackTransactionAsync();
        }

        // Assert
        // Create a NEW and SEPARATE scope and DbContext to verify the rollback.
        // This ensures the check is against the database, not a cached entity.
        using (var assertScope = _factory.Services.CreateScope())
        {
            var context = assertScope.ServiceProvider.GetRequiredService<FootballDbContext>();
            var stadiumAfterRollback = await context.Stadiums.FindAsync(stadium.Id);
            stadiumAfterRollback.Should().BeNull();
        }
    }

    [Fact]
    public async Task GetAllAsync_WithPredicateFilter_ReturnsFilteredResults()
    {
        // Arrange
        var modernStadium1 = new Stadium
        {
            Name = "Modern Stadium 1",
            Capacity = 60000,
            City = "Modern City 1",
            Country = "Test Country",
            BuiltDate = new DateTime(2010, 1, 1),
        };

        var modernStadium2 = new Stadium
        {
            Name = "Modern Stadium 2",
            Capacity = 70000,
            City = "Modern City 2",
            Country = "Test Country",
            BuiltDate = new DateTime(2015, 1, 1),
        };

        var oldStadium = new Stadium
        {
            Name = "Old Stadium",
            Capacity = 40000,
            City = "Old City",
            Country = "Test Country",
            BuiltDate = new DateTime(1980, 1, 1),
        };

        await _stadiumRepository.AddAsync(modernStadium1);
        await _stadiumRepository.AddAsync(modernStadium2);
        await _stadiumRepository.AddAsync(oldStadium);
        await UnitOfWork.SaveChangesAsync();

        // Act
        var modernStadiums = await _stadiumRepository.GetAllAsync(s =>
            s.BuiltDate >= new DateTime(2000, 1, 1)
        );

        // Assert
        var filteredStadiums = modernStadiums as Stadium[] ?? modernStadiums.ToArray();
        filteredStadiums.Should().HaveCount(2);
        filteredStadiums.Should().Contain(s => s.Name == "Modern Stadium 1");
        filteredStadiums.Should().Contain(s => s.Name == "Modern Stadium 2");
        filteredStadiums.Should().NotContain(s => s.Name == "Old Stadium");
    }

    [Fact]
    public async Task Stadium_WithOptionalFields_HandlesNullValues()
    {
        // Arrange
        var stadiumWithMinimalData = new Stadium
        {
            Name = "Minimal Stadium",
            Capacity = 30000,
            City = "Minimal City",
            Country = "Minimal Country",
            // BuiltDate and Description are optional and left null
        };

        // Act
        await _stadiumRepository.AddAsync(stadiumWithMinimalData);
        await UnitOfWork.SaveChangesAsync();

        // Assert
        var retrievedStadium = await _stadiumRepository.GetByIdAsync(stadiumWithMinimalData.Id);
        retrievedStadium.Should().NotBeNull();
        retrievedStadium!.Name.Should().Be("Minimal Stadium");
        retrievedStadium.Capacity.Should().Be(30000);
        retrievedStadium.City.Should().Be("Minimal City");
        retrievedStadium.Country.Should().Be("Minimal Country");
        retrievedStadium.BuiltDate.Should().BeNull();
        retrievedStadium.Description.Should().BeNull();
    }

    [Fact]
    public async Task Stadium_WithCompleteData_SavesAllFields()
    {
        // Arrange
        var completeStadium = new Stadium
        {
            Name = "Complete Stadium",
            Capacity = 85000,
            City = "Complete City",
            Country = "Complete Country",
            BuiltDate = new DateTime(2020, 1, 1),
            Description = "A complete stadium with all fields filled",
        };

        // Act
        await _stadiumRepository.AddAsync(completeStadium);
        await UnitOfWork.SaveChangesAsync();

        // Assert
        var retrievedStadium = await _stadiumRepository.GetByIdAsync(completeStadium.Id);
        retrievedStadium.Should().NotBeNull();
        retrievedStadium!.Name.Should().Be("Complete Stadium");
        retrievedStadium.Capacity.Should().Be(85000);
        retrievedStadium.City.Should().Be("Complete City");
        retrievedStadium.Country.Should().Be("Complete Country");
        retrievedStadium.BuiltDate.Should().Be(new DateTime(2020, 1, 1));
        retrievedStadium.Description.Should().Be("A complete stadium with all fields filled");
    }

    [Fact]
    public async Task FindAsync_WithComplexPredicate_ReturnsCorrectResults()
    {
        // Arrange
        var stadium1 = new Stadium
        {
            Name = "Premium Stadium",
            Capacity = 80000,
            City = "Premium City",
            Country = "Premium Country",
            BuiltDate = new DateTime(2018, 1, 1),
        };

        var stadium2 = new Stadium
        {
            Name = "Standard Stadium",
            Capacity = 45000,
            City = "Standard City",
            Country = "Standard Country",
            BuiltDate = new DateTime(2010, 1, 1),
        };

        var stadium3 = new Stadium
        {
            Name = "Legacy Stadium",
            Capacity = 35000,
            City = "Legacy City",
            Country = "Legacy Country",
            BuiltDate = new DateTime(1995, 1, 1),
        };

        await _stadiumRepository.AddAsync(stadium1);
        await _stadiumRepository.AddAsync(stadium2);
        await _stadiumRepository.AddAsync(stadium3);
        await UnitOfWork.SaveChangesAsync();

        // Act - Find stadiums with capacity >= 40000 AND built after 2005
        var premiumModernStadiums = await _stadiumRepository.GetAsync(s =>
            s.Capacity >= 40000 && s.BuiltDate >= new DateTime(2005, 1, 1)
        );

        // Assert
        premiumModernStadiums.Should().HaveCount(2);
        premiumModernStadiums.Should().Contain(s => s.Name == "Premium Stadium");
        premiumModernStadiums.Should().Contain(s => s.Name == "Standard Stadium");
        premiumModernStadiums.Should().NotContain(s => s.Name == "Legacy Stadium");
    }
}
