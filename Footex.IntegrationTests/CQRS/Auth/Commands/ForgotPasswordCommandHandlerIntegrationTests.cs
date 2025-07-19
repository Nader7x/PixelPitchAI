using Application.CQRS.Auth.Commands;
using Domain.Models;
using FluentAssertions;
using Footex.IntegrationTests.Common;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Footex.IntegrationTests.CQRS.Auth.Commands;

public class ForgotPasswordCommandHandlerIntegrationTests : BaseIntegrationTest
{
    private readonly UserManager<ApplicationUser> _userManager;

    public ForgotPasswordCommandHandlerIntegrationTests(FootexWebApplicationFactory factory)
        : base(factory)
    {
        _userManager = FactoryServiceScope.ServiceProvider.GetRequiredService<
            UserManager<ApplicationUser>
        >();
    }

    [Fact]
    public async Task Handle_ValidForgotPassword_Succeeds()
    {
        // Arrange
        var password = "Password123!";
        var user = new ApplicationUser
        {
            FirstName = "Forgot",
            LastName = "Password",
            UserName = "forgotuser",
            Email = "forgot@example.com",
            EmailConfirmed = true,
        };
        await _userManager.CreateAsync(user, password);

        var command = new ForgotPasswordCommand { Email = user.Email };

        // Act
        var result = await Mediator.Send(command);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Error.Should().BeNull();
    }
}
