using Application.CQRS.Auth.Queries;
using Domain.Models;
using Footex.IntegrationTests.Common;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Footex.IntegrationTests.CQRS.Auth.Queries;

public class GetUserProfileQueryHandlerIntegrationTests : BaseIntegrationTest
{
    private readonly UserManager<ApplicationUser> _userManager;

    public GetUserProfileQueryHandlerIntegrationTests(FootexWebApplicationFactory factory)
        : base(factory)
    {
        _userManager = FactoryServiceScope.ServiceProvider.GetRequiredService<
            UserManager<ApplicationUser>
        >();
    }

    [Fact]
    public async Task Handle_ValidUserId_ReturnsUserProfile()
    {
        // Arrange
        var password = "Password123!";
        var user = new ApplicationUser
        {
            FirstName = "Profile",
            LastName = "User",
            UserName = "profileuser",
            Email = "profile@example.com",
            EmailConfirmed = true,
        };
        await _userManager.CreateAsync(user, password);

        var query = new GetUserProfileQuery { UserId = user.Id };

        // Act
        var result = await Mediator.Send(query);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(user.Id, result.UserId);
        Assert.Equal(user.Email, result.Email);
        Assert.Equal(user.UserName, result.Username);
        Assert.Equal(user.FirstName, result.FirstName);
        Assert.Equal(user.LastName, result.LastName);
    }
}
