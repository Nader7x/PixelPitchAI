using System.Net;
using System.Net.Http.Json;
using Application.CQRS.Auth.Commands;
using FluentAssertions;
using Footex.IntegrationTests.Common;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Footex.IntegrationTests.Controllers;

public class AuthControllerTests : IClassFixture<FootexWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthControllerTests(FootexWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task Register_WithValidData_ReturnsSuccess()
    {
        // Arrange
        var registerRequest = new
        {
            Email = "test@example.com",
            Password = "Test123!",
            Username = "testuser"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        // Assert
        response.EnsureSuccessStatusCode();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsToken()
    {
        // Arrange
        var loginRequest = new
        {
            Email = "user@example.com", // Use credentials that exist in your test database
            Password = "Password123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.EnsureSuccessStatusCode();
        var loginResponse = await response.Content.ReadFromJsonAsync<LoginUserCommandResponse>();
        
        loginResponse.Should().NotBeNull();
        loginResponse.AccessToken.Should().NotBeNullOrEmpty();
        loginResponse.RefreshToken.Should().NotBeNullOrEmpty();
        loginResponse.TokenExpires.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var loginRequest = new
        {
            Email = "nonexistent@example.com",
            Password = "WrongPassword123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RefreshToken_WithValidToken_ReturnsNewToken()
    {
        // Arrange - First login to get tokens
        var loginRequest = new
        {
            Email = "user@example.com",
            Password = "Password123!"
        };

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        var tokens = await loginResponse.Content.ReadFromJsonAsync<LoginUserCommandResponse>();

        var refreshRequest = new
        {
            RefreshToken = tokens.RefreshToken
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/refresh", refreshRequest);

        // Assert
        response.EnsureSuccessStatusCode();
        var refreshResponse = await response.Content.ReadFromJsonAsync<LoginUserCommandResponse>();
        
        refreshResponse.Should().NotBeNull();
        refreshResponse.AccessToken.Should().NotBeNullOrEmpty();
        refreshResponse.RefreshToken.Should().NotBeNullOrEmpty();
        refreshResponse.TokenExpires.Should().BeAfter(DateTime.UtcNow + TimeSpan.FromMinutes(5));
        refreshResponse.AccessToken.Should().NotBe(tokens.AccessToken);
    }
}
