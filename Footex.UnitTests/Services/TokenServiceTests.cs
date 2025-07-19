using System.Security.Claims;
using Application.Services;
using AutoFixture;
using Domain.Interfaces;
using Domain.Models;
using FluentAssertions;
using Footex.UnitTests.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Footex.UnitTests.Services;

public class TokenServiceTests
{
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly NoRecursionFixture _fixture;
    private readonly TokenService _tokenService;
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<IApplicationUserRepository> _userRepositoryMock;

    public TokenServiceTests()
    {
        _fixture = new NoRecursionFixture();
        _configurationMock = new Mock<IConfiguration>();
        var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            userStoreMock.Object,
            new Mock<IOptions<IdentityOptions>>().Object,
            new Mock<IPasswordHasher<ApplicationUser>>().Object,
            new IUserValidator<ApplicationUser>[0],
            new IPasswordValidator<ApplicationUser>[0],
            new Mock<ILookupNormalizer>().Object,
            new Mock<IdentityErrorDescriber>().Object,
            new Mock<IServiceProvider>().Object,
            new Mock<ILogger<UserManager<ApplicationUser>>>().Object
        );
        _userRepositoryMock = new Mock<IApplicationUserRepository>();

        _tokenService = new TokenService(
            _configurationMock.Object,
            _userManagerMock.Object,
            _userRepositoryMock.Object
        );

        _configurationMock
            .Setup(x => x["JWT:Secret"])
            .Returns("super-secret-key-that-is-long-enough");
    }

    [Fact]
    public async Task CreateTokenAsync_WhenUserHasNoClaims_ShouldCreateTokenAndAddClaims()
    {
        // Arrange
        var user = _fixture.Create<ApplicationUser>();
        user.UserName = "testuser";
        user.Email = "test@email.com";
        _userManagerMock.Setup(x => x.GetClaimsAsync(user)).ReturnsAsync(new List<Claim>());
        _userManagerMock
            .Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "User" });
        _userManagerMock
            .Setup(x => x.AddClaimsAsync(user, It.IsAny<IEnumerable<Claim>>()))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var token = await _tokenService.CreateTokenAsync(user);

        // Assert
        token.Should().NotBeNullOrEmpty();
        _userManagerMock.Verify(
            x => x.AddClaimsAsync(user, It.IsAny<IEnumerable<Claim>>()),
            Times.Once
        );
    }

    [Fact]
    public void GenerateRefreshToken_ShouldReturnValidRefreshToken()
    {
        // Arrange
        var ipAddress = "127.0.0.1";

        // Act
        var refreshToken = _tokenService.GenerateRefreshToken(ipAddress);

        // Assert
        refreshToken.Should().NotBeNull();
        refreshToken.Token.Should().NotBeNullOrEmpty();
        refreshToken.Expires.Should().BeAfter(DateTime.UtcNow);
        refreshToken.CreatedByIp.Should().Be(ipAddress);
    }

    [Fact]
    public async Task GenerateTokenAsync_ShouldReturnTokenAndRefreshToken()
    {
        // Arrange
        var user = _fixture.Create<ApplicationUser>();
        user.UserName = "testuser";
        user.Email = "test@email.com";
        var ipAddress = "127.0.0.1";
        _userManagerMock.Setup(x => x.GetClaimsAsync(user)).ReturnsAsync(new List<Claim>());
        _userManagerMock
            .Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "User" });

        // Act
        var (token, refreshToken) = await _tokenService.GenerateTokenAsync(user, ipAddress);

        // Assert
        token.Should().NotBeNullOrEmpty();
        refreshToken.Should().NotBeNull();
        _userRepositoryMock.Verify(x => x.AddRefreshTokenAsync(user, refreshToken), Times.Once);
    }

    [Fact]
    public async Task RefreshTokenAsync_WithValidToken_ShouldReturnNewTokens()
    {
        // Arrange
        var ipAddress = "127.0.0.1";
        var user = _fixture.Create<ApplicationUser>();
        user.UserName = "testuser";
        user.Email = "test@email.com";
        var oldRefreshToken = _tokenService.GenerateRefreshToken(ipAddress);
        oldRefreshToken.User = user;

        _userRepositoryMock
            .Setup(x => x.GetRefreshTokenAsync(oldRefreshToken.Token))
            .ReturnsAsync(oldRefreshToken);
        _userManagerMock.Setup(x => x.GetClaimsAsync(user)).ReturnsAsync(new List<Claim>());
        _userManagerMock
            .Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "User" });

        // Act
        var (newToken, newRefreshToken) = await _tokenService.RefreshTokenAsync(
            oldRefreshToken.Token,
            ipAddress
        );

        // Assert
        newToken.Should().NotBeNullOrEmpty();
        newRefreshToken.Should().NotBeNull();
        oldRefreshToken.Revoked.Should().NotBeNull();
        oldRefreshToken.ReplacedByToken.Should().Be(newRefreshToken.Token);
        _userRepositoryMock.Verify(x => x.AddRefreshTokenAsync(user, newRefreshToken), Times.Once);
    }

    [Fact]
    public async Task RevokeTokenAsync_ShouldCallRepository()
    {
        // Arrange
        var token = "some-token";
        var ipAddress = "127.0.0.1";

        // Act
        await _tokenService.RevokeTokenAsync(token, ipAddress);

        // Assert
        _userRepositoryMock.Verify(x => x.RevokeRefreshTokenAsync(token, ipAddress), Times.Once);
    }
}
