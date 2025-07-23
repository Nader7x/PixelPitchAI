using System.Net;
using System.Net.Http.Json;
using Application.CQRS.Notifications.Queries;
using FluentAssertions;
using Footex.IntegrationTests.Common;
using Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Footex.IntegrationTests.Controllers;
[Collection("Database collection")]
public class NotificationsControllerIntegrationTests(FootexWebApplicationFactory factory)
    : IClassFixture<FootexWebApplicationFactory>
{
    [Fact]
    public async Task GetAllUserNotifications_WithValidUserId_ReturnsNotifications()
    {
        // Arrange
        var httpClient = await factory.CreateAuthenticatedClientAsync();
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FootballDbContext>();
        var user = TestData.CreateTestUser(true);
        context.Users.Add(user);
        await context.SaveChangesAsync();
        var notification = TestData.CreateTestNotification(user.Id);
        context.Notifications.Add(notification);
        await context.SaveChangesAsync();

        // Act
        var response = await httpClient.GetAsync($"/api/notifications/user/{user.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<GetUserNotificationsQueryResponse>();
        result.Should().NotBeNull();
        result!.Succeeded.Should().BeTrue();
        result.Notifications.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetUnreadNotificationsCount_WithValidUserId_ReturnsCount()
    {
        // Arrange
        var httpClient = await factory.CreateAuthenticatedClientAsync();
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FootballDbContext>();
        var user = TestData.CreateTestUser(true);
        context.Users.Add(user);
        await context.SaveChangesAsync();
        var notification = TestData.CreateTestNotification(user.Id);
        context.Notifications.Add(notification);
        await context.SaveChangesAsync();

        // Act
        var response = await httpClient.GetAsync($"/api/notifications/user/{user.Id}/unread-count");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<int>();
        result.Should().NotBe(0);
        result.Should().Be(1);
    }

    [Fact]
    public async Task MarkNotificationAsRead_WithValidId_ReturnsOk()
    {
        // Arrange
        var httpClient = await factory.CreateAuthenticatedClientAsync();
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FootballDbContext>();
        var user = TestData.CreateTestUser(true);
        context.Users.Add(user);
        await context.SaveChangesAsync();
        var notification = TestData.CreateTestNotification(user.Id);
        context.Notifications.Add(notification);
        await context.SaveChangesAsync();

        // Act
        var response = await httpClient.PostAsync(
            $"/api/notifications/mark-as-read/{notification.Id}",
            null
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task MarkAllNotificationsAsRead_WithValidUserId_ReturnsNoContent()
    {
        // Arrange
        var httpClient = await factory.CreateAuthenticatedClientAsync();
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FootballDbContext>();
        var user = TestData.CreateTestUser(true);
        context.Users.Add(user);
        await context.SaveChangesAsync();
        var notification = TestData.CreateTestNotification(user.Id);
        context.Notifications.Add(notification);
        await context.SaveChangesAsync();

        // Act
        var response = await httpClient.PostAsync(
            $"/api/notifications/user/{user.Id}/mark-all-read",
            null
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteNotification_WithValidId_ReturnsNoContent()
    {
        // Arrange
        var httpClient = await factory.CreateAuthenticatedClientAsync();
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FootballDbContext>();
        var user = TestData.CreateTestUser(true);
        context.Users.Add(user);
        await context.SaveChangesAsync();
        var notification = TestData.CreateTestNotification(user.Id);
        context.Notifications.Add(notification);
        await context.SaveChangesAsync();

        // Act
        var response = await httpClient.DeleteAsync($"/api/notifications/{notification.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteAllUserNotifications_WithValidUserId_ReturnsNoContent()
    {
        // Arrange
        var httpClient = await factory.CreateAuthenticatedClientAsync();
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FootballDbContext>();
        var user = TestData.CreateTestUser(true);
        context.Users.Add(user);
        await context.SaveChangesAsync();
        var notification = TestData.CreateTestNotification(user.Id);
        context.Notifications.Add(notification);
        await context.SaveChangesAsync();

        // Act
        var response = await httpClient.DeleteAsync($"/api/notifications/user/{user.Id}/all");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
