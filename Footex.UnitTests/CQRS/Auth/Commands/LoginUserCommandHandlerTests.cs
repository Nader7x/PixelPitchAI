using Application.CQRS.Auth.Commands;
using Application.Interfaces;
using AutoFixture;
using Domain.Interfaces;
using Domain.Models;
using FluentAssertions;
using Moq;
using Xunit;

namespace Footex.UnitTests.CQRS.Auth.Commands;

public class LoginUserCommandHandlerTests
{
    private readonly Fixture _fixture;
    private readonly LoginUserCommandHandler _handler;
    private readonly Mock<ITokenService> _mockTokenService;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IApplicationUserRepository> _mockUserRepository;

    public LoginUserCommandHandlerTests()
    {
        _mockUserRepository = new Mock<IApplicationUserRepository>();
        _mockTokenService = new Mock<ITokenService>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        var mockIUserMapper = new Mock<IUserMapper>();
        _fixture = new Fixture();

        _handler = new LoginUserCommandHandler(
            _mockUserRepository.Object,
            _mockTokenService.Object,
            _mockUnitOfWork.Object,
            mockIUserMapper.Object);
    }

    [Fact]
    public async Task Handle_ValidCredentials_ReturnsSuccessResponse()
    {
        // Arrange
        var command = _fixture.Create<LoginUserCommand>();
        var user = _fixture.Create<ApplicationUser>();
        var roles = new List<string> { "User" };
        var refreshToken = new RefreshToken
        {
            Token = "refresh-token",
            Expires = DateTime.UtcNow.AddDays(7)
        };

        _mockUserRepository.Setup(x => x.GetByEmailAsync(command.Email))
            .ReturnsAsync(user);

        _mockUserRepository.Setup(x => x.CheckPasswordAsync(user, command.Password))
            .ReturnsAsync(true);

        _mockUserRepository.Setup(x => x.GetUserRolesAsync(user))
            .ReturnsAsync(roles);

        _mockTokenService.Setup(x => x.GenerateTokenAsync(user, command.IpAddress))
            .ReturnsAsync(("access-token", refreshToken));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.UserId.Should().Be(user.Id);
        result.Email.Should().Be(user.Email);
        result.AccessToken.Should().Be("access-token");
        result.RefreshToken.Should().Be("refresh-token");
        result.Roles.Should().BeEquivalentTo(roles);

        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UserNotFound_ReturnsFailureResponse()
    {
        // Arrange
        var command = _fixture.Create<LoginUserCommand>();

        _mockUserRepository.Setup(x => x.GetByEmailAsync(command.Email))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("Invalid email or password");
    }

    [Fact]
    public async Task Handle_InvalidPassword_ReturnsFailureResponse()
    {
        // Arrange
        var command = _fixture.Create<LoginUserCommand>();
        var user = _fixture.Create<ApplicationUser>();

        _mockUserRepository.Setup(x => x.GetByEmailAsync(command.Email))
            .ReturnsAsync(user);

        _mockUserRepository.Setup(x => x.CheckPasswordAsync(user, command.Password))
            .ReturnsAsync(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("Invalid email or password");
    }

    [Fact]
    public async Task Handle_ExceptionThrown_ReturnsFailureResponse()
    {
        // Arrange
        var command = _fixture.Create<LoginUserCommand>();

        _mockUserRepository.Setup(x => x.GetByEmailAsync(command.Email))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("Database error");
    }

    [Fact]
    public async Task Handle_ValidLogin_UpdatesLastLoginTime()
    {
        // Arrange
        var command = _fixture.Create<LoginUserCommand>();
        var user = _fixture.Create<ApplicationUser>();
        var oldLastLogin = user.LastLogin;
        var roles = new List<string> { "User" };
        var refreshToken = new RefreshToken
        {
            Token = "refresh-token",
            Expires = DateTime.UtcNow.AddDays(7)
        };

        _mockUserRepository.Setup(x => x.GetByEmailAsync(command.Email))
            .ReturnsAsync(user);

        _mockUserRepository.Setup(x => x.CheckPasswordAsync(user, command.Password))
            .ReturnsAsync(true);

        _mockUserRepository.Setup(x => x.GetUserRolesAsync(user))
            .ReturnsAsync(roles);

        _mockTokenService.Setup(x => x.GenerateTokenAsync(user, command.IpAddress))
            .ReturnsAsync(("access-token", refreshToken));

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        user.LastLogin.Should().BeAfter(oldLastLogin ?? DateTime.MinValue);
        user.LastLogin.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}
