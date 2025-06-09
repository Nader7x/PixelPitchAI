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

public class ResendEmailConfirmationCommandHandlerTests
{
    private readonly Fixture _fixture;
    private readonly ResendEmailConfirmationCommandHandler _handler;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<IIdentityService> _mockIdentityService;
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;

    public ResendEmailConfirmationCommandHandlerTests()
    {
        _fixture = new Fixture();
        _mockIdentityService = new Mock<IIdentityService>();
        _mockEmailService = new Mock<IEmailService>();
        _mockConfiguration = new Mock<IConfiguration>();

        // Create mock UserManager with required dependencies
        var mockUserStore = new Mock<IUserStore<ApplicationUser>>();
        _mockUserManager = new Mock<UserManager<ApplicationUser>>(
            mockUserStore.Object, null, null, null, null, null, null, null, null);

        _handler = new ResendEmailConfirmationCommandHandler(
            _mockIdentityService.Object,
            _mockEmailService.Object,
            _mockConfiguration.Object,
            _mockUserManager.Object);
    }

    [Fact]
    public async Task Handle_ValidEmailNotConfirmed_ShouldSendEmailAndReturnSuccess()
    {
        // Arrange
        var command = _fixture.Build<ResendEmailConfirmationCommand>()
            .With(x => x.Email, "test@example.com")
            .Create();

        var user = _fixture.Build<ApplicationUser>()
            .With(x => x.Email, command.Email)
            .With(x => x.Id, Guid.NewGuid().ToString())
            .With(x => x.EmailConfirmed, false)
            .Create();

        var token = "confirmation-token";
        var appUrl = "https://footex.ai";

        _mockIdentityService.Setup(x => x.GetUserByEmailAsync(command.Email))
            .ReturnsAsync(user);

        _mockUserManager.Setup(x => x.IsEmailConfirmedAsync(user))
            .ReturnsAsync(false);

        _mockUserManager.Setup(x => x.GenerateEmailConfirmationTokenAsync(user))
            .ReturnsAsync(token);

        _mockConfiguration.Setup(x => x["AppUrl"])
            .Returns(appUrl);

        _mockEmailService.Setup(x => x.SendEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Error.Should().BeNull();

        _mockIdentityService.Verify(x => x.GetUserByEmailAsync(command.Email), Times.Once);
        _mockUserManager.Verify(x => x.IsEmailConfirmedAsync(user), Times.Once);
        _mockUserManager.Verify(x => x.GenerateEmailConfirmationTokenAsync(user), Times.Once);
        _mockEmailService.Verify(x => x.SendEmailAsync(
                user.Email,
                "Confirm Your Email",
                It.Is<string>(content => content.Contains($"userId={user.Id}") && content.Contains("token="))),
            Times.Once);
    }

    [Fact]
    public async Task Handle_UserNotFound_ShouldReturnSuccessWithoutSendingEmail()
    {
        // Arrange
        var command = _fixture.Build<ResendEmailConfirmationCommand>()
            .With(x => x.Email, "nonexistent@example.com")
            .Create();

        _mockIdentityService.Setup(x => x.GetUserByEmailAsync(command.Email))
            .ReturnsAsync((ApplicationUser)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Error.Should().BeNull();

        _mockIdentityService.Verify(x => x.GetUserByEmailAsync(command.Email), Times.Once);
        _mockUserManager.Verify(x => x.IsEmailConfirmedAsync(It.IsAny<ApplicationUser>()), Times.Never);
        _mockEmailService.Verify(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_EmailAlreadyConfirmed_ShouldReturnError()
    {
        // Arrange
        var command = _fixture.Build<ResendEmailConfirmationCommand>()
            .With(x => x.Email, "confirmed@example.com")
            .Create();

        var user = _fixture.Build<ApplicationUser>()
            .With(x => x.Email, command.Email)
            .With(x => x.EmailConfirmed, true)
            .Create();

        _mockIdentityService.Setup(x => x.GetUserByEmailAsync(command.Email))
            .ReturnsAsync(user);

        _mockUserManager.Setup(x => x.IsEmailConfirmedAsync(user))
            .ReturnsAsync(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("Email is already confirmed.");

        _mockIdentityService.Verify(x => x.GetUserByEmailAsync(command.Email), Times.Once);
        _mockUserManager.Verify(x => x.IsEmailConfirmedAsync(user), Times.Once);
        _mockUserManager.Verify(x => x.GenerateEmailConfirmationTokenAsync(It.IsAny<ApplicationUser>()), Times.Never);
        _mockEmailService.Verify(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_DefaultAppUrl_ShouldUseDefaultUrl()
    {
        // Arrange
        var command = _fixture.Build<ResendEmailConfirmationCommand>()
            .With(x => x.Email, "test@example.com")
            .Create();

        var user = _fixture.Build<ApplicationUser>()
            .With(x => x.Email, command.Email)
            .With(x => x.Id, Guid.NewGuid().ToString())
            .Create();

        var token = "confirmation-token";

        _mockIdentityService.Setup(x => x.GetUserByEmailAsync(command.Email))
            .ReturnsAsync(user);

        _mockUserManager.Setup(x => x.IsEmailConfirmedAsync(user))
            .ReturnsAsync(false);

        _mockUserManager.Setup(x => x.GenerateEmailConfirmationTokenAsync(user))
            .ReturnsAsync(token);

        _mockConfiguration.Setup(x => x["AppUrl"])
            .Returns((string)null); // No app URL configured

        _mockEmailService.Setup(x => x.SendEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();

        _mockEmailService.Verify(x => x.SendEmailAsync(
                user.Email,
                "Confirm Your Email",
                It.Is<string>(content => content.Contains("https://Footex.AI"))),
            Times.Once);
    }

    [Fact]
    public async Task Handle_EmailServiceThrowsException_ShouldReturnError()
    {
        // Arrange
        var command = _fixture.Build<ResendEmailConfirmationCommand>()
            .With(x => x.Email, "test@example.com")
            .Create();

        var user = _fixture.Build<ApplicationUser>()
            .With(x => x.Email, command.Email)
            .With(x => x.Id, Guid.NewGuid().ToString())
            .Create();

        var token = "confirmation-token";
        var exceptionMessage = "Email service unavailable";

        _mockIdentityService.Setup(x => x.GetUserByEmailAsync(command.Email))
            .ReturnsAsync(user);

        _mockUserManager.Setup(x => x.IsEmailConfirmedAsync(user))
            .ReturnsAsync(false);

        _mockUserManager.Setup(x => x.GenerateEmailConfirmationTokenAsync(user))
            .ReturnsAsync(token);

        _mockConfiguration.Setup(x => x["AppUrl"])
            .Returns("https://footex.ai");

        _mockEmailService.Setup(x => x.SendEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ThrowsAsync(new Exception(exceptionMessage));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be(exceptionMessage);
    }

    [Fact]
    public async Task Handle_TokenGenerationFails_ShouldReturnError()
    {
        // Arrange
        var command = _fixture.Build<ResendEmailConfirmationCommand>()
            .With(x => x.Email, "test@example.com")
            .Create();

        var user = _fixture.Build<ApplicationUser>()
            .With(x => x.Email, command.Email)
            .Create();

        var exceptionMessage = "Token generation failed";

        _mockIdentityService.Setup(x => x.GetUserByEmailAsync(command.Email))
            .ReturnsAsync(user);

        _mockUserManager.Setup(x => x.IsEmailConfirmedAsync(user))
            .ReturnsAsync(false);

        _mockUserManager.Setup(x => x.GenerateEmailConfirmationTokenAsync(user))
            .ThrowsAsync(new InvalidOperationException(exceptionMessage));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be(exceptionMessage);

        _mockEmailService.Verify(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_UserEmailIsNull_ShouldNotSendEmail()
    {
        // Arrange
        var command = _fixture.Build<ResendEmailConfirmationCommand>()
            .With(x => x.Email, "test@example.com")
            .Create();

        var user = _fixture.Build<ApplicationUser>()
            .Without(x => x.Email) // User has null email
            .With(x => x.Id, Guid.NewGuid().ToString())
            .Create();

        var token = "confirmation-token";

        _mockIdentityService.Setup(x => x.GetUserByEmailAsync(command.Email))
            .ReturnsAsync(user);

        _mockUserManager.Setup(x => x.IsEmailConfirmedAsync(user))
            .ReturnsAsync(false);

        _mockUserManager.Setup(x => x.GenerateEmailConfirmationTokenAsync(user))
            .ReturnsAsync(token);

        _mockConfiguration.Setup(x => x["AppUrl"])
            .Returns("https://footex.ai");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();

        _mockEmailService.Verify(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Handle_InvalidEmail_ShouldReturnSuccessWithoutProcessing(string email)
    {
        // Arrange
        var command = _fixture.Build<ResendEmailConfirmationCommand>()
            .With(x => x.Email, email)
            .Create();

        _mockIdentityService.Setup(x => x.GetUserByEmailAsync(email))
            .ReturnsAsync((ApplicationUser)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Error.Should().BeNull();

        _mockIdentityService.Verify(x => x.GetUserByEmailAsync(email), Times.Once);
    }

    [Fact]
    public async Task Handle_ConfirmationLinkContainsCorrectFormat_ShouldFormatProperly()
    {
        // Arrange
        var command = _fixture.Build<ResendEmailConfirmationCommand>()
            .With(x => x.Email, "test@example.com")
            .Create();

        var userId = Guid.NewGuid().ToString();
        var user = _fixture.Build<ApplicationUser>()
            .With(x => x.Email, command.Email)
            .With(x => x.Id, userId)
            .Create();

        var token = "token+with/special&characters=";
        var appUrl = "https://footex.ai";
        var expectedEncodedToken = Uri.EscapeDataString(token);

        _mockIdentityService.Setup(x => x.GetUserByEmailAsync(command.Email))
            .ReturnsAsync(user);

        _mockUserManager.Setup(x => x.IsEmailConfirmedAsync(user))
            .ReturnsAsync(false);

        _mockUserManager.Setup(x => x.GenerateEmailConfirmationTokenAsync(user))
            .ReturnsAsync(token);

        _mockConfiguration.Setup(x => x["AppUrl"])
            .Returns(appUrl);

        _mockEmailService.Setup(x => x.SendEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();

        _mockEmailService.Verify(x => x.SendEmailAsync(
                user.Email,
                "Confirm Your Email",
                It.Is<string>(content =>
                    content.Contains($"userId={userId}") &&
                    content.Contains($"token={expectedEncodedToken}"))),
            Times.Once);
    }
}