using Domain.Models;
using Domain.Repositories;
using FluentAssertions;
using Footex.IntegrationTests.Common;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Footex.IntegrationTests.Repositories;

public class NotificationRepositoryIntegrationTests : BaseIntegrationTest
{
    private readonly INotificationRepository _notificationRepository;

    public NotificationRepositoryIntegrationTests(FootexWebApplicationFactory factory)
        : base(factory)
    {
        _notificationRepository =
            FactoryServiceScope.ServiceProvider.GetRequiredService<INotificationRepository>();
    }

    [Fact]
    public async Task AddAsync_WithValidNotification_ShouldPersistToDatabase()
    {
        // Arrange
        var user = await SeedUserAsync();
        var notification = new Notification
        {
            UserId = user.Id,
            Content = "Test notification content",
            Type = NotificationType.Info,
            IsRead = false,
            Title = "Test Notification Title",
        }; // Act
        var result = await _notificationRepository.AddAsync(notification);
        await Context.SaveChangesAsync();

        // Assert
        result.Should().NotBeNull();
        result.Entity.Id.Should().NotBeNullOrEmpty();

        var persistedNotification = await Context.Notifications.FindAsync(result.Entity.Id);
        persistedNotification.Should().NotBeNull();
        persistedNotification!.UserId.Should().Be(user.Id);
        persistedNotification.Content.Should().Be("Test notification content");
        persistedNotification.Type.Should().Be(NotificationType.Info);
        persistedNotification.IsRead.Should().BeFalse();
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingNotification_ShouldReturnNotification()
    {
        // Arrange
        var notification = await SeedNotificationAsync();

        // Act
        var result = await _notificationRepository.GetByIdAsync(notification.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(notification.Id);
        result.UserId.Should().Be(notification.UserId);
        result.Content.Should().Be(notification.Content);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentNotification_ShouldReturnNull()
    {
        // Act
        var result = await _notificationRepository.GetByIdAsync("non-existent-id");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetNotificationsAsync_WithValidUserId_ShouldReturnUserNotifications()
    {
        // Arrange
        var user1 = await SeedUserAsync();
        var user2 = await SeedUserAsync();
        var notification1 = await SeedNotificationAsync(user1.Id, "Notification 1");
        var notification2 = await SeedNotificationAsync(
            user1.Id,
            "Notification 2",
            NotificationType.MatchUpdate
        );
        var notification3 = await SeedNotificationAsync(user2.Id, "Notification 3");

        // Act
        var result = await _notificationRepository.GetNotificationsAsync(user1.Id);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().Contain(n => n.Id == notification1.Id);
        result.Should().Contain(n => n.Id == notification2.Id);
        result.Should().NotContain(n => n.Id == notification3.Id);
    }

    [Fact]
    public async Task GetNotificationsAsync_ShouldReturnNotificationsOrderedByTimeDescending()
    {
        // Arrange
        var user = await SeedUserAsync();
        var oldNotification = await SeedNotificationAsync(user.Id, "Old notification", time: DateTime.UtcNow.AddMinutes(-10));
        var newNotification = await SeedNotificationAsync(user.Id, "New notification");

        // Act
        var result = await _notificationRepository.GetNotificationsAsync(user.Id);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.First().Id.Should().Be(newNotification.Id);
        result.Last().Id.Should().Be(oldNotification.Id);
    }

    [Fact]
    public async Task GetNotificationsAsync_WithUserHavingNoNotifications_ShouldReturnEmptyCollection()
    {
        // Arrange
        var user = await SeedUserAsync();

        // Act
        var result = await _notificationRepository.GetNotificationsAsync(user.Id);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUnreadNotificationsCountAsync_WithUnreadNotifications_ShouldReturnCorrectCount()
    {
        // Arrange
        var user = await SeedUserAsync();
        await SeedNotificationAsync(user.Id, "Unread 1");
        await SeedNotificationAsync(user.Id, "Unread 2", NotificationType.MatchUpdate);
        await SeedNotificationAsync(user.Id, "Read 1", NotificationType.Info, true);

        // Act
        var result = await _notificationRepository.GetUnreadNotificationsCountAsync(user.Id);

        // Assert
        result.Should().Be(2);
    }

    [Fact]
    public async Task GetUnreadNotificationsCountAsync_WithAllNotificationsRead_ShouldReturnZero()
    {
        // Arrange
        var user = await SeedUserAsync();

        await SeedNotificationAsync(user.Id, "Read 1", NotificationType.Info, true);
        await SeedNotificationAsync(user.Id, "Read 2", NotificationType.MatchUpdate, true);

        // Act
        var result = await _notificationRepository.GetUnreadNotificationsCountAsync(user.Id);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task GetUnreadNotificationsCountAsync_WithUserHavingNoNotifications_ShouldReturnZero()
    {
        // Arrange
        var user = await SeedUserAsync();

        // Act
        var result = await _notificationRepository.GetUnreadNotificationsCountAsync(user.Id);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task UpdateAsync_WithValidNotification_ShouldUpdateNotificationInDatabase()
    {
        // Arrange
        var notification = await SeedNotificationAsync(isRead: false);

        notification.IsRead = true;
        notification.Content = "Updated content"; // Act
        var result = _notificationRepository.Update(notification);
        await Context.SaveChangesAsync();

        // Assert
        result.Should().NotBeNull();
        result.Entity.IsRead.Should().BeTrue();
        result.Entity.Content.Should().Be("Updated content");

        var persistedNotification = await Context.Notifications.FindAsync(notification.Id);
        persistedNotification!.IsRead.Should().BeTrue();
        persistedNotification.Content.Should().Be("Updated content");
    }

    [Fact]
    public async Task DeleteAsync_WithExistingNotification_ShouldRemoveFromDatabase()
    {
        // Arrange
        var notification = await SeedNotificationAsync();
        var notificationId = notification.Id; // Act
        _notificationRepository.Delete(notification);
        await Context.SaveChangesAsync();

        // Assert
        var deletedNotification = await Context.Notifications.FindAsync(notificationId);
        deletedNotification.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_WithMultipleNotifications_ShouldReturnAllNotifications()
    {
        // Arrange
        var user1 = await SeedUserAsync();
        var user2 = await SeedUserAsync();

        var notification1 = await SeedNotificationAsync(user1.Id);
        var notification2 = await SeedNotificationAsync(user2.Id);

        // Act
        var result = await _notificationRepository.GetAllAsync();

        // Assert
        var notifications = result as Notification[] ?? result.ToArray();
        notifications.Should().NotBeNull();
        notifications.Should().HaveCountGreaterThanOrEqualTo(2);
        notifications.Should().Contain(n => n.Id == notification1.Id);
        notifications.Should().Contain(n => n.Id == notification2.Id);
    }

    private async Task<ApplicationUser> SeedUserAsync()
    {
        var uniqueId = Guid.NewGuid().ToString()[..8];
        var user = new ApplicationUser
        {
            UserName = $"testuser{uniqueId}",
            Email = $"testuser{uniqueId}@example.com",
            FirstName = "Test",
            LastName = "User",
            Age = 25,
            EmailConfirmed = true,
        };
        Context.Users.Add(user);
        await Context.SaveChangesAsync();
        return user;
    }

    private async Task<Notification> SeedNotificationAsync(
        string? userId = null,
        string content = "Test notification",
        NotificationType type = NotificationType.Info,
        bool isRead = false,
        DateTime? time = null
    )
    {
        userId ??= (await SeedUserAsync()).Id;

        var notification = new Notification
        {
            UserId = userId,
            Content = content,
            Type = type,
            IsRead = isRead,
            Time = time ?? DateTime.UtcNow,
            Title = "Test Notification Title",
        };

        Context.Notifications.Add(notification);
        await Context.SaveChangesAsync();
        return notification;
    }
}
