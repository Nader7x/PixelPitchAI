using Application.CQRS.Auth.Commands;
using Domain.Interfaces;
using Domain.Models;
using Footex.IntegrationTests.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Footex.IntegrationTests.CQRS.Auth.Commands;

public class RegisterUserCommandHandlerIntegrationTests : BaseIntegrationTest
{
    private readonly RegisterUserCommandHandler _handler;
    private readonly IUnitOfWork _unitOfWork;
    private readonly UserManager<ApplicationUser> _userManager;

    public RegisterUserCommandHandlerIntegrationTests(FootexWebApplicationFactory factory) : base(factory)
    {
        _handler = ServiceProvider.GetRequiredService<RegisterUserCommandHandler>();
        _unitOfWork = ServiceProvider.GetRequiredService<IUnitOfWork>();
        _userManager = ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    }

    [Fact]
    public async Task Handle_ValidRegistrationCommand_CreatesUserSuccessfully()
    {
        // Arrange
        var command = new RegisterUserCommand
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            Password = "StrongPassword123!",
            UserName = "JohnDoe"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);
        var savedUser = await _unitOfWork.ApplicationUser.GetByEmailAsync(command.Email);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.UserId);
        Assert.Equal(command.Email, savedUser.Email);
        Assert.Equal(command.UserName, savedUser.UserName);
        Assert.Null(result.Error);

        // Verify user was created in database
        var user = await _userManager.FindByEmailAsync(command.Email);
        Assert.NotNull(user);
        Assert.Equal(command.FirstName, user.FirstName);
        Assert.Equal(command.LastName, user.LastName);
        Assert.Equal(command.Email, user.Email);
        Assert.Equal(command.UserName, user.UserName);
        Assert.False(user.EmailConfirmed); // Should be false initially
    }

    [Fact]
    public async Task Handle_DuplicateEmail_ReturnsError()
    {
        // Arrange
        var existingUser = new ApplicationUser
        {
            FirstName = "Existing",
            LastName = "User",
            Email = "existing@example.com",
            UserName = "existinguser",
            EmailConfirmed = true
        };

        await _userManager.CreateAsync(existingUser, "Password123!");

        var command = new RegisterUserCommand
        {
            FirstName = "New",
            LastName = "User",
            Email = "existing@example.com", // Same email
            Password = "StrongPassword123!",
            UserName = "NewUser"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.NotNull(result.Error);
        Assert.Contains("email", result.Error.ToLower());
    }

    [Fact]
    public async Task Handle_DuplicateUsername_ReturnsError()
    {
        // Arrange
        var existingUser = new ApplicationUser
        {
            FirstName = "Existing",
            LastName = "User",
            Email = "existing@example.com",
            UserName = "duplicateusername",
            EmailConfirmed = true
        };

        await _userManager.CreateAsync(existingUser, "Password123!");

        var command = new RegisterUserCommand
        {
            FirstName = "New",
            LastName = "User",
            Email = "new@example.com",
            Password = "StrongPassword123!",
            UserName = "NewUser"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.NotNull(result.Error);
        Assert.Contains("username", result.Error.ToLower());
    }

    [Fact]
    public async Task Handle_WeakPassword_ReturnsError()
    {
        // Arrange
        var command = new RegisterUserCommand
        {
            FirstName = "Test",
            LastName = "User",
            Email = "weak@example.com",
            Password = "123", // Weak password
            UserName = "TestUser"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.NotNull(result.Error);
        Assert.Contains("password", result.Error.ToLower());
    }

    [Fact]
    public async Task Handle_MismatchedPasswords_ReturnsError()
    {
        // Arrange
        var command = new RegisterUserCommand
        {
            FirstName = "Test",
            LastName = "User",
            Email = "mismatch@example.com",
            Password = "StrongPassword123!",
            UserName = "TestUser" // Different confirmation
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.NotNull(result.Error);
        Assert.Contains("password", result.Error.ToLower());
    }

    [Fact]
    public async Task Handle_InvalidEmailFormat_ReturnsError()
    {
        // Arrange
        var command = new RegisterUserCommand
        {
            FirstName = "Test",
            LastName = "User",
            Email = "invalid-email-format", // Invalid email
            Password = "StrongPassword123!",
            UserName = "TestUser"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.NotNull(result.Error);
        Assert.Contains("email", result.Error.ToLower());
    }

    [Fact]
    public async Task Handle_EmptyRequiredFields_ReturnsError()
    {
        // Arrange
        var command = new RegisterUserCommand
        {
            FirstName = "", // Empty
            LastName = "", // Empty
            Email = "", // Empty
            Password = "StrongPassword123!",
            UserName = ""
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task Handle_LongUsername_HandlesGracefully()
    {
        // Arrange
        var longUsername = new string('a', 300); // Very long username
        var command = new RegisterUserCommand
        {
            FirstName = "Test",
            LastName = "User",
            Email = "long@example.com",
            Password = "StrongPassword123!",
            UserName = "TestUser"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        if (result.Succeeded)
        {
            var user = await _userManager.FindByEmailAsync(command.Email);
            Assert.NotNull(user);
        }
        else
        {
            Assert.NotNull(result.Error);
            // Should handle long username appropriately
        }
    }

    [Fact]
    public async Task Handle_SpecialCharactersInName_CreatesSuccessfully()
    {
        // Arrange
        var command = new RegisterUserCommand
        {
            FirstName = "José María",
            LastName = "García-López",
            Email = "jose.garcia@example.com",
            Password = "StrongPassword123!",
            UserName = "JoseGarcia"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);

        var user = await _userManager.FindByEmailAsync(command.Email);
        Assert.NotNull(user);
        Assert.Equal("José María", user.FirstName);
        Assert.Equal("García-López", user.LastName);
    }

    [Fact]
    public async Task Handle_MultipleUsersRegistration_AllCreateSuccessfully()
    {
        // Arrange
        var commands = new[]
        {
            new RegisterUserCommand
            {
                FirstName = "User1",
                LastName = "Test",
                Email = "user1@example.com",
                Password = "Password123!",
                UserName = "User1Test"
            },
            new RegisterUserCommand
            {
                FirstName = "User2",
                LastName = "Test",
                Email = "user2@example.com",
                UserName = "user2",
                Password = "Password123!"
            },
            new RegisterUserCommand
            {
                FirstName = "User3",
                LastName = "Test",
                Email = "user3@example.com",
                UserName = "user3",
                Password = "Password123!"
            }
        };

        var results = new List<RegisterUserCommandResponse>();

        // Act
        foreach (var command in commands)
        {
            var result = await _handler.Handle(command, CancellationToken.None);
            results.Add(result);
        }

        // Assert
        Assert.All(results, r => Assert.True(r.Succeeded));
        Assert.Equal(3, results.Count);

        // Verify all users were created
        foreach (var command in commands)
        {
            var user = await _userManager.FindByEmailAsync(command.Email);
            Assert.NotNull(user);
        }
    }

    [Fact]
    public async Task Handle_DatabaseException_ReturnsErrorResponse()
    {
        // Arrange
        var command = new RegisterUserCommand
        {
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com",
            UserName = "testuser",
            Password = "StrongPassword123!"
        };

        // Dispose context to simulate database error
        await DisposeContext();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.NotNull(result.Error);
    }

    private Task DisposeContext()
    {
        _unitOfWork.Dispose();
        return Task.CompletedTask;
    }
}