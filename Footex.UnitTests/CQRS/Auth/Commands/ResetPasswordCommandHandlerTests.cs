using Application.CQRS.Auth.Commands;
using AutoFixture;
using Domain.Interfaces;
using Domain.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Moq;
using Xunit;

namespace Footex.UnitTests.CQRS.Auth.Commands;

public class ResetPasswordCommandHandlerTests
{
    private readonly Mock<IIdentityService> _mockIdentityService;
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
    private readonly ResetPasswordCommandHandler _handler;
    private readonly Fixture _fixture;

    public ResetPasswordCommandHandlerTests()
    {
        _fixture = new Fixture();
        _mockIdentityService = new Mock<IIdentityService>();
        
        // Create mock UserManager with required dependencies
        var mockUserStore = new Mock<IUserStore<ApplicationUser>>();
        _mockUserManager = new Mock<UserManager<ApplicationUser>>(
            mockUserStore.Object, null, null, null, null, null, null, null, null);
        
        _handler = new ResetPasswordCommandHandler(_mockIdentityService.Object, _mockUserManager.Object);
    }

    [Fact]
    public async Task Handle_ValidResetPasswordRequest_ShouldReturnSuccess()
    {
        // Arrange
        var command = _fixture.Build<ResetPasswordCommand>()
            .With(x => x.Email, "test@example.com")
            .With(x => x.Token, "valid-token")
            .With(x => x.NewPassword, "NewPassword123!")
            .Create();

        var user = _fixture.Build<ApplicationUser>()
            .With(x => x.Email, command.Email)
            .Create();

        _mockIdentityService.Setup(x => x.GetUserByEmailAsync(command.Email))
            .ReturnsAsync(user);

        _mockUserManager.Setup(x => x.ResetPasswordAsync(user, command.Token, command.NewPassword))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Error.Should().BeNull();

        _mockIdentityService.Verify(x => x.GetUserByEmailAsync(command.Email), Times.Once);
        _mockUserManager.Verify(x => x.ResetPasswordAsync(user, command.Token, command.NewPassword), Times.Once);
    }

    [Fact]
    public async Task Handle_UserNotFound_ShouldReturnError()
    {
        // Arrange
        var command = _fixture.Build<ResetPasswordCommand>()
            .With(x => x.Email, "nonexistent@example.com")
            .Create();

        _mockIdentityService.Setup(x => x.GetUserByEmailAsync(command.Email))
            .ReturnsAsync((ApplicationUser)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("Invalid email address.");

        _mockIdentityService.Verify(x => x.GetUserByEmailAsync(command.Email), Times.Once);
        _mockUserManager.Verify(x => x.ResetPasswordAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_InvalidToken_ShouldReturnError()
    {
        // Arrange
        var command = _fixture.Build<ResetPasswordCommand>()
            .With(x => x.Email, "test@example.com")
            .With(x => x.Token, "invalid-token")
            .With(x => x.NewPassword, "NewPassword123!")
            .Create();

        var user = _fixture.Build<ApplicationUser>()
            .With(x => x.Email, command.Email)
            .Create();

        var identityErrors = new[]
        {
            new IdentityError { Code = "InvalidToken", Description = "Invalid token." }
        };

        _mockIdentityService.Setup(x => x.GetUserByEmailAsync(command.Email))
            .ReturnsAsync(user);

        _mockUserManager.Setup(x => x.ResetPasswordAsync(user, command.Token, command.NewPassword))
            .ReturnsAsync(IdentityResult.Failed(identityErrors));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("Invalid token.");

        _mockIdentityService.Verify(x => x.GetUserByEmailAsync(command.Email), Times.Once);
        _mockUserManager.Verify(x => x.ResetPasswordAsync(user, command.Token, command.NewPassword), Times.Once);
    }

    [Fact]
    public async Task Handle_WeakPassword_ShouldReturnError()
    {
        // Arrange
        var command = _fixture.Build<ResetPasswordCommand>()
            .With(x => x.Email, "test@example.com")
            .With(x => x.Token, "valid-token")
            .With(x => x.NewPassword, "weak")
            .Create();

        var user = _fixture.Build<ApplicationUser>()
            .With(x => x.Email, command.Email)
            .Create();

        var identityErrors = new[]
        {
            new IdentityError { Code = "PasswordTooShort", Description = "Passwords must be at least 6 characters." },
            new IdentityError { Code = "PasswordRequiresDigit", Description = "Passwords must have at least one digit." }
        };

        _mockIdentityService.Setup(x => x.GetUserByEmailAsync(command.Email))
            .ReturnsAsync(user);

        _mockUserManager.Setup(x => x.ResetPasswordAsync(user, command.Token, command.NewPassword))
            .ReturnsAsync(IdentityResult.Failed(identityErrors));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("Passwords must be at least 6 characters., Passwords must have at least one digit.");

        _mockIdentityService.Verify(x => x.GetUserByEmailAsync(command.Email), Times.Once);
        _mockUserManager.Verify(x => x.ResetPasswordAsync(user, command.Token, command.NewPassword), Times.Once);
    }

    [Fact]
    public async Task Handle_ExceptionThrown_ShouldReturnError()
    {
        // Arrange
        var command = _fixture.Build<ResetPasswordCommand>()
            .With(x => x.Email, "test@example.com")
            .Create();

        var exceptionMessage = "Database connection failed";
        _mockIdentityService.Setup(x => x.GetUserByEmailAsync(command.Email))
            .ThrowsAsync(new Exception(exceptionMessage));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be(exceptionMessage);

        _mockIdentityService.Verify(x => x.GetUserByEmailAsync(command.Email), Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Handle_InvalidEmail_ShouldReturnError(string email)
    {
        // Arrange
        var command = _fixture.Build<ResetPasswordCommand>()
            .With(x => x.Email, email)
            .Create();

        _mockIdentityService.Setup(x => x.GetUserByEmailAsync(email))
            .ReturnsAsync((ApplicationUser)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("Invalid email address.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Handle_InvalidToken_ShouldStillCallUserManager(string token)
    {
        // Arrange
        var command = _fixture.Build<ResetPasswordCommand>()
            .With(x => x.Email, "test@example.com")
            .With(x => x.Token, token)
            .With(x => x.NewPassword, "NewPassword123!")
            .Create();

        var user = _fixture.Build<ApplicationUser>()
            .With(x => x.Email, command.Email)
            .Create();

        var identityErrors = new[]
        {
            new IdentityError { Code = "InvalidToken", Description = "Invalid token." }
        };

        _mockIdentityService.Setup(x => x.GetUserByEmailAsync(command.Email))
            .ReturnsAsync(user);

        _mockUserManager.Setup(x => x.ResetPasswordAsync(user, token, command.NewPassword))
            .ReturnsAsync(IdentityResult.Failed(identityErrors));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("Invalid token.");

        _mockUserManager.Verify(x => x.ResetPasswordAsync(user, token, command.NewPassword), Times.Once);
    }
}
