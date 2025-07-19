using Application.CQRS.Auth.Commands;
using Domain.Models;
using FluentAssertions;
using Footex.IntegrationTests.Common;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Footex.IntegrationTests.CQRS.Auth.Commands;

public class RevokeTokenCommandHandlerIntegrationTests : BaseIntegrationTest
{
    private readonly UserManager<ApplicationUser> _userManager;

    public RevokeTokenCommandHandlerIntegrationTests(FootexWebApplicationFactory factory)
        : base(factory)
    {
        _userManager = FactoryServiceScope.ServiceProvider.GetRequiredService<
            UserManager<ApplicationUser>
        >();
    }

    [Fact]
    public async Task Handle_ValidRevokeToken_Succeeds()
    {
        // Arrange
        const string password = "Password123!";
        var user = new ApplicationUser
        {
            FirstName = "Revoke",
            LastName = "Token",
            UserName = "revokeuser",
            Email = "revoke@example.com",
            EmailConfirmed = true,
        };
        await _userManager.CreateAsync(user, password);
        // Simulate login to get refresh token
        var loginCommand = new LoginUserCommand
        {
            Email = user.Email,
            Password = password,
            IpAddress = "127.0.0.1",
        };
        var loginResult = await Mediator.Send(loginCommand);
        var refreshToken = loginResult.RefreshToken;

        var command = new RevokeTokenCommand
        {
            RefreshToken = refreshToken,
            IpAddress = "127.0.0.1",
        };

        // Act
        var result = await Mediator.Send(command);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Error.Should().BeNull();
    }
}
