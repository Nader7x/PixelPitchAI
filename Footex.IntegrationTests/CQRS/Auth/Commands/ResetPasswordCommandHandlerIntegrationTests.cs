using Application.CQRS.Auth.Commands;
using Domain.Models;
using FluentAssertions;
using Footex.IntegrationTests.Common;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Footex.IntegrationTests.CQRS.Auth.Commands;

public class ResetPasswordCommandHandlerIntegrationTests : BaseIntegrationTest
{
    private readonly UserManager<ApplicationUser> _userManager;

    public ResetPasswordCommandHandlerIntegrationTests(FootexWebApplicationFactory factory)
        : base(factory)
    {
        _userManager = FactoryServiceScope.ServiceProvider.GetRequiredService<
            UserManager<ApplicationUser>
        >();
    }

    [Fact]
    public async Task Handle_ValidResetPassword_Succeeds()
    {
        // Arrange
        var password = "Password123!";
        var user = new ApplicationUser
        {
            FirstName = "Reset",
            LastName = "Password",
            UserName = "resetuser",
            Email = "reset@example.com",
            EmailConfirmed = true,
        };
        await _userManager.CreateAsync(user, password);
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var newPassword = "NewPassword123!";

        var command = new ResetPasswordCommand
        {
            Email = user.Email,
            Token = token,
            NewPassword = newPassword,
        };

        // Act
        var result = await Mediator.Send(command);

        // Assert
        result.Succeeded.Should().BeTrue();
        var signInResult = await _userManager.CheckPasswordAsync(user, newPassword);
        signInResult.Should().BeTrue();
    }
}
