using System.Linq;
using AutoFixture;
using AutoFixture.Kernel;
using Domain.Models;
using FluentAssertions;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Moq;
using Xunit;

using Footex.UnitTests.Common;

namespace Footex.UnitTests.Infrastructure.Identity;

public class IdentityServiceUnitTests
{
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
    private readonly Mock<RoleManager<IdentityRole>> _mockRoleManager;
    private readonly IdentityService _identityService;
    private readonly Fixture _fixture;

    public IdentityServiceUnitTests()
    {
        _fixture = new NoRecursionFixture();

        // Customize entity creation to avoid problematic navigation properties
        _fixture.Customize<ApplicationUser>(composer =>
            composer
                .Without(u => u.FavoriteTeam)
                .Without(u => u.RefreshTokens)
                .Without(u => u.Notifications)
                .Without(u => u.Matches)
        );

        // Setup UserManager mock
        var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
        _mockUserManager = new Mock<UserManager<ApplicationUser>>(
            userStoreMock.Object,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!
        );

        // Setup RoleManager mock
        var roleStoreMock = new Mock<IRoleStore<IdentityRole>>();
        _mockRoleManager = new Mock<RoleManager<IdentityRole>>(
            roleStoreMock.Object,
            null!,
            null!,
            null!,
            null!
        );

        _identityService = new IdentityService(_mockUserManager.Object, _mockRoleManager.Object);
    }

    [Fact]
    public async Task CreateUserAsync_Succeeds_ReturnsSuccessResult()
    {
        // Arrange
        var user = _fixture
            .Build<ApplicationUser>()
            .With(u => u.Id, "user123")
            .With(u => u.UserName, "test@example.com")
            .With(u => u.Email, "test@example.com")
            .Create();
        const string password = "Password123!";

        var identityResult = IdentityResult.Success;
        _mockUserManager.Setup(m => m.CreateAsync(user, password)).ReturnsAsync(identityResult);

        // Act
        var result = await _identityService.CreateUserAsync(user, password);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.UserId.Should().Be("user123");
        result.result.Should().Be(identityResult);
        _mockUserManager.Verify(m => m.CreateAsync(user, password), Times.Once);
    }

    [Fact]
    public async Task CreateUserAsync_Fails_ReturnsFailureResult()
    {
        // Arrange
        var user = _fixture
            .Build<ApplicationUser>()
            .With(u => u.Id, "user123")
            .With(u => u.UserName, "test@example.com")
            .With(u => u.Email, "test@example.com")
            .Create();
        const string password = "weak";

        var identityErrors = new IdentityError[]
        {
            new() { Code = "PasswordTooShort", Description = "Password is too short" },
        };
        var identityResult = IdentityResult.Failed(identityErrors);
        _mockUserManager.Setup(m => m.CreateAsync(user, password)).ReturnsAsync(identityResult);

        // Act
        var result = await _identityService.CreateUserAsync(user, password);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.UserId.Should().Be("user123");
        result.result.Should().Be(identityResult);
        _mockUserManager.Verify(m => m.CreateAsync(user, password), Times.Once);
    }

    [Fact]
    public async Task DeleteUserAsync_UserExists_ReturnsTrue()
    {
        // Arrange
        var userId = "user123";
        var user = _fixture.Build<ApplicationUser>().With(u => u.Id, userId).Create();

        _mockUserManager.Setup(m => m.FindByIdAsync(userId)).ReturnsAsync(user);
        _mockUserManager.Setup(m => m.DeleteAsync(user)).ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _identityService.DeleteUserAsync(userId);

        // Assert
        result.Should().BeTrue();
        _mockUserManager.Verify(m => m.FindByIdAsync(userId), Times.Once);
        _mockUserManager.Verify(m => m.DeleteAsync(user), Times.Once);
    }

