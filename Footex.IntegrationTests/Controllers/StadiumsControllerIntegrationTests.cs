using System.Net;
using System.Net.Http.Json;
using Application.CQRS.Stadiums.Commands;
using Application.CQRS.Stadiums.Queries;
using Application.Dtos;
using Application.Helpers;
using FluentAssertions;
using Footex.IntegrationTests.Common;
using Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Footex.IntegrationTests.Controllers;
[Collection("Database collection")]
public class StadiumsControllerIntegrationTests(FootexWebApplicationFactory factory)
    : IClassFixture<FootexWebApplicationFactory>
{
    [Fact]
    public async Task GetAllStadiums_ReturnsSuccessStatusCode()
    {
        // Arrange
        var httpClient = await factory.CreateAuthenticatedClientAsync();

        // Act
        var response = await httpClient.GetAsync("/api/stadiums");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<GetAllStadiumsQueryResponse>();
        result.Should().NotBeNull();
        result?.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task GetAllStadiums_WithQueryParameters_ReturnsFilteredResults()
    {
        // Arrange
        var httpClient = await factory.CreateAuthenticatedClientAsync();
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FootballDbContext>();
        var stadium = TestData.CreateTestDbStadium();
        context.Stadiums.Add(stadium);
        await context.SaveChangesAsync();

        // Act
        var response = await httpClient.GetAsync("/api/stadiums?country=England&city=London");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<GetAllStadiumsQueryResponse>();
        result.Should().NotBeNull();
        result?.Succeeded.Should().BeTrue();
        result?.Stadiums.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetStadiumById_WithValidId_ReturnsStadium()
    {
        // Arrange
        var httpClient = await factory.CreateAuthenticatedClientAsync();
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FootballDbContext>();
        var stadium = TestData.CreateTestDbStadium();
        context.Stadiums.Add(stadium);
        await context.SaveChangesAsync();

        // Act
        var response = await httpClient.GetAsync($"/api/stadiums/{stadium.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<GetStadiumByIdQueryResponse>();
        result.Should().NotBeNull();
        result?.Succeeded.Should().BeTrue();
        result?.Stadium.Should().NotBeNull();
        result?.Stadium!.Id.Should().Be(stadium.Id);
    }

    [Fact]
    public async Task GetStadiumById_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var httpClient = await factory.CreateAuthenticatedClientAsync();

        // Act
        var response = await httpClient.GetAsync("/api/stadiums/999999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateStadium_WithValidData_ReturnsCreated()
    {
        // Arrange
        var httpClient = await factory.CreateAuthenticatedClientAsync();
        var createStadiumDto = new CreateStadiumDto
        {
            Name = "Test Stadium",
            Country = "England",
            City = "London",
            Capacity = 50000,
            BuiltDate = new DateTime(2000, 1, 1),
        };

        // Act
        var response = await httpClient.PostAsync(
            "/api/stadiums",
            createStadiumDto.ToMultipartFormDataContent()
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<CreateStadiumCommandResponse>();
        result.Should().NotBeNull();
        result?.Succeeded.Should().BeTrue();
        result?.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task UpdateStadium_WithValidData_ReturnsOk()
    {
        // Arrange
        var httpClient = await factory.CreateAuthenticatedClientAsync();
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FootballDbContext>();
        var stadium = TestData.CreateTestDbStadium();
        context.Stadiums.Add(stadium);
        await context.SaveChangesAsync();
        var updateStadiumDto = new UpdateStadiumDto
        {
            Name = "Updated Stadium",
            Country = "Spain",
            City = "Madrid",
            Capacity = 60000,
            BuiltDate = new DateTime(1995, 5, 15),
        };

        // Act
        var response = await httpClient.PutAsync(
            $"/api/stadiums/{stadium.Id}",
            updateStadiumDto.ToMultipartFormDataContent()
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<UpdateStadiumCommandResponse>();
        result.Should().NotBeNull();
        result?.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteStadium_WithValidId_ReturnsOk()
    {
        // Arrange
        var httpClient = await factory.CreateAuthenticatedClientAsync();
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FootballDbContext>();
        var stadium = TestData.CreateTestDbStadium();
        context.Stadiums.Add(stadium);
        await context.SaveChangesAsync();

        // Act
        var response = await httpClient.DeleteAsync($"/api/stadiums/{stadium.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<DeleteStadiumCommandResponse>();
        result.Should().NotBeNull();
        result?.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateStadium_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var httpClient = await factory.CreateAuthenticatedClientAsync();
        var updateStadiumDto = new UpdateStadiumDto
        {
            Name = "Nonexistent Stadium",
            Country = "Nowhere",
            City = "Ghost City",
            Capacity = 1000,
            BuiltDate = new DateTime(1900, 1, 1),
        };

        // Act
        var response = await httpClient.PutAsync(
            $"/api/stadiums/999999",
            updateStadiumDto.ToMultipartFormDataContent()
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateStadium_WithInvalidData_ReturnsBadRequest()
    {
        // Arrange
        var httpClient = await factory.CreateAuthenticatedClientAsync();
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FootballDbContext>();
        var stadium = TestData.CreateTestDbStadium();
        context.Stadiums.Add(stadium);
        await context.SaveChangesAsync();
        var updateStadiumDto = new UpdateStadiumDto
        {
            Name = "", // Invalid: Name required
            Country = "",
            City = "",
            Capacity = -1, // Invalid: Capacity must be positive
            BuiltDate = DateTime.MinValue,
        };

        // Act
        var response = await httpClient.PutAsync(
            $"/api/stadiums/{stadium.Id}",
            updateStadiumDto.ToMultipartFormDataContent()
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeleteStadium_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var httpClient = await factory.CreateAuthenticatedClientAsync();

        // Act
        var response = await httpClient.DeleteAsync($"/api/stadiums/999999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
