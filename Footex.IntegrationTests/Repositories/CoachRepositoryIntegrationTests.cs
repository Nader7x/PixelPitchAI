using Domain.Interfaces;
using Domain.Models;
using Domain.Repositories;
using FluentAssertions;
using Footex.IntegrationTests.Common;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Footex.IntegrationTests.Repositories;

[Collection("Database")]
public class CoachRepositoryIntegrationTests : BaseIntegrationTest
{
    private readonly ICoachRepository _coachRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CoachRepositoryIntegrationTests(
        FootexWebApplicationFactory factory,
        IUnitOfWork unitOfWork
    )
        : base(factory)
    {
        _unitOfWork = unitOfWork;
        _coachRepository = ServiceProvider.GetRequiredService<ICoachRepository>();
    }

    [Fact]
    public async Task AddAsync_WithValidCoach_SavesSuccessfully()
    {
        // Arrange
        var team = new Team
        {
            Name = "Test Team",
            League = "Test League",
            Country = "Test Country",
            FoundationDate = new DateTime(2000, 1, 1),
        };

        var coach = new Coach
        {
            FirstName = "John",
            LastName = "Smith",
            DateOfBirth = new DateTime(1990, 1, 1),
            Nationality = "England",
            Role = "Head Coach",
            YearsOfExperience = 15,
            Team = team,
        };

        // Act
        await _unitOfWork.Teams.AddAsync(team);
        await _unitOfWork.SaveChangesAsync();

        await _coachRepository.AddAsync(coach);
        await _unitOfWork.SaveChangesAsync();

        // Assert
        coach.Id.Should().BeGreaterThan(0);

        var retrievedCoach = await _coachRepository.GetByIdAsync(coach.Id);
        retrievedCoach.Should().NotBeNull();
        retrievedCoach!.FirstName.Should().Be("John");
        retrievedCoach.LastName.Should().Be("Smith");
        retrievedCoach.DateOfBirth.Should().Be(new DateTime(1990, 1, 1));
        retrievedCoach.Nationality.Should().Be("England");
        retrievedCoach.Role.Should().Be("Head Coach");
        retrievedCoach.YearsOfExperience.Should().Be(15);
        retrievedCoach.TeamId.Should().Be(team.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsCoach()
    {
        // Arrange
        var team = new Team
        {
            Name = "Coach Team",
            League = "Premier League",
            Country = "England",
            FoundationDate = new DateTime(1900, 1, 1),
        };

        var coach = new Coach
        {
            FirstName = "Roberto",
            LastName = "Martinez",
            DateOfBirth = new DateTime(1995, 1, 1),
            Nationality = "Spain",
            Role = "Head Coach",
            YearsOfExperience = 20,
            Team = team,
        };

        await _unitOfWork.Teams.AddAsync(team);
        await _coachRepository.AddAsync(coach);
        await _unitOfWork.SaveChangesAsync();

        // Act
        var result = await _coachRepository.GetByIdAsync(coach.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(coach.Id);
        result.FirstName.Should().Be("Roberto");
        result.LastName.Should().Be("Martinez");
        result.DateOfBirth.Should().Be(new DateTime(1995, 1, 1));
        result.Nationality.Should().Be("Spain");
        result.Role.Should().Be("Head Coach");
        result.YearsOfExperience.Should().Be(20);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentId_ReturnsNull()
    {
        // Act
        var result = await _coachRepository.GetByIdAsync(999999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task SearchAsync_WithFirstName_ReturnsMatchingCoaches()
    {
        // Arrange
        var team = new Team
        {
            Name = "Search Team",
            League = "Test League",
            Country = "Test Country",
            FoundationDate = new DateTime(2000, 1, 1),
        };

        var coach1 = new Coach
        {
            FirstName = "Jose",
            LastName = "Mourinho",
            DateOfBirth = new DateTime(1995, 1, 1),
            Nationality = "Portugal",
            Role = "Head Coach",
            YearsOfExperience = 25,
            Team = team,
        };

        var coach2 = new Coach
        {
            FirstName = "Jose",
            LastName = "Guardiola",
            DateOfBirth = new DateTime(1985, 1, 1),
            Nationality = "Spain",
            Role = "Head Coach",
            YearsOfExperience = 18,
            Team = team,
        };

        var coach3 = new Coach
        {
            FirstName = "Carlo",
            LastName = "Ancelotti",
            DateOfBirth = new DateTime(1985, 1, 1),
            Nationality = "Italy",
            Role = "Head Coach",
            YearsOfExperience = 30,
            Team = team,
        };

        await _unitOfWork.Teams.AddAsync(team);
        await _coachRepository.AddAsync(coach1);
        await _coachRepository.AddAsync(coach2);
        await _coachRepository.AddAsync(coach3);
        await _unitOfWork.SaveChangesAsync();

        // Act
        var searchResult = await _coachRepository.SearchAsync("Jose");

        // Assert
        searchResult.Should().HaveCount(2);
        searchResult.Should().Contain(c => c.LastName == "Mourinho");
        searchResult.Should().Contain(c => c.LastName == "Guardiola");
        searchResult.Should().NotContain(c => c.LastName == "Ancelotti");
    }

    [Fact]
    public async Task SearchAsync_WithLastName_ReturnsMatchingCoaches()
    {
        // Arrange
        var team = new Team
        {
            Name = "Search Team 2",
            League = "Test League",
            Country = "Test Country",
            FoundationDate = new DateTime(1900, 1, 1),
        };

        var coach1 = new Coach
        {
            FirstName = "Pep",
            LastName = "Guardiola",
            DateOfBirth = new DateTime(1985, 1, 1),
            Nationality = "Spain",
            Role = "Head Coach",
            YearsOfExperience = 18,
            Team = team,
        };

        var coach2 = new Coach
        {
            FirstName = "Frank",
            LastName = "Lampard",
            DateOfBirth = new DateTime(1985, 1, 1),
            Nationality = "England",
            Role = "Head Coach",
            YearsOfExperience = 8,
            Team = team,
        };

        await _unitOfWork.Teams.AddAsync(team);
        await _coachRepository.AddAsync(coach1);
        await _coachRepository.AddAsync(coach2);
        await _unitOfWork.SaveChangesAsync();

        // Act
        var searchResult = await _coachRepository.SearchAsync("Guardiola");

        // Assert
        searchResult.Should().HaveCount(1);
        searchResult.First().FirstName.Should().Be("Pep");
        searchResult.First().LastName.Should().Be("Guardiola");
        searchResult.First().Nationality.Should().Be("Spain");
    }

    [Fact]
    public async Task SearchAsync_WithFullName_ReturnsMatchingCoach()
    {
        // Arrange
        var team = new Team
        {
            Name = "Full Name Team",
            League = "Test League",
            Country = "Test Country",
            FoundationDate = new DateTime(1900, 1, 1),
        };

        var coach = new Coach
        {
            FirstName = "Jurgen",
            LastName = "Klopp",
            DateOfBirth = new DateTime(1985, 1, 1),
            Nationality = "Germany",
            Role = "Head Coach",
            YearsOfExperience = 22,
            Team = team,
        };

        await _unitOfWork.Teams.AddAsync(team);
        await _coachRepository.AddAsync(coach);
        await _unitOfWork.SaveChangesAsync();

        // Act
        var searchResult = await _coachRepository.SearchAsync("Jurgen Klopp");

        // Assert
        searchResult.Should().HaveCount(1);
        searchResult.First().FirstName.Should().Be("Jurgen");
        searchResult.First().LastName.Should().Be("Klopp");
        searchResult.First().Nationality.Should().Be("Germany");
    }

    [Fact]
    public async Task SearchAsync_WithRole_ReturnsMatchingCoaches()
    {
        // Arrange
        var team = new Team
        {
            Name = "Role Team",
            League = "Test League",
            Country = "Test Country",
            FoundationDate = new DateTime(1900, 1, 1),
        };

        var headCoach = new Coach
        {
            FirstName = "Head",
            LastName = "Coach",
            DateOfBirth = new DateTime(1985, 1, 1),
            Nationality = "England",
            Role = "Head Coach",
            YearsOfExperience = 15,
            Team = team,
        };

        var assistantCoach = new Coach
        {
            FirstName = "Assistant",
            LastName = "Coach",
            DateOfBirth = new DateTime(1985, 1, 1),
            Nationality = "England",
            Role = "Assistant Coach",
            YearsOfExperience = 10,
            Team = team,
        };

        await _unitOfWork.Teams.AddAsync(team);
        await _coachRepository.AddAsync(headCoach);
        await _coachRepository.AddAsync(assistantCoach);
        await _unitOfWork.SaveChangesAsync();

        // Act
        var searchResult = await _coachRepository.SearchAsync("Assistant");

        // Assert
        searchResult.Should().HaveCount(1);
        searchResult.First().Role.Should().Be("Assistant Coach");
        searchResult.First().FirstName.Should().Be("Assistant");
    }

    [Fact]
    public async Task SearchAsync_WithNationality_ReturnsMatchingCoaches()
    {
        // Arrange
        var team = new Team
        {
            Name = "Nationality Team",
            League = "Test League",
            Country = "Test Country",
            FoundationDate = new DateTime(1985, 1, 1),
        };

        var spanishCoach1 = new Coach
        {
            FirstName = "Luis",
            LastName = "Enrique",
            DateOfBirth = new DateTime(1985, 1, 1),
            Nationality = "Spain",
            Role = "Head Coach",
            YearsOfExperience = 12,
            Team = team,
        };

        var spanishCoach2 = new Coach
        {
            FirstName = "Diego",
            LastName = "Simeone",
            DateOfBirth = new DateTime(1985, 1, 1),
            Nationality = "Spain",
            Role = "Head Coach",
            YearsOfExperience = 20,
            Team = team,
        };

        var germanCoach = new Coach
        {
            FirstName = "Thomas",
            LastName = "Tuchel",
            DateOfBirth = new DateTime(1985, 1, 1),
            Nationality = "Germany",
            Role = "Head Coach",
            YearsOfExperience = 15,
            Team = team,
        };

        await _unitOfWork.Teams.AddAsync(team);
        await _coachRepository.AddAsync(spanishCoach1);
        await _coachRepository.AddAsync(spanishCoach2);
        await _coachRepository.AddAsync(germanCoach);
        await _unitOfWork.SaveChangesAsync();

        // Act
        var searchResult = await _coachRepository.SearchAsync("Spain");

        // Assert
        searchResult.Should().HaveCount(2);
        searchResult.Should().Contain(c => c.FirstName == "Luis");
        searchResult.Should().Contain(c => c.FirstName == "Diego");
        searchResult.Should().NotContain(c => c.FirstName == "Thomas");
        searchResult.All(c => c.Nationality == "Spain").Should().BeTrue();
    }

    [Fact]
    public async Task SearchAsync_WithTeamName_ReturnsCoachesFromTeam()
    {
        // Arrange
        var team1 = new Team
        {
            Name = "Manchester United",
            League = "Premier League",
            Country = "England",
            FoundationDate = new DateTime(1900, 1, 1),
        };

        var team2 = new Team
        {
            Name = "Barcelona",
            League = "La Liga",
            Country = "Spain",
            FoundationDate = new DateTime(1900, 1, 1),
        };

        var coach1 = new Coach
        {
            FirstName = "Erik",
            LastName = "Ten Hag",
            DateOfBirth = new DateTime(1985, 1, 1),
            Nationality = "Netherlands",
            Role = "Head Coach",
            YearsOfExperience = 16,
            Team = team1,
        };

        var coach2 = new Coach
        {
            FirstName = "Xavi",
            LastName = "Hernandez",
            DateOfBirth = new DateTime(1985, 1, 1),
            Nationality = "Spain",
            Role = "Head Coach",
            YearsOfExperience = 5,
            Team = team2,
        };

        await _unitOfWork.Teams.AddAsync(team1);
        await _unitOfWork.Teams.AddAsync(team2);
        await _coachRepository.AddAsync(coach1);
        await _coachRepository.AddAsync(coach2);
        await _unitOfWork.SaveChangesAsync();

        // Act
        var searchResult = await _coachRepository.SearchAsync("Manchester");

        // Assert
        searchResult.Should().HaveCount(1);
        searchResult.First().FirstName.Should().Be("Erik");
        searchResult.First().LastName.Should().Be("Ten Hag");
        searchResult.First().Team!.Name.Should().Be("Manchester United");
    }

    [Fact]
    public async Task SearchAsync_WithEmptyQuery_ReturnsEmpty()
    {
        // Act & Assert
        (await _coachRepository.SearchAsync(""))
            .Should()
            .BeEmpty();
        (await _coachRepository.SearchAsync("   ")).Should().BeEmpty();
    }

    [Fact]
    public async Task SearchAsync_IncludesTeamData_LoadsTeamInformation()
    {
        // Arrange
        var team = new Team
        {
            Name = "Team With Coach",
            League = "Premier League",
            Country = "England",
            FoundationDate = new DateTime(1900, 1, 1),
        };

        var coach = new Coach
        {
            FirstName = "Team",
            LastName = "Coach",
            DateOfBirth = new DateTime(1985, 1, 1),
            Nationality = "England",
            Role = "Head Coach",
            YearsOfExperience = 12,
            Team = team,
        };

        await _unitOfWork.Teams.AddAsync(team);
        await _coachRepository.AddAsync(coach);
        await _unitOfWork.SaveChangesAsync();

        // Act
        var searchResult = await _coachRepository.SearchAsync("Team Coach");

        // Assert
        searchResult.Should().HaveCount(1);
        var foundCoach = searchResult.First();
        foundCoach.Team.Should().NotBeNull();
        foundCoach.Team!.Name.Should().Be("Team With Coach");
        foundCoach.Team.League.Should().Be("Premier League");
        foundCoach.Team.Country.Should().Be("England");
        foundCoach.Team.FoundationDate.Should().Be(new DateTime(1900, 1, 1));
    }

    [Fact]
    public async Task UpdateAsync_WithValidCoach_UpdatesSuccessfully()
    {
        // Arrange
        var team = new Team
        {
            Name = "Update Team",
            League = "Test League",
            Country = "Test Country",
            FoundationDate = new DateTime(1900, 1, 1),
        };

        var coach = new Coach
        {
            FirstName = "Original",
            LastName = "Coach",
            DateOfBirth = new DateTime(1985, 1, 1),
            Nationality = "England",
            Role = "Assistant Coach",
            YearsOfExperience = 5,
            Team = team,
        };

        await _unitOfWork.Teams.AddAsync(team);
        await _coachRepository.AddAsync(coach);
        await _unitOfWork.SaveChangesAsync();

        // Act
        coach.FirstName = "Updated";
        coach.LastName = "Name";
        coach.DateOfBirth = new DateTime(1985, 1, 1);
        coach.Role = "Head Coach";
        coach.YearsOfExperience = 15;

        _coachRepository.UpdateAsync(coach);
        await _unitOfWork.SaveChangesAsync();

        // Assert
        var updatedCoach = await _coachRepository.GetByIdAsync(coach.Id);
        updatedCoach.Should().NotBeNull();
        updatedCoach!.FirstName.Should().Be("Updated");
        updatedCoach.LastName.Should().Be("Name");
        updatedCoach.DateOfBirth.Should().Be(new DateTime(1985, 1, 1));
        updatedCoach.Role.Should().Be("Head Coach");
        updatedCoach.YearsOfExperience.Should().Be(15);
        updatedCoach.Nationality.Should().Be("England"); // Should remain unchanged
    }

    [Fact]
    public async Task DeleteAsync_WithValidCoach_RemovesSuccessfully()
    {
        // Arrange
        var team = new Team
        {
            Name = "Delete Team",
            League = "Test League",
            Country = "Test Country",
            FoundationDate = new DateTime(1900, 1, 1),
        };

        var coach = new Coach
        {
            FirstName = "Coach",
            LastName = "To Delete",
            DateOfBirth = new DateTime(1985, 1, 1),
            Nationality = "Portugal",
            Role = "Head Coach",
            YearsOfExperience = 20,
            Team = team,
        };

        await _unitOfWork.Teams.AddAsync(team);
        await _coachRepository.AddAsync(coach);
        await _unitOfWork.SaveChangesAsync();

        var coachId = coach.Id;

        // Act
        _coachRepository.DeleteAsync(coach);
        await _unitOfWork.SaveChangesAsync();

        // Assert
        var deletedCoach = await _coachRepository.GetByIdAsync(coachId);
        deletedCoach.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_WithMultipleCoaches_ReturnsAllCoaches()
    {
        // Arrange
        var team = new Team
        {
            Name = "Multi Coach Team",
            League = "Test League",
            Country = "Test Country",
            FoundationDate = new DateTime(1900, 1, 1),
        };

        var coaches = new List<Coach>
        {
            new()
            {
                FirstName = "Coach",
                LastName = "One",
                DateOfBirth = new DateTime(1985, 1, 1),
                Nationality = "Spain",
                Role = "Head Coach",
                YearsOfExperience = 15,
                Team = team,
            },
            new()
            {
                FirstName = "Coach",
                LastName = "Two",
                DateOfBirth = new DateTime(1985, 1, 1),
                Nationality = "Italy",
                Role = "Assistant Coach",
                YearsOfExperience = 8,
                Team = team,
            },
            new()
            {
                FirstName = "Coach",
                LastName = "Three",
                DateOfBirth = new DateTime(1985, 1, 1),
                Nationality = "Germany",
                Role = "Goalkeeping Coach",
                YearsOfExperience = 12,
                Team = team,
            },
        };

        await _unitOfWork.Teams.AddAsync(team);
        foreach (var coach in coaches)
            await _coachRepository.AddAsync(coach);
        await _unitOfWork.SaveChangesAsync();

        // Act
        var allCoaches = await _coachRepository.GetAllAsync();

        // Assert
        allCoaches.Should().HaveCountGreaterOrEqualTo(3);
        allCoaches.Should().Contain(c => c.LastName == "One");
        allCoaches.Should().Contain(c => c.LastName == "Two");
        allCoaches.Should().Contain(c => c.LastName == "Three");
    }

    [Fact]
    public async Task Repository_WithTransactions_HandlesRollbackCorrectly()
    {
        // Arrange
        var team = new Team
        {
            Name = "Transaction Team",
            League = "Test League",
            Country = "Test Country",
            FoundationDate = new DateTime(1900, 1, 1),
        };

        var coach = new Coach
        {
            FirstName = "Transaction",
            LastName = "Coach",
            DateOfBirth = new DateTime(1985, 1, 1),
            Nationality = "France",
            Role = "Head Coach",
            YearsOfExperience = 18,
            Team = team,
        };

        await _unitOfWork.Teams.AddAsync(team);
        await _unitOfWork.SaveChangesAsync();

        // Act
        await _unitOfWork.BeginTransactionAsync();

        await _coachRepository.AddAsync(coach);
        await _unitOfWork.SaveChangesAsync();

        // Verify coach exists within transaction
        var coachInTransaction = await _coachRepository.SearchAsync("Transaction Coach");
        coachInTransaction.Should().HaveCount(1);

        // Rollback
        await _unitOfWork.RollbackTransactionAsync();

        // Assert
        var coachAfterRollback = await _coachRepository.SearchAsync("Transaction Coach");
        coachAfterRollback.Should().BeEmpty();
    }

    [Fact]
    public async Task FindAsync_WithPredicate_ReturnsMatchingCoaches()
    {
        // Arrange
        var team = new Team
        {
            Name = "Experience Team",
            League = "Test League",
            Country = "Test Country",
            FoundationDate = new DateTime(1900, 1, 1),
        };

        var experiencedCoach1 = new Coach
        {
            FirstName = "Experienced",
            LastName = "Coach One",
            DateOfBirth = new DateTime(1985, 1, 1),
            Nationality = "Italy",
            Role = "Head Coach",
            YearsOfExperience = 25,
            Team = team,
        };

        var experiencedCoach2 = new Coach
        {
            FirstName = "Experienced",
            LastName = "Coach Two",
            DateOfBirth = new DateTime(1985, 1, 1),
            Nationality = "Spain",
            Role = "Head Coach",
            YearsOfExperience = 22,
            Team = team,
        };

        var inexperiencedCoach = new Coach
        {
            FirstName = "New",
            LastName = "Coach",
            DateOfBirth = new DateTime(1985, 1, 1),
            Nationality = "England",
            Role = "Assistant Coach",
            YearsOfExperience = 3,
            Team = team,
        };

        await _unitOfWork.Teams.AddAsync(team);
        await _coachRepository.AddAsync(experiencedCoach1);
        await _coachRepository.AddAsync(experiencedCoach2);
        await _coachRepository.AddAsync(inexperiencedCoach);
        await _unitOfWork.SaveChangesAsync();

        // Act
        var experiencedCoaches = await _coachRepository.GetAsync(c => c.YearsOfExperience >= 20);

        // Assert
        experiencedCoaches.Should().HaveCount(2);
        experiencedCoaches.Should().Contain(c => c.LastName == "Coach One");
        experiencedCoaches.Should().Contain(c => c.LastName == "Coach Two");
        experiencedCoaches.Should().NotContain(c => c.LastName == "Coach");
        experiencedCoaches.All(c => c.YearsOfExperience >= 20).Should().BeTrue();
    }

    [Fact]
    public async Task Coach_WithoutTeam_CanBeCreated()
    {
        // Arrange
        var coach = new Coach
        {
            FirstName = "Free",
            LastName = "DateOfBirthnt",
            DateOfBirth = new DateTime(1985, 1, 1),
            Nationality = "Brazil",
            Role = "Head Coach",
            YearsOfExperience = 14,
            // No team assigned
        };

        // Act
        await _coachRepository.AddAsync(coach);
        await _unitOfWork.SaveChangesAsync();

        // Assert
        coach.Id.Should().BeGreaterThan(0);

        var retrievedCoach = await _coachRepository.GetByIdAsync(coach.Id);
        retrievedCoach.Should().NotBeNull();
        retrievedCoach!.FirstName.Should().Be("Free");
        retrievedCoach.LastName.Should().Be("DateOfBirthnt");
        retrievedCoach.TeamId.Should().BeNull();
    }

    [Fact]
    public async Task SearchAsync_WithCaseInsensitiveSearch_ReturnsResults()
    {
        // Arrange
        var team = new Team
        {
            Name = "Case Test Team",
            League = "Test League",
            Country = "Test Country",
            FoundationDate = new DateTime(1900, 1, 1),
        };

        var coach = new Coach
        {
            FirstName = "UPPERCASE",
            LastName = "lowercase",
            DateOfBirth = new DateTime(1985, 1, 1),
            Nationality = "MiXeD CaSe",
            Role = "Head Coach",
            YearsOfExperience = 15,
            Team = team,
        };

        await _unitOfWork.Teams.AddAsync(team);
        await _coachRepository.AddAsync(coach);
        await _unitOfWork.SaveChangesAsync();

        // Act
        var upperCaseSearch = await _coachRepository.SearchAsync("uppercase");
        var lowerCaseSearch = await _coachRepository.SearchAsync("LOWERCASE");
        var mixedCaseSearch = await _coachRepository.SearchAsync("MiXeD");

        // Assert
        upperCaseSearch.Should().HaveCount(1);
        lowerCaseSearch.Should().HaveCount(1);
        mixedCaseSearch.Should().HaveCount(1);

        upperCaseSearch.First().FirstName.Should().Be("UPPERCASE");
        lowerCaseSearch.First().LastName.Should().Be("lowercase");
        mixedCaseSearch.First().Nationality.Should().Be("MiXeD CaSe");
    }
}
