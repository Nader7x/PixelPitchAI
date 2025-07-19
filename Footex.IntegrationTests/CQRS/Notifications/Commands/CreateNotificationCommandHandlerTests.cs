using Application.CQRS.Notifications.Commands;
using Domain.Models;
using FluentAssertions;
using Footex.IntegrationTests.Common;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Footex.IntegrationTests.CQRS.Notifications;

public class CreateNotificationCommandHandlerTests : IClassFixture<FootexWebApplicationFactory>
{
    private readonly IServiceScope _scope;
    private readonly IMediator _Mediator;

    public CreateNotificationCommandHandlerTests(FootexWebApplicationFactory factory)
    {
        _scope = factory.Services.CreateScope();
        _Mediator = _scope.ServiceProvider.GetRequiredService<IMediator>();
    }

    [Fact]
    public async Task Handle_ShouldCreateNotification_WhenCommandIsValid()
    {
        // Arrange
        var command = new CreateNotificationCommand
        {
            Notification = new Notification
            {
                UserId = Guid.NewGuid().ToString(),
                Content = "Test notification",
                Time = DateTime.UtcNow,
                IsRead = false,
                Type = NotificationType.Info,
                Title = "Test Title",
            },
        };

        // Act
        var result = await _Mediator.Send(command);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Notification.Should().NotBeNull();
        result.Notification?.Content.Should().Be(command.Notification.Content);
    }
}
