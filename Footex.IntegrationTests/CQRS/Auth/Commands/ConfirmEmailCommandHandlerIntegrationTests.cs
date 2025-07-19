using Application.CQRS.Auth.Commands;
using Domain.Models;
using FluentAssertions;
using Footex.IntegrationTests.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Footex.IntegrationTests.CQRS.Auth.Commands;

public class ConfirmEmailCommandHandlerIntegrationTests : BaseIntegrationTest
{
    private readonly UserManager<ApplicationUser> _userManager;

    public ConfirmEmailCommandHandlerIntegrationTests(FootexWebApplicationFactory factory)
        : base(factory)
    {
        _userManager = FactoryServiceScope.ServiceProvider.GetRequiredService<
            UserManager<ApplicationUser>
        >();
    }

    [Fact]
    public async Task Handle_ValidConfirmation_Succeeds()
    {
        // Arrange
        var password = "Password123!";
        var user = new ApplicationUser
        {
            FirstName = "Test",
            LastName = "User",
            UserName = "testuser2",
            Email = "test2@example.com",
            EmailConfirmed = false,
        };
        await _userManager.CreateAsync(user, password);
        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

        var command = new ConfirmEmailCommand { UserId = user.Id, Token = token };

        // Act
        var result = await Mediator.Send(command);

        // Assert
        result.Succeeded.Should().BeTrue();
        var updatedUser = await _userManager.FindByIdAsync(user.Id);
        updatedUser?.EmailConfirmed.Should().BeTrue();
    }
}
