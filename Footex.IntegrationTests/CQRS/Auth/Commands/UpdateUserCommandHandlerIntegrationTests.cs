using Application.CQRS.Auth.Commands;
using Domain.Models;
using FluentAssertions;
using Footex.IntegrationTests.Common;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Footex.IntegrationTests.CQRS.Auth.Commands;

public class UpdateUserCommandHandlerIntegrationTests : BaseIntegrationTest
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UpdateUserCommandHandlerIntegrationTests(FootexWebApplicationFactory factory)
        : base(factory)
    {
        _userManager = FactoryServiceScope.ServiceProvider.GetRequiredService<
            UserManager<ApplicationUser>
        >();
    }

    [Fact]
    public async Task Handle_ValidUpdateUser_Succeeds()
    {
        // Arrange
        var password = "Password123!";
        var user = new ApplicationUser
        {
            FirstName = "Update",
            LastName = "User",
            UserName = "updateuser",
            Email = "update@example.com",
            EmailConfirmed = true,
        };
        await _userManager.CreateAsync(user, password);

        var command = new UpdateUserCommand
        {
            Id = user.Id,
            FirstName = "UpdatedFirstName",
            LastName = "UpdatedLastName",
            UserName = "updatedusername",
        };

        // Act
        var result = await Mediator.Send(command);

        // Assert
        result.Succeeded.Should().BeTrue();
        var updatedUser = await _userManager.FindByIdAsync(user.Id);
        updatedUser?.FirstName.Should().Be("UpdatedFirstName");
        updatedUser?.LastName.Should().Be("UpdatedLastName");
        updatedUser?.UserName.Should().Be("updatedusername");
    }
}
