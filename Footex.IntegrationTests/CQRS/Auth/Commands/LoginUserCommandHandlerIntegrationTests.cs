using Application.CQRS.Auth.Commands;
using Domain.Models;
using Footex.IntegrationTests.Common;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Footex.IntegrationTests.CQRS.Auth.Commands;

public class LoginUserCommandHandlerIntegrationTests : BaseIntegrationTest
{
    private readonly UserManager<ApplicationUser> _userManager;

    public LoginUserCommandHandlerIntegrationTests(FootexWebApplicationFactory factory)
        : base(factory)
    {
        _userManager = FactoryServiceScope.ServiceProvider.GetRequiredService<
            UserManager<ApplicationUser>
        >();
        FreeDbAsync(Context.Users).Wait();
    }

    [Fact]
    public async Task Handle_ValidLogin_ReturnsSuccessAndTokens()
    {
        // Arrange
        const string password = "Password123!";
        var user = new ApplicationUser
        {
            FirstName = "Test",
            LastName = "User",
            UserName = "testuser",
            Email = "test@example.com",
            EmailConfirmed = true,
        };
        await _userManager.CreateAsync(user, password);

        var command = new LoginUserCommand
        {
            Email = "test@example.com",
            Password = password,
            IpAddress = "127.0.0.1",
        };

        // Act
        var result = await Mediator.Send(command, CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.AccessToken);
        Assert.NotNull(result.RefreshToken);
        Assert.Equal(user.Id, result.UserId);
    }

    [Fact]
    public async Task Handle_InvalidEmail_ReturnsError()
    {
        // Arrange
        var command = new LoginUserCommand
        {
            Email = "nonexistent@example.com",
            Password = "Password123!",
            IpAddress = "127.0.0.1",
        };

        // Act
        var result = await Mediator.Send(command, CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Equal("Invalid email or password", result.Error);
    }

    [Fact]
    public async Task Handle_InvalidPassword_ReturnsError()
    {
        // Arrange
        var user = new ApplicationUser
        {
            FirstName = "Test",
            LastName = "User2",
            UserName = "testuser2",
            Email = "test2@example.com",
            EmailConfirmed = true,
        };
        await _userManager.CreateAsync(user, "Password123!");

        var command = new LoginUserCommand
        {
            Email = "test2@example.com",
            Password = "WrongPassword",
            IpAddress = "127.0.0.1",
        };

        // Act
        var result = await Mediator.Send(command, CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Equal("Invalid email or password", result.Error);
    }
}
