using System.Net;
using System.Net.Http.Json;
using Application.CQRS.Coaches.Commands;
using Application.CQRS.Coaches.Queries;
using Application.Dtos;
using FluentAssertions;
using Footex.IntegrationTests.Common;
using Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Footex.IntegrationTests.Controllers;

public class CoachesControllerIntegrationTests(
    FootexWebApplicationFactory factory,
    ITestOutputHelper testOutputHelper
) : IClassFixture<FootexWebApplicationFactory>
{
    private readonly ITestOutputHelper _testOutputHelper = testOutputHelper;

    [Fact]
    public async Task GetAllCoaches_ReturnsSuccessStatusCode()
    {
        // Arrange
        var httpClient = await factory.CreateAuthenticatedClientAsync();

        // Act
        var response = await httpClient.GetAsync("/api/coaches/filter");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<GetAllCoachesQueryResponse>();
        result.Should().NotBeNull();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task GetAllCoaches_WithQueryParameters_ReturnsFilteredResults()
    {
        // Arrange
        var httpClient = await factory.CreateAuthenticatedClientAsync();
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FootballDbContext>();
        var team = TestData.CreateTestDbTeam();
        context.Teams.Add(team);
        await context.SaveChangesAsync();
        var coach = TestData.CreateTestDbCoach(team.Id);
        context.Coaches.Add(coach);
        await context.SaveChangesAsync();

        // Act
        var response = await httpClient.GetAsync("/api/coaches/filter?nationality=England");
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _testOutputHelper.WriteLine(errorContent);
        }

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<GetAllCoachesQueryResponse>();
        result.Should().NotBeNull();
        result?.Succeeded.Should().BeTrue();
        result?.Coaches.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetCoachById_WithValidId_ReturnsCoach()
    {
        // Arrange
        var httpClient = await factory.CreateAuthenticatedClientAsync();
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FootballDbContext>();
        var team = TestData.CreateTestDbTeam();
        context.Teams.Add(team);
        await context.SaveChangesAsync();
        var coach = TestData.CreateTestDbCoach(team.Id);
        context.Coaches.Add(coach);
        await context.SaveChangesAsync();

        // Act
        var response = await httpClient.GetAsync($"/api/coaches/{coach.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<GetCoachByIdQueryResponse>();
        result.Should().NotBeNull();
        result?.Succeeded.Should().BeTrue();
        result?.Coach.Should().NotBeNull();
        result?.Coach?.Id.Should().Be(coach.Id);
    }

    [Fact]
    public async Task GetCoachById_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var httpClient = await factory.CreateAuthenticatedClientAsync();

        // Act
        var response = await httpClient.GetAsync("/api/coaches/99999");

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _testOutputHelper.WriteLine(errorContent);
        }

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateCoach_WithValidData_ReturnsCreated()
    {
        // Arrange
        var httpClient = await factory.CreateAuthenticatedClientAsync();
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FootballDbContext>();
        var team = TestData.CreateTestDbTeam();
        context.Teams.Add(team);
        await context.SaveChangesAsync(CancellationToken.None);
        var createCoachDto = new CreateCoachDto
        {
            FirstName = "John",
            LastName = "Smith",
            DateOfBirth = new DateTime(1970, 3, 15),
            Nationality = "England",
            YearsOfExperience = 10,
            TeamId = team.Id,
            Role = "Head Coach",
        };
        var formData = new MultipartFormDataContent
        {
            { new StringContent(createCoachDto.FirstName), "FirstName" },
            { new StringContent(createCoachDto.LastName), "LastName" },
            { new StringContent(createCoachDto.DateOfBirth.ToString("yyyy-MM-dd")), "DateOfBirth" },
            { new StringContent(createCoachDto.Nationality), "Nationality" },
            {
                new StringContent(createCoachDto.YearsOfExperience.ToString() ?? string.Empty),
                "YearsOfExperience"
            },
            { new StringContent(createCoachDto.TeamId.ToString() ?? string.Empty), "TeamId" },
            { new StringContent(createCoachDto.Role ?? string.Empty), "Role" },
        };

        // Act
        var response = await httpClient.PostAsync("/api/coaches", formData);
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _testOutputHelper.WriteLine(errorContent);
        }

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CreateCoachCommandResponse>();
        result.Should().NotBeNull();
        result!.Succeeded.Should().BeTrue();
        result.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateCoach_WithInvalidData_ReturnsBadRequest()
    {
        // Arrange
        var httpClient = await factory.CreateAuthenticatedClientAsync();
        var createCoachDto = new CreateCoachDto
        {
            FirstName = "", // Invalid: empty name
            LastName = "Smith",
            DateOfBirth = new DateTime(1970, 3, 15),
        };

        // Act
        var response = await httpClient.PostAsJsonAsync("/api/coaches", createCoachDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateCoach_WithValidData_ReturnsOk()
    {
        // Arrange
        var httpClient = await factory.CreateAuthenticatedClientAsync();
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FootballDbContext>();
        var team = TestData.CreateTestDbTeam();
        context.Teams.Add(team);
        await context.SaveChangesAsync();
        var coach = TestData.CreateTestDbCoach(team.Id);
        context.Coaches.Add(coach);
        await context.SaveChangesAsync();
        var updateCoachDto = new UpdateCoachDto
        {
            FirstName = "Updated",
            LastName = "Coach",
            Nationality = "Spain",
            YearsOfExperience = 15,
        };
        var formData = new MultipartFormDataContent
        {
            { new StringContent(updateCoachDto.FirstName), "FirstName" },
            { new StringContent(updateCoachDto.LastName), "LastName" },
            { new StringContent(updateCoachDto.Nationality), "Nationality" },
            {
                new StringContent(updateCoachDto.YearsOfExperience.ToString() ?? string.Empty),
                "YearsOfExperience"
            },
        };

        // Act
        var response = await httpClient.PutAsync($"/api/coaches/{coach.Id}", formData);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<UpdateCoachCommandResponse>();
        result.Should().NotBeNull();
        result?.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateCoach_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var httpClient = await factory.CreateAuthenticatedClientAsync();
        var updateCoachDto = new UpdateCoachDto
        {
            FirstName = "Updated",
            LastName = "Coach",
            Nationality = "Spain",
        };
        var formData = new MultipartFormDataContent
        {
            { new StringContent(updateCoachDto.FirstName), "FirstName" },
            { new StringContent(updateCoachDto.LastName), "LastName" },
            { new StringContent(updateCoachDto.Nationality), "Nationality" },
        };

        // Act
        var response = await httpClient.PutAsync("/api/coaches/999999", formData);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteCoach_WithValidId_ReturnsOk()
    {
        // Arrange
        var httpClient = await factory.CreateAuthenticatedClientAsync();
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FootballDbContext>();
        var coach = TestData.CreateTestDbCoach();
        context.Coaches.Add(coach);
        await context.SaveChangesAsync();

        // Act
        var response = await httpClient.DeleteAsync($"/api/coaches/{coach.Id}");

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _testOutputHelper.WriteLine(errorContent);
        }

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<DeleteCoachCommandResponse>();
        result.Should().NotBeNull();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteCoach_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var httpClient = await factory.CreateAuthenticatedClientAsync();

        // Act
        var response = await httpClient.DeleteAsync("/api/coaches/999999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
