using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Application.Dtos;
using FluentAssertions;
using Footex.IntegrationTests.Common;
using Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Footex.IntegrationTests.Controllers;

public class StadiumsControllerIntegrationTests : IClassFixture<FootexWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly FootexWebApplicationFactory _factory;

    public StadiumsControllerIntegrationTests(FootexWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetAllStadiums_ReturnsSuccessStatusCode()
    {
        // Act
        var response = await _client.GetAsync("/api/stadiums");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeEmpty();

        var jsonDoc = JsonDocument.Parse(content);
        jsonDoc.RootElement.TryGetProperty("succeeded", out var succeeded);
        succeeded.GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task GetAllStadiums_WithQueryParameters_ReturnsFilteredResults()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var response = await _client.GetAsync("/api/stadiums?country=England&city=London");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);

        jsonDoc.RootElement.TryGetProperty("succeeded", out var succeeded);
        succeeded.GetBoolean().Should().BeTrue();

        jsonDoc.RootElement.TryGetProperty("stadiums", out var stadiums);
        stadiums.ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task GetStadiumById_WithValidId_ReturnsStadium()
    {
        // Arrange
        var stadiumId = await SeedTestDataAsync();

        // Act
        var response = await _client.GetAsync($"/api/stadiums/{stadiumId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);

        jsonDoc.RootElement.TryGetProperty("succeeded", out var succeeded);
        succeeded.GetBoolean().Should().BeTrue();

        jsonDoc.RootElement.TryGetProperty("stadium", out var stadium);
        stadium.ValueKind.Should().Be(JsonValueKind.Object);

        stadium.TryGetProperty("id", out var id);
        id.GetInt32().Should().Be(stadiumId);
    }

    [Fact]
    public async Task GetStadiumById_WithInvalidId_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/stadiums/999999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetStadiumById_ReturnsCacheHeaders()
    {
        // Arrange
        var stadiumId = await SeedTestDataAsync();

        // Act - First call
        var response1 = await _client.GetAsync($"/api/stadiums/{stadiumId}");

        // Act - Second call (should be cached)
        var response2 = await _client.GetAsync($"/api/stadiums/{stadiumId}");

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);

        // Check cache headers
        response1.Headers.Should().ContainKey("X-Cache-Hit");
        response1.Headers.GetValues("X-Cache-Hit").First().Should().Be("false");

        response2.Headers.Should().ContainKey("X-Cache-Hit");
        response2.Headers.GetValues("X-Cache-Hit").First().Should().Be("true");
    }

    [Fact]
    public async Task CreateStadium_WithValidData_ReturnsCreated()
    {
        // Arrange
        var createStadiumDto = new CreateStadiumDto
        {
            Name = "Test Stadium",
            Country = "England",
            City = "London",
            Capacity = 50000,
            BuiltDate = new DateTime(2000, 1, 1),
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/stadiums", createStadiumDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);

        jsonDoc.RootElement.TryGetProperty("succeeded", out var succeeded);
        succeeded.GetBoolean().Should().BeTrue();

        jsonDoc.RootElement.TryGetProperty("stadiumId", out var stadiumId);
        stadiumId.GetInt32().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateStadium_WithInvalidData_ReturnsBadRequest()
    {
        // Arrange
        var createStadiumDto = new CreateStadiumDto
        {
            Name = "", // Invalid: empty name
            Country = "England",
            City = "London",
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/stadiums", createStadiumDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateStadium_WithValidData_ReturnsOk()
    {
        // Arrange
        var stadiumId = await SeedTestDataAsync();
        var updateStadiumDto = new UpdateStadiumDto
        {
            Name = "Updated Stadium",
            Country = "Spain",
            City = "Madrid",
            Capacity = 60000,
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/stadiums/{stadiumId}", updateStadiumDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);

        jsonDoc.RootElement.TryGetProperty("succeeded", out var succeeded);
        succeeded.GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task UpdateStadium_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var updateStadiumDto = new UpdateStadiumDto
        {
            Name = "Updated Stadium",
            Country = "Spain",
            City = "Madrid",
        };

        // Act
        var response = await _client.PutAsJsonAsync("/api/stadiums/999999", updateStadiumDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteStadium_WithValidId_ReturnsOk()
    {
        // Arrange
        var stadiumId = await SeedTestDataAsync();

        // Act
        var response = await _client.DeleteAsync($"/api/stadiums/{stadiumId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);

        jsonDoc.RootElement.TryGetProperty("succeeded", out var succeeded);
        succeeded.GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task DeleteStadium_WithInvalidId_ReturnsNotFound()
    {
        // Act
        var response = await _client.DeleteAsync("/api/stadiums/999999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAllStadiums_CachesBehavior_WorksCorrectly()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act - First call
        var response1 = await _client.GetAsync("/api/stadiums?country=England");

        // Act - Second call with same parameters (should be cached)
        var response2 = await _client.GetAsync("/api/stadiums?country=England");

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);

        // Check cache headers
        response1.Headers.Should().ContainKey("X-Cache-Hit");
        response1.Headers.GetValues("X-Cache-Hit").First().Should().Be("false");

        response2.Headers.Should().ContainKey("X-Cache-Hit");
        response2.Headers.GetValues("X-Cache-Hit").First().Should().Be("true");
    }

    private async Task<int> SeedTestDataAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FootballDbContext>();

        var stadium = TestData.CreateTestStadium();
        context.Stadiums.Add(stadium);
        await context.SaveChangesAsync();

        return stadium.Id;
    }
}
