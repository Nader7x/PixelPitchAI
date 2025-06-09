using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Footex.IntegrationTests.Common;
using Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Footex.IntegrationTests.Controllers;

public class NotificationsControllerIntegrationTests : IClassFixture<FootexWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly FootexWebApplicationFactory _factory;

    public NotificationsControllerIntegrationTests(FootexWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetAllUserNotifications_WithValidUserId_ReturnsNotifications()
    {
        // Arrange
        var userId = await SeedTestUserAsync();
        await SeedNotificationsAsync(userId);

        // Act
        var response = await _client.GetAsync($"/api/notifications/user/{userId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);

        jsonDoc.RootElement.TryGetProperty("succeeded", out var succeeded);
        succeeded.GetBoolean().Should().BeTrue();

        jsonDoc.RootElement.TryGetProperty("notifications", out var notifications);
        notifications.ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task GetAllUserNotifications_WithInvalidUserId_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/notifications/user/invalid-user-id");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetUnreadNotificationsCount_WithValidUserId_ReturnsCount()
    {
        // Arrange
        var userId = await SeedTestUserAsync();
        await SeedNotificationsAsync(userId);

        // Act
        var response = await _client.GetAsync($"/api/notifications/user/{userId}/unread-count");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var count = int.Parse(content);
        count.Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public async Task GetUnreadNotificationsCount_WithInvalidUserId_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/notifications/user/invalid-user-id/unread-count");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task MarkNotificationAsRead_WithValidId_ReturnsNoContent()
    {
        // Arrange
        var userId = await SeedTestUserAsync();
        var notificationId = await SeedNotificationsAsync(userId);

        // Act
        var response = await _client.PostAsync($"/api/notifications/mark-as-read/{notificationId}", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task MarkNotificationAsRead_WithInvalidId_ReturnsNotFound()
    {
        // Act
        var response = await _client.PostAsync("/api/notifications/mark-as-read/invalid-notification-id", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task MarkAllNotificationsAsRead_WithValidUserId_ReturnsNoContent()
    {
        // Arrange
        var userId = await SeedTestUserAsync();
        await SeedNotificationsAsync(userId);

        // Act
        var response = await _client.PostAsync($"/api/notifications/user/{userId}/mark-all-read", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task MarkAllNotificationsAsRead_WithInvalidUserId_ReturnsNotFound()
    {
        // Act
        var response = await _client.PostAsync("/api/notifications/user/invalid-user-id/mark-all-read", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteNotification_WithValidId_ReturnsNoContent()
    {
        // Arrange
        var userId = await SeedTestUserAsync();
        var notificationId = await SeedNotificationsAsync(userId);

        // Act
        var response = await _client.DeleteAsync($"/api/notifications/{notificationId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteNotification_WithInvalidId_ReturnsNotFound()
    {
        // Act
        var response = await _client.DeleteAsync("/api/notifications/invalid-notification-id");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteAllUserNotifications_WithValidUserId_ReturnsNoContent()
    {
        // Arrange
        var userId = await SeedTestUserAsync();
        await SeedNotificationsAsync(userId);

        // Act
        var response = await _client.DeleteAsync($"/api/notifications/user/{userId}/all");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteAllUserNotifications_WithInvalidUserId_ReturnsNotFound()
    {
        // Act
        var response = await _client.DeleteAsync("/api/notifications/user/invalid-user-id/all");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task NotificationWorkflow_CreateMarkAsReadDelete_WorksCorrectly()
    {
        // Arrange
        var userId = await SeedTestUserAsync();
        var notificationId = await SeedNotificationsAsync(userId);

        // Act & Assert - Get notifications
        var getResponse = await _client.GetAsync($"/api/notifications/user/{userId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act & Assert - Get unread count
        var countResponse = await _client.GetAsync($"/api/notifications/user/{userId}/unread-count");
        countResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var initialCount = int.Parse(await countResponse.Content.ReadAsStringAsync());
        initialCount.Should().BeGreaterThan(0);

        // Act & Assert - Mark as read
        var markReadResponse = await _client.PostAsync($"/api/notifications/mark-as-read/{notificationId}", null);
        markReadResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Act & Assert - Verify count decreased
        var newCountResponse = await _client.GetAsync($"/api/notifications/user/{userId}/unread-count");
        newCountResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var newCount = int.Parse(await newCountResponse.Content.ReadAsStringAsync());
        newCount.Should().BeLessThan(initialCount);

        // Act & Assert - Delete notification
        var deleteResponse = await _client.DeleteAsync($"/api/notifications/{notificationId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task BulkOperations_MarkAllAsReadAndDeleteAll_WorkCorrectly()
    {
        // Arrange
        var userId = await SeedTestUserAsync();
        await SeedMultipleNotificationsAsync(userId, 5);

        // Act & Assert - Get initial unread count
        var initialCountResponse = await _client.GetAsync($"/api/notifications/user/{userId}/unread-count");
        initialCountResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var initialCount = int.Parse(await initialCountResponse.Content.ReadAsStringAsync());
        initialCount.Should().BeGreaterThan(0);

        // Act & Assert - Mark all as read
        var markAllReadResponse = await _client.PostAsync($"/api/notifications/user/{userId}/mark-all-read", null);
        markAllReadResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Act & Assert - Verify all marked as read
        var newCountResponse = await _client.GetAsync($"/api/notifications/user/{userId}/unread-count");
        newCountResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var newCount = int.Parse(await newCountResponse.Content.ReadAsStringAsync());
        newCount.Should().Be(0);

        // Act & Assert - Delete all notifications
        var deleteAllResponse = await _client.DeleteAsync($"/api/notifications/user/{userId}/all");
        deleteAllResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Act & Assert - Verify no notifications remain
        var finalResponse = await _client.GetAsync($"/api/notifications/user/{userId}");
        finalResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var finalContent = await finalResponse.Content.ReadAsStringAsync();
        var finalDoc = JsonDocument.Parse(finalContent);
        finalDoc.RootElement.TryGetProperty("notifications", out var finalNotifications);
        finalNotifications.GetArrayLength().Should().Be(0);
    }

    private async Task<string> SeedTestUserAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FootballDbContext>();

        var user = TestData.CreateTestUser();
        context.Users.Add(user);
        await context.SaveChangesAsync();

        return user.Id;
    }

    private async Task<string> SeedNotificationsAsync(string userId)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FootballDbContext>();

        var notification = TestData.CreateTestNotification(userId);
        context.Notifications.Add(notification);
        await context.SaveChangesAsync();

        return notification.Id;
    }

    private async Task SeedMultipleNotificationsAsync(string userId, int count)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FootballDbContext>();

        for (var i = 0; i < count; i++)
        {
            var notification = TestData.CreateTestNotification(userId);
            notification.Title = $"Test Notification {i + 1}";
            context.Notifications.Add(notification);
        }

        await context.SaveChangesAsync();
    }
}
