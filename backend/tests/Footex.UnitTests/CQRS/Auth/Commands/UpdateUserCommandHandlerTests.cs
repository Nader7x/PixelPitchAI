using Application.CQRS.Auth.Commands;
using Application.Interfaces;
using AutoFixture;
using Domain.Interfaces;
using Domain.Models;
using FluentAssertions;
using Footex.UnitTests.Common;
using Microsoft.AspNetCore.Identity;
using Moq;
using Xunit;

namespace Footex.UnitTests.CQRS.Auth.Commands;

public class UpdateUserCommandHandlerTests
{
    private readonly NoRecursionFixture _fixture;
    private readonly UpdateUserCommandHandler _handler;
    private readonly Mock<IApplicationUserRepository> _mockApplicationUserRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
    private readonly Mock<IUserMapper> _mockUserMapper;

    public UpdateUserCommandHandlerTests()
    {
        _fixture = new NoRecursionFixture();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockApplicationUserRepository = new Mock<IApplicationUserRepository>();
        _mockUserMapper = new Mock<IUserMapper>();

        // Create mock UserManager with required dependencies
        var mockUserStore = new Mock<IUserStore<ApplicationUser>>();
        _mockUserManager = new Mock<UserManager<ApplicationUser>>(
            mockUserStore.Object,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!
        );

        // Setup UnitOfWork to return the mock repository
        _mockUnitOfWork
            .Setup(x => x.ApplicationUser)
            .Returns(_mockApplicationUserRepository.Object);

        _handler = new UpdateUserCommandHandler(
            _mockUnitOfWork.Object,
            _mockUserManager.Object,
            _mockUserMapper.Object
        );
    }

    [Fact]
    public async Task Handle_ValidUpdateRequest_ShouldReturnSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var command = _fixture
            .Build<UpdateUserCommand>()
            .With(x => x.Id, userId)
            .With(x => x.FirstName, "John")
            .With(x => x.LastName, "Doe")
            .With(x => x.Email, "john.doe@example.com")
            .With(x => x.PhoneNumber, "+1234567890")
            .With(x => x.Age, 30)
            .With(x => x.Gender, "Male")
            .With(x => x.ImageUrl, "https://example.com/image.jpg")
            .Without(x => x.CurrentPassword)
            .Without(x => x.NewPassword)
            .Create();

        var user = _fixture
            .Build<ApplicationUser>()
            .With(x => x.Id, userId)
            .With(x => x.FirstName, "OldFirst")
            .With(x => x.LastName, "OldLast")
            .Create();