    [Fact]
    public async Task DeleteUserAsync_UserNotFound_ReturnsFalse()
    {
        // Arrange
        const string userId = "nonexistent";

        _mockUserManager.Setup(m => m.FindByIdAsync(userId)).ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _identityService.DeleteUserAsync(userId);

        // Assert
        result.Should().BeFalse();
        _mockUserManager.Verify(m => m.FindByIdAsync(userId), Times.Once);
        _mockUserManager.Verify(m => m.DeleteAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    [Fact]
    public async Task DeleteUserAsync_DeleteFails_ReturnsFalse()
    {
        // Arrange
        const string userId = "user123";
        var user = _fixture.Build<ApplicationUser>().With(u => u.Id, userId).Create();

        _mockUserManager.Setup(m => m.FindByIdAsync(userId)).ReturnsAsync(user);
        _mockUserManager
            .Setup(m => m.DeleteAsync(user))
            .ReturnsAsync(
                IdentityResult.Failed(
                    [new IdentityError { Code = "DeleteError", Description = "Cannot delete user" }]
                )
            );

        // Act
        var result = await _identityService.DeleteUserAsync(userId);

        // Assert
        result.Should().BeFalse();
        _mockUserManager.Verify(m => m.FindByIdAsync(userId), Times.Once);
        _mockUserManager.Verify(m => m.DeleteAsync(user), Times.Once);
    }

    [Fact]
    public async Task GetUserByIdAsync_UserExists_ReturnsUser()
    {
        // Arrange
        var userId = "user123";
        var user = _fixture.Build<ApplicationUser>().With(u => u.Id, userId).Create();

        _mockUserManager.Setup(m => m.FindByIdAsync(userId)).ReturnsAsync(user);

        // Act
        var result = await _identityService.GetUserByIdAsync(userId);

        // Assert
        result.Should().BeEquivalentTo(user);
        _mockUserManager.Verify(m => m.FindByIdAsync(userId), Times.Once);
    }

    [Fact]
    public async Task GetUserByIdAsync_UserNotFound_ReturnsNull()
    {
        // Arrange
        const string userId = "nonexistent";

        _mockUserManager.Setup(m => m.FindByIdAsync(userId)).ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _identityService.GetUserByIdAsync(userId);

        // Assert
        result.Should().BeNull();
        _mockUserManager.Verify(m => m.FindByIdAsync(userId), Times.Once);
    }

    [Fact]
    public async Task GetUserByEmailAsync_UserExists_ReturnsUser()
    {
        // Arrange
        const string email = "test@example.com";
        var user = _fixture.Build<ApplicationUser>().With(u => u.Email, email).Create();

        _mockUserManager.Setup(m => m.FindByEmailAsync(email)).ReturnsAsync(user);

        // Act
        var result = await _identityService.GetUserByEmailAsync(email);

        // Assert
        result.Should().BeEquivalentTo(user);
        _mockUserManager.Verify(m => m.FindByEmailAsync(email), Times.Once);
    }

    [Fact]
    public async Task CheckPasswordAsync_CorrectPassword_ReturnsTrue()
    {
        // Arrange
        var user = _fixture.Create<ApplicationUser>();
        const string password = "Password123!";

        _mockUserManager.Setup(m => m.CheckPasswordAsync(user, password)).ReturnsAsync(true);

        // Act
        var result = await _identityService.CheckPasswordAsync(user, password);

        // Assert
        result.Should().BeTrue();
        _mockUserManager.Verify(m => m.CheckPasswordAsync(user, password), Times.Once);
    }

    [Fact]
    public async Task AddUserToRoleAsync_RoleExists_AddsUserToRole()
    {
        // Arrange
        var user = _fixture.Create<ApplicationUser>();
        var role = "Admin";

        _mockRoleManager.Setup(m => m.RoleExistsAsync(role)).ReturnsAsync(true);
        _mockUserManager
            .Setup(m => m.AddToRoleAsync(user, role))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _identityService.AddUserToRoleAsync(user, role);

        // Assert
        result.Should().BeTrue();
        _mockRoleManager.Verify(m => m.RoleExistsAsync(role), Times.Once);
        _mockRoleManager.Verify(m => m.CreateAsync(It.IsAny<IdentityRole>()), Times.Never);
        _mockUserManager.Verify(m => m.AddToRoleAsync(user, role), Times.Once);
    }

    [Fact]
    public async Task AddUserToRoleAsync_RoleDoesNotExist_CreatesRoleAndAddsUser()
    {
        // Arrange
        var user = _fixture
            .Build<ApplicationUser>()
            .With(u => u.Id, "user123")
            .With(u => u.UserName, "newrole@example.com")
            .Create();
        const string role = "NewRole";

        _mockRoleManager.Setup(m => m.RoleExistsAsync(role)).ReturnsAsync(false);
        _mockRoleManager
            .Setup(m => m.CreateAsync(It.Is<IdentityRole>(r => r.Name == role)))
            .ReturnsAsync(IdentityResult.Success);
        _mockUserManager
            .Setup(m => m.AddToRoleAsync(user, role))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _identityService.AddUserToRoleAsync(user, role);

        // Assert
        result.Should().BeTrue();
        _mockRoleManager.Verify(m => m.RoleExistsAsync(role), Times.Once);
        _mockRoleManager.Verify(
            m => m.CreateAsync(It.Is<IdentityRole>(r => r.Name == role)),
            Times.Once
        );
        _mockUserManager.Verify(m => m.AddToRoleAsync(user, role), Times.Once);
    }

    [Fact]
    public async Task IsInRoleAsync_UserExistsAndInRole_ReturnsTrue()
    {
        // Arrange
        const string userId = "user123";
        const string role = "Admin";
        var user = _fixture.Build<ApplicationUser>().With(u => u.Id, userId).Create();

        _mockUserManager.Setup(m => m.FindByIdAsync(userId)).ReturnsAsync(user);
        _mockUserManager.Setup(m => m.IsInRoleAsync(user, role)).ReturnsAsync(true);

        // Act
        var result = await _identityService.IsInRoleAsync(userId, role);

        // Assert
        result.Should().BeTrue();
        _mockUserManager.Verify(m => m.FindByIdAsync(userId), Times.Once);
        _mockUserManager.Verify(m => m.IsInRoleAsync(user, role), Times.Once);
    }

    [Fact]
    public async Task IsInRoleAsync_UserExistsButNotInRole_ReturnsFalse()
    {
        // Arrange
        var userId = "user123";
        var role = "Admin";
        var user = _fixture.Build<ApplicationUser>().With(u => u.Id, userId).Create();

        _mockUserManager.Setup(m => m.FindByIdAsync(userId)).ReturnsAsync(user);
        _mockUserManager.Setup(m => m.IsInRoleAsync(user, role)).ReturnsAsync(false);

        // Act
        var result = await _identityService.IsInRoleAsync(userId, role);

        // Assert
        result.Should().BeFalse();
        _mockUserManager.Verify(m => m.FindByIdAsync(userId), Times.Once);
        _mockUserManager.Verify(m => m.IsInRoleAsync(user, role), Times.Once);
    }

    [Fact]
    public async Task IsInRoleAsync_UserDoesNotExist_ReturnsFalse()
    {
        // Arrange
        const string userId = "nonexistent";
        const string role = "Admin";

        _mockUserManager.Setup(m => m.FindByIdAsync(userId)).ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _identityService.IsInRoleAsync(userId, role);

        // Assert
        result.Should().BeFalse();
        _mockUserManager.Verify(m => m.FindByIdAsync(userId), Times.Once);
        _mockUserManager.Verify(
            m => m.IsInRoleAsync(It.IsAny<ApplicationUser>(), role),
            Times.Never
        );
    }
}
