using Application.CQRS.Auth.Commands;
using Domain.Models;
using FluentAssertions;
using Footex.IntegrationTests.Common;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Footex.IntegrationTests.CQRS.Auth.Commands;

public class ResendEmailConfirmationCommandHandlerIntegrationTests : BaseIntegrationTest
{
    private readonly UserManager<ApplicationUser> _userManager;

    public ResendEmailConfirmationCommandHandlerIntegrationTests(
        FootexWebApplicationFactory factory
    )
        : base(factory)
    {
        _userManager = FactoryServiceScope.ServiceProvider.GetRequiredService<
            UserManager<ApplicationUser>
        >();
    }

    [Fact]
    public async Task Handle_ValidResendEmailConfirmation_Succeeds()
    {
        // Arrange
        const string password = "Password123!";
        var user = new ApplicationUser
        {
            FirstName = "Resend",
            LastName = "Confirmation",
            UserName = "resenduser",
            Email = "resend@example.com",
            EmailConfirmed = false,
        };
        await _userManager.CreateAsync(user, password);

        var command = new ResendEmailConfirmationCommand { Email = user.Email };

        // Act
        var result = await Mediator.Send(command);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Error.Should().BeNull();
        var updatedUser = await _userManager.FindByEmailAsync(user.Email);
        updatedUser.Should().NotBeNull();
        updatedUser?.EmailConfirmed.Should().BeFalse(); // Email should not be confirmed yet
    }
}
