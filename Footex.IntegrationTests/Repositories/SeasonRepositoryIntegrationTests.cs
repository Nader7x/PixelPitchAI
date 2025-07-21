using Domain.Models;
using Domain.Repositories;
using FluentAssertions;
using Footex.IntegrationTests.Common;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Footex.IntegrationTests.Repositories;

public class SeasonRepositoryIntegrationTests : BaseIntegrationTest
{
    private readonly ISeasonRepository _seasonRepository;

    public SeasonRepositoryIntegrationTests(FootexWebApplicationFactory factory)
        : base(factory)
    {
        _seasonRepository =
            FactoryServiceScope.ServiceProvider.GetRequiredService<ISeasonRepository>();
        FreeDbAsync(Context.Seasons).Wait();
    }

    [Fact]
    public async Task GetByNameAsync_WithValidName_ReturnsSeason()
    {
        // Arrange
        var season = new Season
        {
            Name = "Premier League 2023-24",
            Country = "England",
            LeagueName = "Premier League",
            StartDate = new DateTime(2023, 8, 12),
            EndDate = new DateTime(2024, 5, 19),
            IsActive = true,
            CurrentRound = 15,
        };

        await _seasonRepository.AddAsync(season);
        await UnitOfWork.SaveChangesAsync();

        // Act
        var result = await _seasonRepository.GetByNameAsync("Premier League 2023-24");

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Premier League 2023-24");
        result.Country.Should().Be("England");
        result.LeagueName.Should().Be("Premier League");
        result.IsActive.Should().BeTrue();
        result.CurrentRound.Should().Be(15);
    }

