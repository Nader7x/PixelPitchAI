using Application.CQRS.Auth.Commands;
using AutoFixture;
using Domain.Interfaces;
using Domain.Models;
using FluentAssertions;
using Footex.UnitTests.Common;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Xunit;

namespace Footex.UnitTests.CQRS.Auth.Commands;

public class RefreshTokenCommandHandlerTests
{
    private readonly NoRecursionFixture _fixture;
    private readonly RefreshTokenCommandHandler _handler;
    private readonly Mock<ITokenService> _mockTokenService;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;

    public RefreshTokenCommandHandlerTests()
    {
        _mockTokenService = new Mock<ITokenService>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _fixture = new NoRecursionFixture();

        _handler = new RefreshTokenCommandHandler(_mockTokenService.Object, _mockUnitOfWork.Object);
    }

    [Fact]
    public async Task Handle_ValidRefreshToken_ReturnsSuccessResponse()
    {
        // Arrange
        var command = _fixture.Create<RefreshTokenCommand>();
        var newAccessToken = "new-access-token";
        var newRefreshToken = new RefreshToken
        {
            Token = "new-refresh-token",
            Expires = DateTime.UtcNow.AddDays(7),
        };

        _mockTokenService
            .Setup(x => x.RefreshTokenAsync(command.RefreshToken, command.IpAddress))
            .ReturnsAsync((newAccessToken, newRefreshToken));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.AccessToken.Should().Be(newAccessToken);
        result.RefreshToken.Should().Be(newRefreshToken.Token);
        result
            .TokenExpires.Should()
            .BeCloseTo(DateTime.Now.AddMinutes(60), TimeSpan.FromMinutes(1));

        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_InvalidRefreshToken_ReturnsFailureResponse()
    {
        // Arrange
        var command = _fixture.Create<RefreshTokenCommand>();
        var errorMessage = "Invalid refresh token";

        _mockTokenService
            .Setup(x => x.RefreshTokenAsync(command.RefreshToken, command.IpAddress))
            .ThrowsAsync(new SecurityTokenException(errorMessage));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be(errorMessage);
        result.AccessToken.Should().BeNull();
        result.RefreshToken.Should().BeNull();
    }

    [Fact]
    public async Task Handle_TokenServiceThrowsException_ReturnsFailureResponse()
    {
        // Arrange
        var command = _fixture.Create<RefreshTokenCommand>();
        var errorMessage = "Token service unavailable";

        _mockTokenService
            .Setup(x => x.RefreshTokenAsync(command.RefreshToken, command.IpAddress))
            .ThrowsAsync(new Exception(errorMessage));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be(errorMessage);
    }

    [Fact]
    public async Task Handle_ValidRefreshToken_CallsTokenServiceWithCorrectParameters()
    {
        // Arrange
        var command = _fixture.Create<RefreshTokenCommand>();
        var newAccessToken = "new-access-token";
        var newRefreshToken = new RefreshToken
        {
            Token = "new-refresh-token",
            Expires = DateTime.UtcNow.AddDays(7),
        };

        _mockTokenService
            .Setup(x => x.RefreshTokenAsync(command.RefreshToken, command.IpAddress))
            .ReturnsAsync((newAccessToken, newRefreshToken));

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockTokenService.Verify(
            x => x.RefreshTokenAsync(command.RefreshToken, command.IpAddress),
            Times.Once
        );
    }
}
