using Application.CQRS.Auth.Commands;
using Application.CQRS.Notifications.Commands;
using Application.CQRS.Notifications.Queries;
using Domain.Interfaces;
using Domain.Models;
using FluentAssertions;
using Footex.IntegrationTests.Common;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Footex.IntegrationTests.CQRS.Notifications.Queries;

public class GetUserNotificationsQueryHandlerTests(FootexWebApplicationFactory factory)
    : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task Handle_ShouldReturnUserNotifications_WhenUserExists()
    {
        // Arrange
        // Create a user and add notifications for that user
        var user = await Mediator.Send(
            new RegisterUserCommand
            {
                FirstName = "Notifications",
                LastName = "User",
                Email = "notificationsuser@example.com",
                Password = "TestPassword123!",
                UserName = "notificationsuser",
            }
        );
        var userId = user.UserId;

        await Mediator.Send(
            new CreateNotificationCommand
            {
                Notification = new Notification
                {
                    UserId = userId,
                    Content = "Test notification 1",
                    Time = DateTime.UtcNow,
                    IsRead = false,
                    Type = NotificationType.Error,
                    Title = "Test Title 1",
                },
            }
        );
        await Mediator.Send(
            new CreateNotificationCommand
            {
                Notification = new Notification
                {
                    UserId = userId,
                    Content = "Test notification 2",
                    Time = DateTime.UtcNow,
                    IsRead = false,
                    Type = NotificationType.Info,
                    Title = "Test Title 2",
                },
            }
        );
        var unitOfWork = FactoryServiceScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        await unitOfWork.SaveChangesAsync();

        var query = new GetUserNotificationsQuery { UserId = userId };

        // Act
        var result = await Mediator.Send(query);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Notifications.Should().NotBeNull();
        result.Notifications.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyList_WhenUserDoesNotExist()
    {
        // Arrange
        var query = new GetUserNotificationsQuery { UserId = Guid.NewGuid().ToString() };

        // Act
        var result = await Mediator.Send(query);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Notifications.Should().NotBeNull();
        result.Notifications.Should().BeEmpty();
    }
}
