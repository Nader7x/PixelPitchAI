using System.Security.Claims;
using Application.CQRS.Notifications.Queries;
using Application.Dtos;
using Application.Interfaces;
using Application.Services;
using AutoFixture;
using AutoFixture.Kernel;
using Domain.Interfaces;
using Domain.Models;
using FluentAssertions;
using Footex.Controllers;
using Footex.UnitTests.Common;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Moq;
using Xunit;

namespace Footex.UnitTests.Controllers;

public class NotificationsControllerTests : IClassFixture<TestFixtureBase>
{
    private readonly TestFixtureBase _testFixtureBase;
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IHubContext<NotificationService, INotificationService>> _hubContextMock;
    private readonly Mock<INotificationService> _notificationServiceMock;
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly NotificationsController _controller;
    private readonly NoRecursionFixture _fixture;

    public NotificationsControllerTests(TestFixtureBase testFixtureBase)
    {
        _testFixtureBase = testFixtureBase;
        _mediatorMock = new Mock<IMediator>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _hubContextMock = new Mock<IHubContext<NotificationService, INotificationService>>();
        _notificationServiceMock = new Mock<INotificationService>();
        _hubContextMock.Setup(h => h.Clients.All).Returns(_notificationServiceMock.Object);
        _cacheServiceMock = new Mock<ICacheService>();

        _fixture = new NoRecursionFixture();
        _fixture
            .Behaviors.OfType<ThrowingRecursionBehavior>()
            .ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        _controller = new NotificationsController(
            _mediatorMock.Object,
            _unitOfWorkMock.Object,
            _hubContextMock.Object,
            _cacheServiceMock.Object
        );

        // Setup controller context
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "test-user-id"),
            new(ClaimTypes.Name, "test-user"),
            new(ClaimTypes.Role, "User"),
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal },
        };
    }

    [Fact]
    public async Task GetAllUserNotifications_WithValidUserId_ReturnsOkResult()
    {
        // Arrange
        var userId = "test-user-id";
        var user = new Domain.Models.ApplicationUser
        {
            Id = userId,
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
        };

        var expectedResponse = new GetUserNotificationsQueryResponse
        {
            Succeeded = true,
            Notifications = new List<Application.Dtos.NotificationDto>
            {
                new Application.Dtos.NotificationDto
                {
                    Id = "1",
                    Content = "Test notification",
                    Type = Domain.Models.NotificationType.MatchStart,
                    IsRead = false,
                },
            },
        };

        _unitOfWorkMock.Setup(x => x.ApplicationUser.GetByIdAsync(userId)).ReturnsAsync(user);

        _mediatorMock
            .Setup(x => x.Send(It.Is<GetUserNotificationsQuery>(q => q.UserId == userId), default))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetAllUserNotifications(userId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.Value.Should().BeEquivalentTo(expectedResponse);

        _mediatorMock.Verify(
            x => x.Send(It.Is<GetUserNotificationsQuery>(q => q.UserId == userId), default),
            Times.Once
        );
    }

    [Fact]
    public async Task GetAllUserNotifications_WithEmptyUserId_ReturnsBadRequest()
    {
        // Arrange
        var emptyUserId = "";

        // Act
        var result = await _controller.GetAllUserNotifications(emptyUserId);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.Value.Should().Be("User ID cannot be null or empty.");

        _unitOfWorkMock.Verify(
            x => x.ApplicationUser.GetByIdAsync(It.IsAny<string>()),
            Times.Never
        );
    }

    [Fact]
    public async Task GetAllUserNotifications_WithNonExistentUser_ReturnsNotFound()
    {
        // Arrange
        var userId = "non-existent-user";

        _unitOfWorkMock
            .Setup(x => x.ApplicationUser.GetByIdAsync(userId))
            .ReturnsAsync((Domain.Models.ApplicationUser?)null);

        // Act
        var result = await _controller.GetAllUserNotifications(userId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        notFoundResult!.Value.Should().Be($"User with ID '{userId}' not found.");

        _mediatorMock.Verify(
            x => x.Send(It.IsAny<GetUserNotificationsQuery>(), default),
            Times.Never
        );
    }

    [Fact]
    public async Task GetUnreadNotificationsCount_WithValidUserId_ReturnsOkResultWithCount()
    {
        // Arrange
        var userId = "test-user-id";
        var unreadCount = 5;
        _unitOfWorkMock
            .Setup(x => x.Notifications.GetUnreadNotificationsCountAsync(userId))
            .ReturnsAsync(unreadCount);

        // Act
        var result = await _controller.GetUnreadNotificationsCount(userId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(unreadCount, okResult.Value);
        _unitOfWorkMock.Verify(
            x => x.Notifications.GetUnreadNotificationsCountAsync(userId),
            Times.Once
        );
    }

    [Fact]
    public async Task GetUnreadNotificationsCount_WithEmptyUserId_ReturnsOkResultWithZero()
    {
        // Arrange
        var emptyUserId = "";
        _unitOfWorkMock
            .Setup(x => x.Notifications.GetUnreadNotificationsCountAsync(emptyUserId))
            .ReturnsAsync(0);

        // Act
        var result = await _controller.GetUnreadNotificationsCount(emptyUserId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(0, okResult.Value);
    }

    [Fact]
    public async Task MarkNotificationsAsRead_WithValidId_ReturnsOk()
    {
        // Arrange
        var notificationId = "test-notification-id";
        var notification = new Notification
        {
            Id = notificationId,
            IsRead = false,
            Content = "Test",
            Title = "Test",
            Type = NotificationType.Info,
            UserId = "test-user-id",
        };

        _unitOfWorkMock
            .Setup(x => x.Notifications.GetByIdAsync(notificationId))
            .ReturnsAsync(notification);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);

        // Act
        var result = await _controller.MarkNotificationsAsRead(notificationId);

        // Assert
        result.Should().BeOfType<OkResult>();
        notification.IsRead.Should().BeTrue();
        _unitOfWorkMock.Verify(x => x.Notifications.UpdateAsync(notification), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task MarkNotificationsAsRead_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var notificationId = "invalid-id";
        _unitOfWorkMock
            .Setup(x => x.Notifications.GetByIdAsync(notificationId))
            .ReturnsAsync((Notification)null!);

        // Act
        var result = await _controller.MarkNotificationsAsRead(notificationId);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task MarkAllNotificationsAsRead_WithValidUserId_ReturnsNoContent()
    {
        // Arrange
        var userId = "test-user-id";
        var notifications = new List<Notification>
        {
            new()
            {
                Id = "1",
                IsRead = false,
                UserId = userId,
                Content = "Test",
                Title = "Test",
                Type = NotificationType.Info,
            },
            new()
            {
                Id = "2",
                IsRead = false,
                UserId = userId,
                Content = "Test",
                Title = "Test",
                Type = NotificationType.Info,
            },
        };

        _unitOfWorkMock
            .Setup(x => x.Notifications.GetNotificationsAsync(userId))
            .ReturnsAsync(notifications);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);

        // Act
        var result = await _controller.MarkAllNotificationsAsRead(userId);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        notifications.ForEach(n => n.IsRead.Should().BeTrue());
        _unitOfWorkMock.Verify(
            x => x.Notifications.UpdateAsync(It.IsAny<Notification>()),
            Times.Exactly(2)
        );
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task DeleteNotification_WithValidId_ReturnsNoContent()
    {
        // Arrange
        var notificationId = "test-notification-id";
        var notification = new Notification
        {
            Id = notificationId,
            Content = "Test",
            Title = "Test",
            Type = NotificationType.Info,
            UserId = "test-user-id",
        };

        _unitOfWorkMock
            .Setup(x => x.Notifications.GetByIdAsync(notificationId))
            .ReturnsAsync(notification);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);

        // Act
        var result = await _controller.DeleteNotification(notificationId);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        _unitOfWorkMock.Verify(x => x.Notifications.DeleteAsync(notification), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task DeleteNotification_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var notificationId = "invalid-id";
        _unitOfWorkMock
            .Setup(x => x.Notifications.GetByIdAsync(notificationId))
            .ReturnsAsync((Notification)null!);

        // Act
        var result = await _controller.DeleteNotification(notificationId);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task DeleteAllUserNotifications_WithValidUserId_ReturnsNoContent()
    {
        // Arrange
        var userId = "test-user-id";
        var notifications = new List<Notification>
        {
            new()
            {
                Id = "1",
                UserId = userId,
                Content = "Test",
                Title = "Test",
                Type = NotificationType.Info,
            },
            new()
            {
                Id = "2",
                UserId = userId,
                Content = "Test",
                Title = "Test",
                Type = NotificationType.Info,
            },
        };

        _unitOfWorkMock
            .Setup(x => x.Notifications.GetNotificationsAsync(userId))
            .ReturnsAsync(notifications);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);

        // Act
        var result = await _controller.DeleteAllUserNotifications(userId);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        _unitOfWorkMock.Verify(
            x => x.Notifications.DeleteAsync(It.IsAny<Notification>()),
            Times.Exactly(2)
        );
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
    }
}
