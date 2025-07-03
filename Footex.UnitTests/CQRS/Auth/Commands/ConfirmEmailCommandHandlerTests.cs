using Application.CQRS.Auth.Commands;
using AutoFixture;
using Domain.Interfaces;
using Domain.Models;
using FluentAssertions;
using Footex.UnitTests.Common;
using Microsoft.AspNetCore.Identity;
using Moq;
using Xunit;

namespace Footex.UnitTests.CQRS.Auth.Commands;

public class ConfirmEmailCommandHandlerTests
{
    private readonly NoRecursionFixture _fixture;
    private readonly ConfirmEmailCommandHandler _handler;
    private readonly Mock<IIdentityService> _mockIdentityService;
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
    private readonly Mock<IApplicationUserRepository> _mockUserRepository;

    public ConfirmEmailCommandHandlerTests()
    {
        _fixture = new NoRecursionFixture();
        _mockUserRepository = new Mock<IApplicationUserRepository>();
        _mockIdentityService = new Mock<IIdentityService>();

        // Create mock UserManager with required dependencies
        var mockUserStore = new Mock<IUserStore<ApplicationUser>>();
        _mockUserManager = new Mock<UserManager<ApplicationUser>>(
            mockUserStore.Object,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null
        );

        _handler = new ConfirmEmailCommandHandler(
            _mockUserRepository.Object,
            _mockIdentityService.Object,
            _mockUserManager.Object
        );
    }

    [Fact]
    public async Task Handle_ValidEmailConfirmation_ShouldReturnSuccess()
    {
        // Arrange
        var command = _fixture
            .Build<ConfirmEmailCommand>()
            .With(x => x.UserId, Guid.NewGuid().ToString())
            .With(x => x.Token, "valid-confirmation-token")
            .Create();

        var user = _fixture
            .Build<ApplicationUser>()
            .With(x => x.Id, command.UserId)
            .With(x => x.EmailConfirmed, false)
            .Create();

        _mockUserRepository.Setup(x => x.GetByIdAsync(command.UserId)).ReturnsAsync(user);

        _mockUserManager
            .Setup(x => x.ConfirmEmailAsync(user, command.Token))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Error.Should().BeNull();

        _mockUserRepository.Verify(x => x.GetByIdAsync(command.UserId), Times.Once);
        _mockUserManager.Verify(x => x.ConfirmEmailAsync(user, command.Token), Times.Once);
    }

    [Fact]
    public async Task Handle_UserNotFound_ShouldReturnError()
    {
        // Arrange
        var command = _fixture
            .Build<ConfirmEmailCommand>()
            .With(x => x.UserId, Guid.NewGuid().ToString())
            .With(x => x.Token, "valid-token")
            .Create();

        _mockUserRepository
            .Setup(x => x.GetByIdAsync(command.UserId))
            .ReturnsAsync((ApplicationUser)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("User not found.");

        _mockUserRepository.Verify(x => x.GetByIdAsync(command.UserId), Times.Once);
        _mockUserManager.Verify(
            x => x.ConfirmEmailAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_InvalidToken_ShouldReturnError()
    {
        // Arrange
        var command = _fixture
            .Build<ConfirmEmailCommand>()
            .With(x => x.UserId, Guid.NewGuid().ToString())
            .With(x => x.Token, "invalid-token")
            .Create();

        var user = _fixture.Build<ApplicationUser>().With(x => x.Id, command.UserId).Create();

        var identityErrors = new[]
        {
            new IdentityError { Code = "InvalidToken", Description = "Invalid token." },
        };

        _mockUserRepository.Setup(x => x.GetByIdAsync(command.UserId)).ReturnsAsync(user);

        _mockUserManager
            .Setup(x => x.ConfirmEmailAsync(user, command.Token))
            .ReturnsAsync(IdentityResult.Failed(identityErrors));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("Invalid token.");

        _mockUserRepository.Verify(x => x.GetByIdAsync(command.UserId), Times.Once);
        _mockUserManager.Verify(x => x.ConfirmEmailAsync(user, command.Token), Times.Once);
    }

    [Fact]
    public async Task Handle_ExpiredToken_ShouldReturnError()
    {
        // Arrange
        var command = _fixture
            .Build<ConfirmEmailCommand>()
            .With(x => x.UserId, Guid.NewGuid().ToString())
            .With(x => x.Token, "expired-token")
            .Create();

        var user = _fixture.Build<ApplicationUser>().With(x => x.Id, command.UserId).Create();

        var identityErrors = new[]
        {
            new IdentityError { Code = "InvalidToken", Description = "Invalid token." },
        };

        _mockUserRepository.Setup(x => x.GetByIdAsync(command.UserId)).ReturnsAsync(user);

        _mockUserManager
            .Setup(x => x.ConfirmEmailAsync(user, command.Token))
            .ReturnsAsync(IdentityResult.Failed(identityErrors));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("Invalid token.");
    }

    [Fact]
    public async Task Handle_MultipleErrors_ShouldConcatenateErrors()
    {
        // Arrange
        var command = _fixture
            .Build<ConfirmEmailCommand>()
            .With(x => x.UserId, Guid.NewGuid().ToString())
            .With(x => x.Token, "invalid-token")
            .Create();

        var user = _fixture.Build<ApplicationUser>().With(x => x.Id, command.UserId).Create();

        var identityErrors = new[]
        {
            new IdentityError { Code = "InvalidToken", Description = "Invalid token." },
            new IdentityError { Code = "TokenExpired", Description = "Token has expired." },
        };

        _mockUserRepository.Setup(x => x.GetByIdAsync(command.UserId)).ReturnsAsync(user);

        _mockUserManager
            .Setup(x => x.ConfirmEmailAsync(user, command.Token))
            .ReturnsAsync(IdentityResult.Failed(identityErrors));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("Invalid token., Token has expired.");
    }

    [Fact]
    public async Task Handle_ExceptionThrown_ShouldReturnError()
    {
        // Arrange
        var command = _fixture
            .Build<ConfirmEmailCommand>()
            .With(x => x.UserId, Guid.NewGuid().ToString())
            .Create();

        var exceptionMessage = "Database connection failed";
        _mockUserRepository
            .Setup(x => x.GetByIdAsync(command.UserId))
            .ThrowsAsync(new Exception(exceptionMessage));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be(exceptionMessage);

        _mockUserRepository.Verify(x => x.GetByIdAsync(command.UserId), Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Handle_InvalidUserId_ShouldReturnUserNotFound(string userId)
    {
        // Arrange
        var command = _fixture
            .Build<ConfirmEmailCommand>()
            .With(x => x.UserId, userId)
            .With(x => x.Token, "valid-token")
            .Create();

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync((ApplicationUser)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("User not found.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Handle_EmptyToken_ShouldStillCallUserManager(string token)
    {
        // Arrange
        var command = _fixture
            .Build<ConfirmEmailCommand>()
            .With(x => x.UserId, Guid.NewGuid().ToString())
            .With(x => x.Token, token)
            .Create();

        var user = _fixture.Build<ApplicationUser>().With(x => x.Id, command.UserId).Create();

        var identityErrors = new[]
        {
            new IdentityError { Code = "InvalidToken", Description = "Invalid token." },
        };

        _mockUserRepository.Setup(x => x.GetByIdAsync(command.UserId)).ReturnsAsync(user);

        _mockUserManager
            .Setup(x => x.ConfirmEmailAsync(user, token))
            .ReturnsAsync(IdentityResult.Failed(identityErrors));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("Invalid token.");

        _mockUserManager.Verify(x => x.ConfirmEmailAsync(user, token), Times.Once);
    }
}
