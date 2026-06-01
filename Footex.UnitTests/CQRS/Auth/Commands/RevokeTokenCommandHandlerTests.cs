using Application.CQRS.Auth.Commands;
using AutoFixture;
using Domain.Interfaces;
using FluentAssertions;
using Footex.UnitTests.Common;
using Moq;
using Xunit;

namespace Footex.UnitTests.CQRS.Auth.Commands;

public class RevokeTokenCommandHandlerTests
{
    private readonly NoRecursionFixture _fixture;
    private readonly RevokeTokenCommandHandler _handler;
    private readonly Mock<ITokenService> _mockTokenService;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;

    public RevokeTokenCommandHandlerTests()
    {
        _fixture = new NoRecursionFixture();
        _mockTokenService = new Mock<ITokenService>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();

        _handler = new RevokeTokenCommandHandler(_mockTokenService.Object, _mockUnitOfWork.Object);
    }

    [Fact]
    public async Task Handle_ValidRevokeTokenRequest_ShouldReturnSuccess()
    {
        // Arrange
        var command = _fixture
            .Build<RevokeTokenCommand>()
            .With(x => x.RefreshToken, "valid-refresh-token")
            .With(x => x.IpAddress, "192.168.1.1")
            .Create();

        _mockTokenService
            .Setup(x => x.RevokeTokenAsync(command.RefreshToken, command.IpAddress))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Error.Should().BeNull();

        _mockTokenService.Verify(
            x => x.RevokeTokenAsync(command.RefreshToken, command.IpAddress),
            Times.Once
        );
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_TokenServiceThrowsException_ShouldReturnError()
    {
        // Arrange
        var command = _fixture
            .Build<RevokeTokenCommand>()
            .With(x => x.RefreshToken, "invalid-token")
            .With(x => x.IpAddress, "192.168.1.1")
            .Create();

        var exceptionMessage = "Token not found or already revoked";
        _mockTokenService
            .Setup(x => x.RevokeTokenAsync(command.RefreshToken, command.IpAddress))
            .ThrowsAsync(new InvalidOperationException(exceptionMessage));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be(exceptionMessage);

        _mockTokenService.Verify(
            x => x.RevokeTokenAsync(command.RefreshToken, command.IpAddress),
            Times.Once
        );
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_SaveChangesThrowsException_ShouldReturnError()
    {
        // Arrange
        var command = _fixture
            .Build<RevokeTokenCommand>()
            .With(x => x.RefreshToken, "valid-token")
            .With(x => x.IpAddress, "192.168.1.1")
            .Create();

        var exceptionMessage = "Database save failed";
        _mockTokenService
            .Setup(x => x.RevokeTokenAsync(command.RefreshToken, command.IpAddress))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception(exceptionMessage));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be(exceptionMessage);

        _mockTokenService.Verify(
            x => x.RevokeTokenAsync(command.RefreshToken, command.IpAddress),
            Times.Once
        );
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Handle_InvalidRefreshToken_ShouldStillCallTokenService(string? refreshToken)
    {
        // Arrange
        var command = _fixture
            .Build<RevokeTokenCommand>()
            .With(x => x.RefreshToken, refreshToken)
            .With(x => x.IpAddress, "192.168.1.1")
            .Create();

        var exceptionMessage = "Invalid refresh token";
        _mockTokenService
            .Setup(x => x.RevokeTokenAsync(refreshToken!, command.IpAddress))
            .ThrowsAsync(new ArgumentException(exceptionMessage));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be(exceptionMessage);

        _mockTokenService.Verify(
            x => x.RevokeTokenAsync(refreshToken!, command.IpAddress),
            Times.Once
        );
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Handle_InvalidIpAddress_ShouldStillCallTokenService(string? ipAddress)
    {
        // Arrange
        var command = _fixture
            .Build<RevokeTokenCommand>()
            .With(x => x.RefreshToken, "valid-token")
            .With(x => x.IpAddress, ipAddress)
            .Create();

        _mockTokenService
            .Setup(x => x.RevokeTokenAsync(command.RefreshToken, ipAddress!))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Error.Should().BeNull();

        _mockTokenService.Verify(
            x => x.RevokeTokenAsync(command.RefreshToken, ipAddress!),
            Times.Once
        );
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_SecurityException_ShouldReturnError()
    {
        // Arrange
        var command = _fixture
            .Build<RevokeTokenCommand>()
            .With(x => x.RefreshToken, "suspicious-token")
            .With(x => x.IpAddress, "192.168.1.1")
            .Create();

        var exceptionMessage = "Security violation detected";
        _mockTokenService
            .Setup(x => x.RevokeTokenAsync(command.RefreshToken, command.IpAddress))
            .ThrowsAsync(new UnauthorizedAccessException(exceptionMessage));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be(exceptionMessage);

        _mockTokenService.Verify(
            x => x.RevokeTokenAsync(command.RefreshToken, command.IpAddress),
            Times.Once
        );
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_CancellationRequested_ShouldPassCancellationToken()
    {
        // Arrange
        var command = _fixture
            .Build<RevokeTokenCommand>()
            .With(x => x.RefreshToken, "valid-token")
            .With(x => x.IpAddress, "192.168.1.1")
            .Create();

        var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        _mockTokenService
            .Setup(x => x.RevokeTokenAsync(command.RefreshToken, command.IpAddress))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(cancellationToken)).ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();

        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(cancellationToken), Times.Once);
    }

    [Fact]
    public async Task Handle_AlreadyRevokedToken_ShouldReturnError()
    {
        // Arrange
        var command = _fixture
            .Build<RevokeTokenCommand>()
            .With(x => x.RefreshToken, "already-revoked-token")
            .With(x => x.IpAddress, "192.168.1.1")
            .Create();

        var exceptionMessage = "Token is already revoked";
        _mockTokenService
            .Setup(x => x.RevokeTokenAsync(command.RefreshToken, command.IpAddress))
            .ThrowsAsync(new InvalidOperationException(exceptionMessage));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be(exceptionMessage);

        _mockTokenService.Verify(
            x => x.RevokeTokenAsync(command.RefreshToken, command.IpAddress),
            Times.Once
        );
    }
}
