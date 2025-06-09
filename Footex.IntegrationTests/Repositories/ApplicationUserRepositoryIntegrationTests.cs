using Domain.Interfaces;
using Domain.Models;
using FluentAssertions;
using Footex.IntegrationTests.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Footex.IntegrationTests.Repositories;

[Collection("IntegrationTests")]
public class ApplicationUserRepositoryIntegrationTests : BaseIntegrationTest
{
    private readonly IApplicationUserRepository _userRepository;
    private readonly UserManager<ApplicationUser> _userManager;

    public ApplicationUserRepositoryIntegrationTests(FootexWebApplicationFactory factory) : base(factory)
    {
        _userRepository = ServiceProvider.GetRequiredService<IApplicationUserRepository>();
        _userManager = ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    }

    [Fact]
    public async Task GetByIdAsync_WithValidUserId_ShouldReturnUser()
    {
        // Arrange
        var user = await SeedUserAsync();

        // Act
        var result = await _userRepository.GetByIdAsync(user.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(user.Id);
        result.Email.Should().Be(user.Email);
        result.FirstName.Should().Be(user.FirstName);
        result.LastName.Should().Be(user.LastName);
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidUserId_ShouldReturnNull()
    {
        // Act
        var result = await _userRepository.GetByIdAsync("invalid-user-id");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByEmailAsync_WithValidEmail_ShouldReturnUser()
    {
        // Arrange
        var user = await SeedUserAsync();

        // Act
        var result = await _userRepository.GetByEmailAsync(user.Email!);

        // Assert
        result.Should().NotBeNull();
        result!.Email.Should().Be(user.Email);
        result.Id.Should().Be(user.Id);
    }

    [Fact]
    public async Task GetByEmailAsync_WithInvalidEmail_ShouldReturnNull()
    {
        // Act
        var result = await _userRepository.GetByEmailAsync("nonexistent@example.com");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByUsernameAsync_WithValidUsername_ShouldReturnUser()
    {
        // Arrange
        var user = await SeedUserAsync();

        // Act
        var result = await _userRepository.GetByUsernameAsync(user.UserName!);

        // Assert
        result.Should().NotBeNull();
        result!.UserName.Should().Be(user.UserName);
        result.Id.Should().Be(user.Id);
    }

    [Fact]
    public async Task GetByUsernameAsync_WithInvalidUsername_ShouldReturnNull()
    {
        // Act
        var result = await _userRepository.GetByUsernameAsync("nonexistentuser");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task CheckPasswordAsync_WithValidPassword_ShouldReturnTrue()
    {
        // Arrange
        var password = "TestPassword123!";
        var user = await SeedUserAsync(password: password);

        // Act
        var result = await _userRepository.CheckPasswordAsync(user, password);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CheckPasswordAsync_WithInvalidPassword_ShouldReturnFalse()
    {
        // Arrange
        var user = await SeedUserAsync(password: "TestPassword123!");

        // Act
        var result = await _userRepository.CheckPasswordAsync(user, "WrongPassword");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CheckPasswordAsync_WithNullUser_ShouldReturnFalse()
    {
        // Act
        var result = await _userRepository.CheckPasswordAsync(null, "TestPassword123!");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task AddToRoleAsync_WithValidUserAndRole_ShouldReturnTrue()
    {
        // Arrange
        var user = await SeedUserAsync();
        await SeedRoleAsync("TestRole");

        // Act
        var result = await _userRepository.AddToRoleAsync(user, "TestRole");

        // Assert
        result.Should().BeTrue();
        
        var userRoles = await _userRepository.GetUserRolesAsync(user);
        userRoles.Should().Contain("TestRole");
    }

    [Fact]
    public async Task AddToRoleAsync_WithNullUser_ShouldReturnFalse()
    {
        // Arrange
        await SeedRoleAsync("TestRole");

        // Act
        var result = await _userRepository.AddToRoleAsync(null, "TestRole");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetUserRolesAsync_WithUserHavingRoles_ShouldReturnRoles()
    {
        // Arrange
        var user = await SeedUserAsync();
        await SeedRoleAsync("Role1");
        await SeedRoleAsync("Role2");
        
        await _userRepository.AddToRoleAsync(user, "Role1");
        await _userRepository.AddToRoleAsync(user, "Role2");

        // Act
        var result = await _userRepository.GetUserRolesAsync(user);

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain("Role1");
        result.Should().Contain("Role2");
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetUserRolesAsync_WithUserHavingNoRoles_ShouldReturnEmptyCollection()
    {
        // Arrange
        var user = await SeedUserAsync();

        // Act
        var result = await _userRepository.GetUserRolesAsync(user);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUserRolesAsync_WithNullUser_ShouldReturnEmptyCollection()
    {
        // Act
        var result = await _userRepository.GetUserRolesAsync(null);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task AddRefreshTokenAsync_WithValidUserAndToken_ShouldPersistToken()
    {
        // Arrange
        var user = await SeedUserAsync();
        var refreshToken = new RefreshToken
        {
            Token = Guid.NewGuid().ToString(),
            JwtId = Guid.NewGuid().ToString(),
            UserId = user.Id
        };

        // Act
        await _userRepository.AddRefreshTokenAsync(user, refreshToken);

        // Assert
        var persistedToken = await Context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken.Token);
        
        persistedToken.Should().NotBeNull();
        persistedToken!.UserId.Should().Be(user.Id);
        persistedToken.Token.Should().Be(refreshToken.Token);
        persistedToken.Created.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        persistedToken.Expires.Should().BeCloseTo(DateTime.UtcNow.AddDays(7), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task GetRefreshTokenAsync_WithValidToken_ShouldReturnToken()
    {
        // Arrange
        var user = await SeedUserAsync();
        var tokenValue = Guid.NewGuid().ToString();
        var refreshToken = new RefreshToken
        {
            Token = tokenValue,
            JwtId = Guid.NewGuid().ToString(),
            UserId = user.Id,
            Created = DateTime.UtcNow,
            Expires = DateTime.UtcNow.AddDays(7)
        };

        Context.RefreshTokens.Add(refreshToken);
        await Context.SaveChangesAsync();

        // Act
        var result = await _userRepository.GetRefreshTokenAsync(tokenValue);

        // Assert
        result.Should().NotBeNull();
        result!.Token.Should().Be(tokenValue);
        result.UserId.Should().Be(user.Id);
        result.User.Should().NotBeNull();
        result.User!.Id.Should().Be(user.Id);
    }

    [Fact]
    public async Task GetRefreshTokenAsync_WithInvalidToken_ShouldReturnNull()
    {
        // Act
        var result = await _userRepository.GetRefreshTokenAsync("invalid-token");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsyncWithTeam_WithUserHavingFavoriteTeam_ShouldReturnUserWithTeam()
    {
        // Arrange
        var team = await SeedTeamAsync();
        var user = await SeedUserAsync(favoriteTeamId: team.Id);

        // Act
        var result = await _userRepository.GetByIdAsyncWithTeam(user.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(user.Id);
        result.FavoriteTeam.Should().NotBeNull();
        result.FavoriteTeam!.Id.Should().Be(team.Id);
    }

    [Fact]
    public async Task GetByIdAsyncWithTeam_WithUserHavingNoFavoriteTeam_ShouldReturnUserWithoutTeam()
    {
        // Arrange
        var user = await SeedUserAsync();

        // Act
        var result = await _userRepository.GetByIdAsyncWithTeam(user.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(user.Id);
        result.FavoriteTeam.Should().BeNull();
    }

    [Fact]
    public async Task HasRefreshTokensAsync_WithUserHavingTokens_ShouldReturnTrue()
    {
        // Arrange
        var user = await SeedUserAsync();
        var refreshToken = new RefreshToken
        {
            Token = Guid.NewGuid().ToString(),
            JwtId = Guid.NewGuid().ToString(),
            UserId = user.Id,
            Created = DateTime.UtcNow,
            Expires = DateTime.UtcNow.AddDays(7)
        };

        Context.RefreshTokens.Add(refreshToken);
        await Context.SaveChangesAsync();

        // Act
        var result = await _userRepository.HasRefreshTokensAsync(user.Id);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasRefreshTokensAsync_WithUserHavingNoTokens_ShouldReturnFalse()
    {
        // Arrange
        var user = await SeedUserAsync();

        // Act
        var result = await _userRepository.HasRefreshTokensAsync(user.Id);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task RevokeRefreshTokenAsync_WithValidToken_ShouldRevokeToken()
    {
        // Arrange
        var user = await SeedUserAsync();
        var tokenValue = Guid.NewGuid().ToString();
        var refreshToken = new RefreshToken
        {
            Token = tokenValue,
            JwtId = Guid.NewGuid().ToString(),
            UserId = user.Id,
            Created = DateTime.UtcNow,
            Expires = DateTime.UtcNow.AddDays(7)
        };

        Context.RefreshTokens.Add(refreshToken);
        await Context.SaveChangesAsync();

        var ipAddress = "192.168.1.1";

        // Act
        await _userRepository.RevokeRefreshTokenAsync(tokenValue, ipAddress);

        // Assert
        var revokedToken = await Context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == tokenValue);
        
        revokedToken.Should().NotBeNull();
        revokedToken!.Revoked.Should().NotBeNull();
        revokedToken.RevokedByIp.Should().Be(ipAddress);
        revokedToken.Revoked.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    private async Task<ApplicationUser> SeedUserAsync(
        string? email = null, 
        string? username = null, 
        string password = "TestPassword123!",
        int? favoriteTeamId = null)
    {
        var uniqueId = Guid.NewGuid().ToString()[..8];
        email ??= $"testuser{uniqueId}@example.com";
        username ??= $"testuser{uniqueId}";

        var user = new ApplicationUser
        {
            UserName = username,
            Email = email,
            FirstName = "Test",
            LastName = "User",
            Age = 25,
            Gender = "Male",
            EmailConfirmed = true,
            FavoriteTeamId = favoriteTeamId
        };

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"Failed to create user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        return user;
    }

    private async Task SeedRoleAsync(string roleName)
    {
        var roleManager = ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }

    private async Task<Team> SeedTeamAsync()
    {
        var team = new Team
        {
            Name = "Test Team " + Guid.NewGuid().ToString()[..8],
            ShortName = "TT",
            City = "Test City",
            Country = "Test Country",
            FoundationDate = DateTime.UtcNow.AddYears(-50),
            PrimaryColor = "#FF0000",
            SecondaryColor = "#0000FF"
        };

        Context.Teams.Add(team);
        await Context.SaveChangesAsync();
        return team;
    }
}
