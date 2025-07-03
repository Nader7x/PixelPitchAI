using Domain.Models;
using Domain.Repositories;
using FluentAssertions;
using Footex.IntegrationTests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Footex.IntegrationTests.Repositories;

public class CompetitionRepositoryIntegrationTests : BaseIntegrationTest
{
    private readonly ICompetitionRepository _repository;

    public CompetitionRepositoryIntegrationTests(FootexWebApplicationFactory factory)
        : base(factory)
    {
        _repository = ServiceProvider.GetRequiredService<ICompetitionRepository>();
    }

    [Fact]
    public async Task AddAsync_ShouldAddCompetitionSuccessfully()
    {
        // Arrange
        var competition = CreateValidCompetition();

        // Act
        var result = await _repository.AddAsync(competition);

        // Assert
        result.Should().NotBeNull();
        result.Entity.Id.Should().BeGreaterThan(0);
        result.Entity.Name.Should().Be(competition.Name);
        result.Entity.Description.Should().Be(competition.Description);
        result.Entity.Country.Should().Be(competition.Country);
        result.Entity.Logo.Should().Be(competition.Logo);
    }

    [Fact]
    public async Task GetByIdAsync_WhenCompetitionExists_ShouldReturnCompetition()
    {
        // Arrange
        var competition = await SeedCompetitionAsync();

        // Act
        var result = await _repository.GetByIdAsync(competition.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(competition.Id);
        result.Name.Should().Be(competition.Name);
        result.Description.Should().Be(competition.Description);
        result.Country.Should().Be(competition.Country);
        result.Logo.Should().Be(competition.Logo);
    }

    [Fact]
    public async Task GetByIdAsync_WhenCompetitionDoesNotExist_ShouldReturnNull()
    {
        // Act
        var result = await _repository.GetByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateCompetitionSuccessfully()
    {
        // Arrange
        var competition = await SeedCompetitionAsync();
        var updatedName = "Updated Competition Name";
        var updatedDescription = "Updated Description";
        var updatedCountry = "Updated Country";

        // Act
        competition.Name = updatedName;
        competition.Description = updatedDescription;
        competition.Country = updatedCountry;
        var result = _repository.UpdateAsync(competition);
        await Context.SaveChangesAsync();

        // Assert
        result.Should().NotBeNull();
        result.Entity.Name.Should().Be(updatedName);
        result.Entity.Description.Should().Be(updatedDescription);
        result.Entity.Country.Should().Be(updatedCountry);

        // Verify changes are persisted
        var persistedCompetition = await _repository.GetByIdAsync(competition.Id);
        persistedCompetition!.Name.Should().Be(updatedName);
        persistedCompetition.Description.Should().Be(updatedDescription);
        persistedCompetition.Country.Should().Be(updatedCountry);
    }

    [Fact]
    public async Task DeleteAsync_ShouldDeleteCompetitionSuccessfully()
    {
        // Arrange
        var competition = await SeedCompetitionAsync();

        // Act
        _repository.DeleteAsync(competition);
        await Context.SaveChangesAsync();

        // Assert
        var deletedCompetition = await _repository.GetByIdAsync(competition.Id);
        deletedCompetition.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllCompetitions()
    {
        // Arrange
        var competition1 = await SeedCompetitionAsync("Competition 1");
        var competition2 = await SeedCompetitionAsync("Competition 2");

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCountGreaterOrEqualTo(2);
        result.Should().Contain(c => c.Id == competition1.Id);
        result.Should().Contain(c => c.Id == competition2.Id);
    }

    [Fact]
    public async Task GetAllAsync_WhenNoCompetitions_ShouldReturnEmptyList()
    {
        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_WithPagination_ShouldReturnPagedResults()
    {
        // Arrange
        await SeedCompetitionAsync("Competition 1");
        await SeedCompetitionAsync("Competition 2");
        await SeedCompetitionAsync("Competition 3");

        // Act
        var result = await _repository.GetAllAsync(1, 2);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task FindAsync_WithPredicate_ShouldReturnMatchingCompetition()
    {
        // Arrange
        var competition = await SeedCompetitionAsync("Unique Competition Name");

        // Act
        var result = await _repository.FindAsync(c => c.Name == "Unique Competition Name");

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(competition.Id);
        result.Name.Should().Be("Unique Competition Name");
    }

    [Fact]
    public async Task FindAsync_WithPredicate_WhenNoMatch_ShouldReturnNull()
    {
        // Act
        var result = await _repository.FindAsync(c => c.Name == "Non-existent Competition");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task FindAsync_WithEntity_ShouldReturnExistingEntity()
    {
        // Arrange
        var competition = await SeedCompetitionAsync();

        // Act
        var result = await _repository.FindAsync(competition);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(competition.Id);
    }

    [Fact]
    public async Task Competition_ShouldHandleEmptyOrNullFields()
    {
        // Arrange
        var competition = new Competition
        {
            Name = "Required Competition Name",
            Description = null,
            Country = "",
            Logo = null,
        };

        // Act
        var result = await _repository.AddAsync(competition);
        await Context.SaveChangesAsync();

        // Assert
        result.Should().NotBeNull();
        result.Entity.Name.Should().Be("Required Competition Name");
        result.Entity.Description.Should().BeNull();
        result.Entity.Country.Should().Be("");
        result.Entity.Logo.Should().BeNull();
    }

    [Fact]
    public async Task Competition_ShouldSupportLongDescriptions()
    {
        // Arrange
        var longDescription = new string('A', 1000); // 1000 character description
        var competition = new Competition
        {
            Name = "Competition with Long Description",
            Description = longDescription,
            Country = "Test Country",
            Logo = "http://example.com/logo.png",
        };

        // Act
        var result = await _repository.AddAsync(competition);
        await Context.SaveChangesAsync();

        // Assert
        result.Should().NotBeNull();
        result.Entity.Description.Should().Be(longDescription);
        result.Entity.Description!.Length.Should().Be(1000);
    }

    [Fact]
    public async Task Competition_ShouldHandleSpecialCharactersInName()
    {
        // Arrange
        var specialName = "Ñoël's Competition & Tëam #1 (2024)";
        var competition = new Competition
        {
            Name = specialName,
            Description = "Competition with special characters",
            Country = "España",
            Logo = "http://example.com/logo.png",
        };

        // Act
        var result = await _repository.AddAsync(competition);
        await Context.SaveChangesAsync();

        // Assert
        result.Should().NotBeNull();
        result.Entity.Name.Should().Be(specialName);
        result.Entity.Country.Should().Be("España");
    }

    [Fact]
    public async Task Competition_WithSeasons_ShouldMaintainRelationship()
    {
        // Arrange
        var competition = await SeedCompetitionAsync();
        var season = await SeedSeasonAsync(competition.Id);

        // Act
        var result = await Context
            .Competitions.Where(c => c.Id == competition.Id)
            .Select(c => new
            {
                c.Id,
                c.Name,
                SeasonCount = c.Seasons!.Count(),
            })
            .FirstOrDefaultAsync();

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(competition.Id);
        result.SeasonCount.Should().BeGreaterThan(0);
    }

    // Helper methods for seeding test data
    private async Task<Competition> SeedCompetitionAsync(string name = "Test Competition")
    {
        var competition = CreateValidCompetition(name);
        var result = await _repository.AddAsync(competition);
        await Context.SaveChangesAsync();

        return result.Entity;
    }

    private Competition CreateValidCompetition(string name = "Test Competition")
    {
        return new Competition
        {
            Name = name,
            Description = $"Description for {name}",
            Country = "Test Country",
            Logo = "http://example.com/logo.png",
        };
    }

    private async Task<Season> SeedSeasonAsync(int competitionId)
    {
        var season = new Season
        {
            Name = "Test Season 2024",
            LeagueName = "Test League",
            Country = "Test Country",
            StartDate = DateTime.UtcNow.AddMonths(-3),
            EndDate = DateTime.UtcNow.AddMonths(6),
            IsActive = true,
            TotalRounds = 38,
            CurrentRound = 1,
            CompetitionId = competitionId,
        };

        Context.Seasons.Add(season);
        await Context.SaveChangesAsync();

        return season;
    }
}