        _mockApplicationUserRepository.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);

        _mockUnitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _mockUserMapper
            .Setup(x =>
                x.UpdateUserFromCommand(It.IsAny<UpdateUserCommand>(), It.IsAny<ApplicationUser>())
            )
            .Callback<UpdateUserCommand, ApplicationUser>(
                (updateUserCommand, applicationUser) =>
                {
                    if (updateUserCommand.FirstName != null)
                        applicationUser.FirstName = updateUserCommand.FirstName;
                    if (updateUserCommand.LastName != null)
                        applicationUser.LastName = updateUserCommand.LastName;
                    if (updateUserCommand.Gender != null)
                        applicationUser.Gender = updateUserCommand.Gender;
                    if (updateUserCommand.Age != null)
                        applicationUser.Age = updateUserCommand.Age.Value;
                    if (updateUserCommand.ImageUrl != null)
                        applicationUser.ImageUrl = updateUserCommand.ImageUrl;
                    applicationUser.Id = updateUserCommand.Id;
                    if (updateUserCommand.UserName != null)
                        applicationUser.UserName = updateUserCommand.UserName;
                    if (updateUserCommand.Email != null)
                        applicationUser.Email = updateUserCommand.Email;
                    if (updateUserCommand.PhoneNumber != null)
                        applicationUser.PhoneNumber = updateUserCommand.PhoneNumber;
                }
            );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.NotFound.Should().BeFalse();
        result.Error.Should().BeNull();
        result.ImageUrl.Should().Be(command.ImageUrl);

        // Verify user properties were updated
        user.FirstName.Should().Be(command.FirstName);
        user.LastName.Should().Be(command.LastName);
        user.Email.Should().Be(command.Email);
        user.PhoneNumber.Should().Be(command.PhoneNumber);
        user.Age.Should().Be(command.Age);
        user.Gender.Should().Be(command.Gender);
        user.ImageUrl.Should().Be(command.ImageUrl);

        _mockApplicationUserRepository.Verify(x => x.GetByIdAsync(userId), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UserNotFound_ShouldReturnNotFoundError()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var command = _fixture.Build<UpdateUserCommand>().With(x => x.Id, userId).Create();

        _mockApplicationUserRepository
            .Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.NotFound.Should().BeTrue();
        result.Error.Should().Be($"User with ID {userId} not found");

        _mockApplicationUserRepository.Verify(x => x.GetByIdAsync(userId), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithPasswordChange_ShouldChangePassword()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var command = _fixture
            .Build<UpdateUserCommand>()
            .With(x => x.Id, userId)
            .With(x => x.CurrentPassword, "OldPassword123!")
            .With(x => x.NewPassword, "NewPassword123!")
            .Create();

        var user = _fixture.Build<ApplicationUser>().With(x => x.Id, userId).Create();

        _mockApplicationUserRepository.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);

        _mockUserManager
            .Setup(x => x.ChangePasswordAsync(user, command.CurrentPassword!, command.NewPassword!))
            .ReturnsAsync(IdentityResult.Success);

        _mockUnitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();

        _mockUserManager.Verify(
            x => x.ChangePasswordAsync(user, command.CurrentPassword!, command.NewPassword!),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_WithOnlyCurrentPassword_ShouldNotChangePassword()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var command = _fixture
            .Build<UpdateUserCommand>()
            .With(x => x.Id, userId)
            .With(x => x.CurrentPassword, "OldPassword123!")
            .Without(x => x.NewPassword)
            .Create();

        var user = _fixture.Build<ApplicationUser>().With(x => x.Id, userId).Create();

        _mockApplicationUserRepository.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);

        _mockUnitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();

        _mockUserManager.Verify(
            x =>
                x.ChangePasswordAsync(
                    It.IsAny<ApplicationUser>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()
                ),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_WithEmptyStringValues_ShouldNotUpdateFields()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var originalFirstName = "OriginalFirst";
        var originalLastName = "OriginalLast";

        var command = _fixture
            .Build<UpdateUserCommand>()
            .With(x => x.Id, userId)
            .With(x => x.FirstName, "")
            .With(x => x.LastName, "")
            .With(x => x.Email, "")
            .With(x => x.PhoneNumber, "")
            .With(x => x.Gender, "")
            .With(x => x.ImageUrl, "")
            .Create();

        var user = _fixture
            .Build<ApplicationUser>()
            .With(x => x.Id, userId)
            .With(x => x.FirstName, originalFirstName)
            .With(x => x.LastName, originalLastName)
            .Create();

        _mockApplicationUserRepository.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);

        _mockUnitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();

        // Verify original values were not changed
        user.FirstName.Should().Be(originalFirstName);
        user.LastName.Should().Be(originalLastName);
    }

    [Fact]
    public async Task Handle_WithZeroAge_ShouldNotUpdateAge()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var originalAge = 25;

        var command = _fixture
            .Build<UpdateUserCommand>()
            .With(x => x.Id, userId)
            .With(x => x.Age, 0)
            .Create();

        var user = _fixture
            .Build<ApplicationUser>()
            .With(x => x.Id, userId)
            .With(x => x.Age, originalAge)
            .Create();

        _mockApplicationUserRepository.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);

        _mockUnitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();

        // Verify age was not changed
        user.Age.Should().Be(originalAge);
    }

    [Fact]
    public async Task Handle_ExceptionThrown_ShouldReturnError()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var command = _fixture.Build<UpdateUserCommand>().With(x => x.Id, userId).Create();

        var exceptionMessage = "Database connection failed";
        _mockApplicationUserRepository
            .Setup(x => x.GetByIdAsync(userId))
            .ThrowsAsync(new Exception(exceptionMessage));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be(exceptionMessage);
    }

    [Fact]
    public async Task Handle_WithoutImageUrl_ShouldReturnSuccessWithoutImageUrl()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var command = _fixture
            .Build<UpdateUserCommand>()
            .With(x => x.Id, userId)
            .With(x => x.FirstName, "John")
            .Without(x => x.ImageUrl)
            .Create();

        var user = _fixture
            .Build<ApplicationUser>()
            .With(x => x.Id, userId)
            .Without(x => x.ImageUrl)
            .Create();

        _mockApplicationUserRepository.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);

        _mockUnitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.ImageUrl.Should().BeNull();
    }

    [Fact]
    public async Task Handle_SaveChangesFails_ShouldStillReturnSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var command = _fixture
            .Build<UpdateUserCommand>()
            .With(x => x.Id, userId)
            .With(x => x.FirstName, "John")
            .Create();

        var user = _fixture.Build<ApplicationUser>().With(x => x.Id, userId).Create();

        _mockApplicationUserRepository.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);

        _mockUnitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0); // No changes saved

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue(); // Handler doesn't check save result
    }
}
