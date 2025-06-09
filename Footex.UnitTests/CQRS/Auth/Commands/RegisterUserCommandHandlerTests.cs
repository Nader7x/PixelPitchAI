using Application.CQRS.Auth.Commands;
using Application.Mappers;
using AutoFixture;
using Domain.Interfaces;
using Domain.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Moq;
using Xunit;

namespace Footex.UnitTests.CQRS.Auth.Commands;

public class RegisterUserCommandHandlerTests
{
    private readonly Fixture _fixture;
    private readonly RegisterUserCommandHandler _handler;
    private readonly Mock<IIdentityService> _mockIdentityService;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<UserMapper> _mockUserMapper;

    public RegisterUserCommandHandlerTests()
    {
        _mockIdentityService = new Mock<IIdentityService>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockUserMapper = new Mock<UserMapper>();
        _fixture = new Fixture();

        _handler = new RegisterUserCommandHandler(
            _mockIdentityService.Object,
            _mockUnitOfWork.Object,
            _mockUserMapper.Object);
    }

    [Fact]
    public async Task Handle_ValidRegistration_ReturnsSuccessResponse()
    {
        // Arrange
        var command = _fixture.Create<RegisterUserCommand>();
        var user = _fixture.Create<ApplicationUser>();
        var userId = Guid.NewGuid().ToString();
        var identityResult = IdentityResult.Success;

        _mockUserMapper.Setup(x => x.ToUserFromRegister(command))
            .Returns(user);

        _mockIdentityService.Setup(x => x.CreateUserAsync(user, command.Password))
            .ReturnsAsync((true, userId, identityResult));

        _mockIdentityService.Setup(x => x.AddUserToRoleAsync(user, "User"))
            .ReturnsAsync(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.UserId.Should().Be(userId);
        result.Error.Should().BeNull();

        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockIdentityService.Verify(x => x.AddUserToRoleAsync(user, "User"), Times.Once);
    }

    [Fact]
    public async Task Handle_UserCreationFails_ReturnsFailureResponse()
    {
        // Arrange
        var command = _fixture.Create<RegisterUserCommand>();
        var user = _fixture.Create<ApplicationUser>();
        var errorMessage = "Email already exists";
        var identityErrors = new[] { new IdentityError { Description = errorMessage } };
        var identityResult = IdentityResult.Failed(identityErrors);

        _mockUserMapper.Setup(x => x.ToUserFromRegister(command))
            .Returns(user);

        _mockIdentityService.Setup(x => x.CreateUserAsync(user, command.Password))
            .ReturnsAsync((false, null, identityResult));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.UserId.Should().BeNull();
        result.Error.Should().Be(errorMessage);

        _mockIdentityService.Verify(x => x.AddUserToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()),
            Times.Never);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ExceptionThrown_ReturnsFailureResponse()
    {
        // Arrange
        var command = _fixture.Create<RegisterUserCommand>();
        var user = _fixture.Create<ApplicationUser>();

        _mockUserMapper.Setup(x => x.ToUserFromRegister(command))
            .Returns(user);

        _mockIdentityService.Setup(x => x.CreateUserAsync(user, command.Password))
            .ThrowsAsync(new Exception("Database connection failed"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.UserId.Should().BeNull();
        result.Error.Should().Be("Database connection failed");
    }

    [Fact]
    public async Task Handle_ValidRegistration_CallsUserMapperCorrectly()
    {
        // Arrange
        var command = _fixture.Create<RegisterUserCommand>();
        var user = _fixture.Create<ApplicationUser>();
        var userId = Guid.NewGuid().ToString();
        var identityResult = IdentityResult.Success;

        _mockUserMapper.Setup(x => x.ToUserFromRegister(command))
            .Returns(user);

        _mockIdentityService.Setup(x => x.CreateUserAsync(user, command.Password))
            .ReturnsAsync((true, userId, identityResult));

        _mockIdentityService.Setup(x => x.AddUserToRoleAsync(user, "User"))
            .ReturnsAsync(true);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockUserMapper.Verify(x => x.ToUserFromRegister(command), Times.Once);
    }
}