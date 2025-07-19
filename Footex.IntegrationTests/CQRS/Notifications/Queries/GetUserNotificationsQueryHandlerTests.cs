using Application.CQRS.Auth.Commands;
using Application.CQRS.Notifications.Commands;
using Application.CQRS.Notifications.Queries;
using Domain.Models;
using FluentAssertions;
using Footex.IntegrationTests.Common;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Footex.IntegrationTests.CQRS.Notifications;

public class GetUserNotificationsQueryHandlerTests : IClassFixture<FootexWebApplicationFactory>
{
    private readonly IServiceScope _scope;
    private readonly IMediator _mediator;

    public GetUserNotificationsQueryHandlerTests(FootexWebApplicationFactory factory)
    {
        _scope = factory.Services.CreateScope();
        _mediator = _scope.ServiceProvider.GetRequiredService<IMediator>();
    }

    [Fact]
    public async Task Handle_ShouldReturnUserNotifications_WhenUserExists()
    {
        // Arrange
        // Create a user and add notifications for that user
        var user = await _mediator.Send(
            new RegisterUserCommand
            {
                FirstName = "Test",
                LastName = "User",
                Email = "test.user@example.com",
                Password = "TestPassword123",
                UserName = "testuser",
            }
        );
        var userId = user.UserId;

        await _mediator.Send(
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
        await _mediator.Send(
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

        var query = new GetUserNotificationsQuery { UserId = userId };

        // Act
        var result = await _mediator.Send(query);

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
        var result = await _mediator.Send(query);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Notifications.Should().NotBeNull();
        result.Notifications.Should().BeEmpty();
    }
}