    [Fact]
    public async Task GetByNameAsync_WithNonExistentName_ReturnsNull()
    {
        // Act
        var result = await _seasonRepository.GetByNameAsync("Non Existent Season");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByNameAsync_WithCancellationToken_ReturnsCorrectSeason()
    {
        // Arrange
        var season = new Season
        {
            Name = "La Liga 2023-24",
            Country = "Spain",
            LeagueName = "La Liga",
            StartDate = new DateTime(2023, 8, 18),
            EndDate = new DateTime(2024, 5, 25),
            IsActive = true,
            CurrentRound = 12,
        };

        await _seasonRepository.AddAsync(season);
        await UnitOfWork.SaveChangesAsync();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        // Act
        var result = await _seasonRepository.GetByNameAsync("La Liga 2023-24", cts.Token);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("La Liga 2023-24");
        result.Country.Should().Be("Spain");
        result.LeagueName.Should().Be("La Liga");
    }

    [Fact]
    public async Task GetActiveSeasons_WithMultipleSeasons_ReturnsOnlyActiveSeasons()
    {
        // Arrange
        var activeSeason1 = new Season
        {
            Name = "Active Season 1",
            Country = "England",
            LeagueName = "Premier League",
            StartDate = new DateTime(2023, 8, 12),
            EndDate = new DateTime(2024, 5, 19),
            IsActive = true,
            CurrentRound = 20,
        };

        var activeSeason2 = new Season
        {
            Name = "Active Season 2",
            Country = "Spain",
            LeagueName = "La Liga",
            StartDate = new DateTime(2023, 8, 18),
            EndDate = new DateTime(2024, 5, 25),
            IsActive = true,
            CurrentRound = 18,
        };

        var inactiveSeason = new Season
        {
            Name = "Inactive Season",
            Country = "Italy",
            LeagueName = "Serie A",
            StartDate = new DateTime(2022, 8, 13),
            EndDate = new DateTime(2023, 6, 4),
            IsActive = false,
            CurrentRound = 38,
        };

        await _seasonRepository.AddAsync(activeSeason1);
        await _seasonRepository.AddAsync(activeSeason2);
        await _seasonRepository.AddAsync(inactiveSeason);
        await UnitOfWork.SaveChangesAsync();

        // Act
        var activeSeasons = await _seasonRepository.GetActiveSeasons();

        // Assert
        activeSeasons.Should().HaveCount(2);
        activeSeasons.Should().Contain(s => s.Name == "Active Season 1");
        activeSeasons.Should().Contain(s => s.Name == "Active Season 2");
        activeSeasons.Should().NotContain(s => s.Name == "Inactive Season");
        activeSeasons.All(s => s.IsActive).Should().BeTrue();
    }

    [Fact]
    public async Task GetSeasonsByCountry_WithValidCountry_ReturnsMatchingSeasons()
    {
        // Arrange
        var englishSeason1 = new Season
        {
            Name = "Premier League 2023-24",
            Country = "England",
            LeagueName = "Premier League",
            StartDate = new DateTime(2023, 8, 12),
            EndDate = new DateTime(2024, 5, 19),
            IsActive = true,
            CurrentRound = 15,
        };

        var englishSeason2 = new Season
        {
            Name = "Championship 2023-24",
            Country = "England",
            LeagueName = "Championship",
            StartDate = new DateTime(2023, 8, 5),
            EndDate = new DateTime(2024, 5, 4),
            IsActive = true,
            CurrentRound = 25,
        };

        var spanishSeason = new Season
        {
            Name = "La Liga 2023-24",
            Country = "Spain",
            LeagueName = "La Liga",
            StartDate = new DateTime(2023, 8, 18),
            EndDate = new DateTime(2024, 5, 25),
            IsActive = true,
            CurrentRound = 12,
        };

        await _seasonRepository.AddAsync(englishSeason1);
        await _seasonRepository.AddAsync(englishSeason2);
        await _seasonRepository.AddAsync(spanishSeason);
        await UnitOfWork.SaveChangesAsync();

        // Act
        var englishSeasons = await _seasonRepository.GetSeasonsByCountry("England");

        // Assert
        englishSeasons.Should().HaveCount(2);
        englishSeasons.Should().Contain(s => s.Name == "Premier League 2023-24");
        englishSeasons.Should().Contain(s => s.Name == "Championship 2023-24");
        englishSeasons.Should().NotContain(s => s.Name == "La Liga 2023-24");
        englishSeasons.All(s => s.Country == "England").Should().BeTrue();
    }

    [Fact]
    public async Task GetSeasonsByLeagueName_WithValidLeague_ReturnsMatchingSeasons()
    {
        // Arrange
        var premierLeague2023 = new Season
        {
            Name = "Premier League 2023-24",
            Country = "England",
            LeagueName = "Premier League",
            StartDate = new DateTime(2023, 8, 12),
            EndDate = new DateTime(2024, 5, 19),
            IsActive = true,
            CurrentRound = 15,
        };

        var premierLeague2022 = new Season
        {
            Name = "Premier League 2022-23",
            Country = "England",
            LeagueName = "Premier League",
            StartDate = new DateTime(2022, 8, 6),
            EndDate = new DateTime(2023, 5, 28),
            IsActive = false,
            CurrentRound = 38,
        };

        var laLiga = new Season
        {
            Name = "La Liga 2023-24",
            Country = "Spain",
            LeagueName = "La Liga",
            StartDate = new DateTime(2023, 8, 18),
            EndDate = new DateTime(2024, 5, 25),
            IsActive = true,
            CurrentRound = 12,
        };

        await _seasonRepository.AddAsync(premierLeague2023);
        await _seasonRepository.AddAsync(premierLeague2022);
        await _seasonRepository.AddAsync(laLiga);
        await UnitOfWork.SaveChangesAsync();

        // Act
        var premierLeagueSeasons = await _seasonRepository.GetSeasonsByLeagueName("Premier League");

        // Assert
        premierLeagueSeasons.Should().HaveCount(2);
        premierLeagueSeasons.Should().Contain(s => s.Name == "Premier League 2023-24");
        premierLeagueSeasons.Should().Contain(s => s.Name == "Premier League 2022-23");
        premierLeagueSeasons.Should().NotContain(s => s.Name == "La Liga 2023-24");
        premierLeagueSeasons.All(s => s.LeagueName == "Premier League").Should().BeTrue();
    }

    [Fact]
    public async Task GetSeasonMatches_WithValidSeasonIds_ReturnsSeasonData()
    {
        // Arrange
        var season1 = new Season
        {
            Name = "Season 1",
            Country = "England",
            LeagueName = "Premier League",
            StartDate = new DateTime(2023, 8, 12),
            EndDate = new DateTime(2024, 5, 19),
            IsActive = true,
            CurrentRound = 15,
        };

        var season2 = new Season
        {
            Name = "Season 2",
            Country = "Spain",
            LeagueName = "La Liga",
            StartDate = new DateTime(2023, 8, 18),
            EndDate = new DateTime(2024, 5, 25),
            IsActive = true,
            CurrentRound = 12,
        };

        await _seasonRepository.AddAsync(season1);
        await _seasonRepository.AddAsync(season2);
        await UnitOfWork.SaveChangesAsync();

        // Act
        var seasonMatches = await _seasonRepository.GetSeasonMatches(season1.Id, season2.Id);

        // Assert
        seasonMatches.Should().HaveCount(2);
        seasonMatches.Should().Contain(s => s.Id == season1.Id);
        seasonMatches.Should().Contain(s => s.Id == season2.Id);
    }

    [Fact]
    public async Task AddAsync_WithValidSeason_SavesSuccessfully()
    {
        // Arrange
        var season = new Season
        {
            Name = "New Season 2024-25",
            Country = "Germany",
            LeagueName = "Bundesliga",
            StartDate = new DateTime(2024, 8, 24),
            EndDate = new DateTime(2025, 5, 17),
            IsActive = true,
            CurrentRound = 1,
        };

        // Act
        await _seasonRepository.AddAsync(season);
        await UnitOfWork.SaveChangesAsync();

        // Assert
        season.Id.Should().BeGreaterThan(0);

        var retrievedSeason = await _seasonRepository.GetByIdAsync(season.Id);
        retrievedSeason.Should().NotBeNull();
        retrievedSeason!.Name.Should().Be("New Season 2024-25");
        retrievedSeason.Country.Should().Be("Germany");
        retrievedSeason.LeagueName.Should().Be("Bundesliga");
        retrievedSeason.StartDate.Should().Be(new DateTime(2024, 8, 24));
        retrievedSeason.EndDate.Should().Be(new DateTime(2025, 5, 17));
        retrievedSeason.IsActive.Should().BeTrue();
        retrievedSeason.CurrentRound.Should().Be(1);
    }

    [Fact]
    public async Task UpdateAsync_WithValidSeason_UpdatesSuccessfully()
    {
        // Arrange
        var season = new Season
        {
            Name = "Original Season",
            Country = "France",
            LeagueName = "Ligue 1",
            StartDate = new DateTime(2023, 8, 12),
            EndDate = new DateTime(2024, 5, 19),
            IsActive = true,
            CurrentRound = 10,
        };

        await _seasonRepository.AddAsync(season);
        await UnitOfWork.SaveChangesAsync();

        // Act
        season.Name = "Updated Season";
        season.CurrentRound = 25;
        season.IsActive = false;

        _seasonRepository.Update(season);
        await UnitOfWork.SaveChangesAsync();

        // Assert
        var updatedSeason = await _seasonRepository.GetByIdAsync(season.Id);
        updatedSeason.Should().NotBeNull();
        updatedSeason!.Name.Should().Be("Updated Season");
        updatedSeason.CurrentRound.Should().Be(25);
        updatedSeason.IsActive.Should().BeFalse();
        updatedSeason.Country.Should().Be("France"); // Should remain unchanged
        updatedSeason.LeagueName.Should().Be("Ligue 1"); // Should remain unchanged
    }

    [Fact]
    public async Task DeleteAsync_WithValidSeason_RemovesSuccessfully()
    {
        // Arrange
        var season = new Season
        {
            Name = "Season To Delete",
            Country = "Portugal",
            LeagueName = "Primeira Liga",
            StartDate = new DateTime(2023, 8, 12),
            EndDate = new DateTime(2024, 5, 19),
            IsActive = false,
            CurrentRound = 38,
        };

        await _seasonRepository.AddAsync(season);
        await UnitOfWork.SaveChangesAsync();

        var seasonId = season.Id;

        // Act
        _seasonRepository.Delete(season);
        await UnitOfWork.SaveChangesAsync();

        // Assert
        var deletedSeason = await _seasonRepository.GetByIdAsync(seasonId);
        deletedSeason.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_WithMultipleSeasons_ReturnsAllSeasons()
    {
        // Arrange
        var seasons = new List<Season>
        {
            new()
            {
                Name = "Season A",
                Country = "England",
                LeagueName = "Premier League",
                StartDate = new DateTime(2023, 8, 12),
                EndDate = new DateTime(2024, 5, 19),
                IsActive = true,
                CurrentRound = 15,
            },
            new()
            {
                Name = "Season B",
                Country = "Spain",
                LeagueName = "La Liga",
                StartDate = new DateTime(2023, 8, 18),
                EndDate = new DateTime(2024, 5, 25),
                IsActive = true,
                CurrentRound = 12,
            },
            new()
            {
                Name = "Season C",
                Country = "Italy",
                LeagueName = "Serie A",
                StartDate = new DateTime(2023, 8, 20),
                EndDate = new DateTime(2024, 5, 26),
                IsActive = false,
                CurrentRound = 38,
            },
        };

        foreach (var season in seasons)
            await _seasonRepository.AddAsync(season);
        await UnitOfWork.SaveChangesAsync();

        // Act
        var allSeasons = await _seasonRepository.GetAllAsync();

        // Assert
        allSeasons.Should().HaveCountGreaterOrEqualTo(3);
        allSeasons.Should().Contain(s => s.Name == "Season A");
        allSeasons.Should().Contain(s => s.Name == "Season B");
        allSeasons.Should().Contain(s => s.Name == "Season C");
    }

    [Fact]
    public async Task Repository_WithTransactions_HandlesRollbackCorrectly()
    {
        // Arrange
        var season = new Season
        {
            Name = "Transaction Test Season",
            Country = "Netherlands",
            LeagueName = "Eredivisie",
            StartDate = new DateTime(2023, 8, 12),
            EndDate = new DateTime(2024, 5, 19),
            IsActive = true,
            CurrentRound = 5,
        };

        // Act
        await UnitOfWork.BeginTransactionAsync();

        await _seasonRepository.AddAsync(season);
        await UnitOfWork.SaveChangesAsync();

        // Verify season exists within transaction
        var seasonInTransaction = await _seasonRepository.GetByNameAsync("Transaction Test Season");
        seasonInTransaction.Should().NotBeNull();

        // Rollback
        await UnitOfWork.RollbackTransactionAsync();

        // Assert
        var seasonAfterRollback = await _seasonRepository.GetByNameAsync("Transaction Test Season");
        seasonAfterRollback.Should().BeNull();
    }

    [Fact]
    public async Task GetSeasonsByCountry_WithCaseInsensitiveSearch_ReturnsResults()
    {
        // Arrange
        var season = new Season
        {
            Name = "Test Season",
            Country = "England",
            LeagueName = "Premier League",
            StartDate = new DateTime(2023, 8, 12),
            EndDate = new DateTime(2024, 5, 19),
            IsActive = true,
            CurrentRound = 15,
        };

        await _seasonRepository.AddAsync(season);
        await UnitOfWork.SaveChangesAsync();

        // Act
        var resultsLowerCase = await _seasonRepository.GetSeasonsByCountry("england");
        var resultsUpperCase = await _seasonRepository.GetSeasonsByCountry("ENGLAND");
        var resultsMixedCase = await _seasonRepository.GetSeasonsByCountry("EnGlAnD");

        // Assert
        resultsLowerCase.Should().HaveCount(1);
        resultsUpperCase.Should().HaveCount(1);
        resultsMixedCase.Should().HaveCount(1);

        resultsLowerCase.First().Name.Should().Be("Test Season");
        resultsUpperCase.First().Name.Should().Be("Test Season");
        resultsMixedCase.First().Name.Should().Be("Test Season");
    }

    [Fact]
    public async Task GetSeasonsByLeagueName_WithCaseInsensitiveSearch_ReturnsResults()
    {
        // Arrange
        var season = new Season
        {
            Name = "Test Season",
            Country = "Spain",
            LeagueName = "La Liga",
            StartDate = new DateTime(2023, 8, 18),
            EndDate = new DateTime(2024, 5, 25),
            IsActive = true,
            CurrentRound = 12,
        };

        await _seasonRepository.AddAsync(season);
        await UnitOfWork.SaveChangesAsync();

        // Act
        var resultsLowerCase = await _seasonRepository.GetSeasonsByLeagueName("la liga");
        var resultsUpperCase = await _seasonRepository.GetSeasonsByLeagueName("LA LIGA");
        var resultsMixedCase = await _seasonRepository.GetSeasonsByLeagueName("La LiGa");

        // Assert
        resultsLowerCase.Should().HaveCount(1);
        resultsUpperCase.Should().HaveCount(1);
        resultsMixedCase.Should().HaveCount(1);

        resultsLowerCase.First().Name.Should().Be("Test Season");
        resultsUpperCase.First().Name.Should().Be("Test Season");
        resultsMixedCase.First().Name.Should().Be("Test Season");
    }

    [Fact]
    public async Task GetActiveSeasons_OrderedByCurrentRound_ReturnsCorrectOrder()
    {
        // Arrange
        var season1 = new Season
        {
            Name = "Season with Round 5",
            Country = "England",
            LeagueName = "Premier League",
            StartDate = new DateTime(2023, 8, 12),
            EndDate = new DateTime(2024, 5, 19),
            IsActive = true,
            CurrentRound = 5,
        };

        var season2 = new Season
        {
            Name = "Season with Round 25",
            Country = "Spain",
            LeagueName = "La Liga",
            StartDate = new DateTime(2023, 8, 18),
            EndDate = new DateTime(2024, 5, 25),
            IsActive = true,
            CurrentRound = 25,
        };

        var season3 = new Season
        {
            Name = "Season with Round 15",
            Country = "Italy",
            LeagueName = "Serie A",
            StartDate = new DateTime(2023, 8, 20),
            EndDate = new DateTime(2024, 5, 26),
            IsActive = true,
            CurrentRound = 15,
        };

        await _seasonRepository.AddAsync(season1);
        await _seasonRepository.AddAsync(season2);
        await _seasonRepository.AddAsync(season3);
        await UnitOfWork.SaveChangesAsync();

        // Act
        var activeSeasons = await _seasonRepository.GetActiveSeasons();

        // Assert
        activeSeasons.Should().HaveCount(3);

        // Should be ordered by CurrentRound descending
        var seasonsList = activeSeasons.ToList();
        seasonsList[0].CurrentRound.Should().Be(25);
        seasonsList[1].CurrentRound.Should().Be(15);
        seasonsList[2].CurrentRound.Should().Be(5);

        seasonsList[0].Name.Should().Be("Season with Round 25");
        seasonsList[1].Name.Should().Be("Season with Round 15");
        seasonsList[2].Name.Should().Be("Season with Round 5");
    }

    [Fact]
    public async Task FindAsync_WithComplexPredicate_ReturnsCorrectResults()
    {
        // Arrange
        var currentSeason = new Season
        {
            Name = "Current Active Season",
            Country = "England",
            LeagueName = "Premier League",
            StartDate = new DateTime(2023, 8, 12),
            EndDate = new DateTime(2024, 5, 19),
            IsActive = true,
            CurrentRound = 20,
        };

        var futureSeason = new Season
        {
            Name = "Future Season",
            Country = "England",
            LeagueName = "Premier League",
            StartDate = new DateTime(2024, 8, 17),
            EndDate = new DateTime(2025, 5, 25),
            IsActive = false,
            CurrentRound = 0,
        };

        var pastSeason = new Season
        {
            Name = "Past Season",
            Country = "England",
            LeagueName = "Premier League",
            StartDate = new DateTime(2022, 8, 6),
            EndDate = new DateTime(2023, 5, 28),
            IsActive = false,
            CurrentRound = 38,
        };

        await _seasonRepository.AddAsync(currentSeason);
        await _seasonRepository.AddAsync(futureSeason);
        await _seasonRepository.AddAsync(pastSeason);
        await UnitOfWork.SaveChangesAsync();

        // Act - Find active seasons with CurrentRound > 10
        var activeAdvancedSeasons = await _seasonRepository.GetAsync(s =>
            s.IsActive && s.CurrentRound > 10
        );

        // Assert
        activeAdvancedSeasons.Should().HaveCount(1);
        activeAdvancedSeasons.First().Name.Should().Be("Current Active Season");
        activeAdvancedSeasons.First().CurrentRound.Should().Be(20);
        activeAdvancedSeasons.First().IsActive.Should().BeTrue();
    }
}
