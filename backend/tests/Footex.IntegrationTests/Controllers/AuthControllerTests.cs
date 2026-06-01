using System.Net;
using System.Net.Http.Json;
using Application.CQRS.Auth.Commands;
using Application.Dtos;
using Domain.Models;
using FluentAssertions;
using Footex.IntegrationTests.Common;
using Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Footex.IntegrationTests.Controllers;
[Collection("Database collection")]
public class AuthControllerTests(FootexWebApplicationFactory factory)
    : IClassFixture<FootexWebApplicationFactory>
{
    [Fact]
    public async Task Register_WithValidData_ReturnsSuccess()
    {
        var httpClient = await factory.CreateAuthenticatedClientAsync();
        // Arrange
        var registerRequest = new RegisterUserDto
        {
            Email = "test@example.com",
            Password = "Test123!",
            FirstName = "User1",
            UserName = "test@example.com",
            LastName = "LastName",
            PhoneNumber = "0124647879546",
        };

        var formData = new MultipartFormDataContent
        {
            { new StringContent(registerRequest.Email), "Email" },
            { new StringContent(registerRequest.Password), "Password" },
            { new StringContent(registerRequest.FirstName), "FirstName" },
            { new StringContent(registerRequest.LastName ?? string.Empty), "LastName" },
            { new StringContent(registerRequest.PhoneNumber ?? string.Empty), "PhoneNumber" },
            { new StringContent(registerRequest.UserName), "UserName" },
        };

        // Act
        var response = await httpClient.PostAsync("/api/auth/register", formData);

        // Assert
        response.EnsureSuccessStatusCode();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsToken()
    {
        var httpClient = await factory.CreateAuthenticatedClientAsync();
        // await CreateValidUser();
        // Arrange
        var loginRequest = new UserLoginDto
        {
            Email = "testuser@example.com", // Use credentials that exist in your test database
            Password = "Password123!",
        };

        // Act
        var response = await httpClient.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.EnsureSuccessStatusCode();
        var loginResponse = await response.Content.ReadFromJsonAsync<LoginUserCommandResponse>();

        loginResponse.Should().NotBeNull();
        loginResponse.AccessToken.Should().NotBeNullOrEmpty();
        loginResponse.RefreshToken.Should().NotBeNullOrEmpty();
        loginResponse.TokenExpires.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsBadRequest()
    {
        var httpClient = await factory.CreateAuthenticatedClientAsync();
        // Arrange
        var loginRequest = new UserLoginDto
        {
            Email = "nonexistent@example.com",
            Password = "WrongPassword123!",
        };

        // Act
        var response = await httpClient.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RefreshToken_WithValidToken_ReturnsNewToken()
    {
        var httpClient = await factory.CreateAuthenticatedClientAsync();
        // await CreateValidUser();
        var loginRequest = new UserLoginDto
        {
            Email = "testuser@example.com",
            Password = "Password123!",
        };

        var loginResponse = await httpClient.PostAsJsonAsync("/api/auth/login", loginRequest);
        var tokens = await loginResponse.Content.ReadFromJsonAsync<LoginUserCommandResponse>();
        
        // Act
        var response = await httpClient.PostAsync("/api/auth/refresh-token", null);

        // Assert
        response.EnsureSuccessStatusCode();
        var refreshResponse =
            await response.Content.ReadFromJsonAsync<RefreshTokenCommandResponse>();

        refreshResponse.Should().NotBeNull();
        refreshResponse.AccessToken.Should().NotBeNullOrEmpty();
        refreshResponse.RefreshToken.Should().NotBeNullOrEmpty();
        refreshResponse.TokenExpires.Should().BeAfter(DateTime.UtcNow + TimeSpan.FromMinutes(5));
        refreshResponse.AccessToken.Should().NotBe(tokens?.AccessToken);
    }

    // private async Task CreateValidUser()
    // {
    //     try
    //     {
    //         var scope = factory.Services.CreateScope();
    //         var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    //         var context = scope.ServiceProvider.GetRequiredService<FootballDbContext>();
    //         var testUser = TestData.CreateTestUser(true);
    //         await userManager.CreateAsync(testUser, "Password123!");
    //         await userManager.AddToRoleAsync(testUser, "Admin");
    //         await userManager.AddToRoleAsync(testUser, "User");
    //         await context.SaveChangesAsync();
    //         Console.WriteLine("Test user created successfully.");
    //     }
    //     catch (Exception e)
    //     {
    //         Console.WriteLine(e.Message);
    //     }
    // }
}
