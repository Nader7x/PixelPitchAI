using Application.CQRS.Auth.Commands;
using Application.Services;
using AutoFixture;
using Domain.Interfaces;
using Domain.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace Footex.UnitTests.CQRS.Auth.Commands;

public class ForgotPasswordCommandHandlerTests
{
    private readonly Mock<IIdentityService> _mockIdentityService;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
    private readonly ForgotPasswordCommandHandler _handler;
    private readonly Fixture _fixture;

    public ForgotPasswordCommandHandlerTests()
    {
        _mockIdentityService = new Mock<IIdentityService>();
        _mockEmailService = new Mock<IEmailService>();
        _mockConfiguration = new Mock<IConfiguration>();
        
        var userStore = new Mock<IUserStore<ApplicationUser>>();
        _mockUserManager = new Mock<UserManager<ApplicationUser>>(
            userStore.Object, null, null, null, null, null, null, null, null);
        
        _fixture = new Fixture();
        
        _handler = new ForgotPasswordCommandHandler(
            _mockIdentityService.Object,
            _mockEmailService.Object,
            _mockConfiguration.Object,
            _mockUserManager.Object);
    }

    [Fact]
    public async Task Handle_ValidEmailConfirmedUser_ReturnsSuccessResponse()
    {
        // Arrange
        var command = _fixture.Create<ForgotPasswordCommand>();
        var user = _fixture.Create<ApplicationUser>();
        var resetToken = "reset-token-123";
        var appUrl = "https://test-app.com";

        _mockIdentityService.Setup(x => x.GetUserByEmailAsync(command.Email))
            .ReturnsAsync(user);
        
        _mockUserManager.Setup(x => x.IsEmailConfirmedAsync(user))
            .ReturnsAsync(true);
        
        _mockUserManager.Setup(x => x.GeneratePasswordResetTokenAsync(user))
            .ReturnsAsync(resetToken);
        
        _mockConfiguration.Setup(x => x["AppUrl"])
            .Returns(appUrl);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Error.Should().BeNull();
        
        _mockEmailService.Verify(x => x.SendEmailAsync(
            user.Email,
            "Reset Your Password",
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UserNotFound_ReturnsSuccessResponse()
    {
        // Arrange
        var command = _fixture.Create<ForgotPasswordCommand>();

        _mockIdentityService.Setup(x => x.GetUserByEmailAsync(command.Email))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue(); // Don't reveal user doesn't exist
        result.Error.Should().BeNull();
        
        _mockEmailService.Verify(x => x.SendEmailAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_EmailNotConfirmed_ReturnsFailureResponse()
    {
        // Arrange
        var command = _fixture.Create<ForgotPasswordCommand>();
        var user = _fixture.Create<ApplicationUser>();

        _mockIdentityService.Setup(x => x.GetUserByEmailAsync(command.Email))
            .ReturnsAsync(user);
        
        _mockUserManager.Setup(x => x.IsEmailConfirmedAsync(user))
            .ReturnsAsync(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("Email is not confirmed. Please confirm your email before resetting your password.");
    }

    [Fact]
    public async Task Handle_ValidRequest_GeneratesCorrectResetLink()
    {
        // Arrange
        var command = new ForgotPasswordCommand { Email = "test@example.com" };
        var user = new ApplicationUser
        {
            Email = command.Email,
            FirstName = "FirstName",
            LastName = "LastName"
        };
        var resetToken = "reset-token-123";
        var appUrl = "https://test-app.com";
        
        string capturedEmailBody = null;

        _mockIdentityService.Setup(x => x.GetUserByEmailAsync(command.Email))
            .ReturnsAsync(user);
        
        _mockUserManager.Setup(x => x.IsEmailConfirmedAsync(user))
            .ReturnsAsync(true);
        
        _mockUserManager.Setup(x => x.GeneratePasswordResetTokenAsync(user))
            .ReturnsAsync(resetToken);
        
        _mockConfiguration.Setup(x => x["AppUrl"])
            .Returns(appUrl);

        _mockEmailService.Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Callback<string, string, string>((email, subject, body) => capturedEmailBody = body);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        capturedEmailBody.Should().NotBeNull();
        capturedEmailBody.Should().Contain($"{appUrl}/reset-password");
        capturedEmailBody.Should().Contain($"email={Uri.EscapeDataString(command.Email)}");
        capturedEmailBody.Should().Contain($"token={Uri.EscapeDataString(resetToken)}");
    }

    [Fact]
    public async Task Handle_ExceptionThrown_ReturnsFailureResponse()
    {
        // Arrange
        var command = _fixture.Create<ForgotPasswordCommand>();
        var errorMessage = "Email service unavailable";

        _mockIdentityService.Setup(x => x.GetUserByEmailAsync(command.Email))
            .ThrowsAsync(new Exception(errorMessage));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be(errorMessage);
    }
}
