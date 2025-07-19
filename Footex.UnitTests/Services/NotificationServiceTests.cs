using System.Security.Claims;
using Application.Interfaces;
using Application.Services;
using Microsoft.AspNetCore.SignalR;
using Moq;
using Xunit;

namespace Footex.UnitTests.Services;

public class NotificationServiceTests
{
    private readonly Mock<IHubCallerClients<INotificationService>> _clientsMock;
    private readonly Mock<HubCallerContext> _contextMock;
    private readonly Mock<INotificationService> _mockClientProxy;
    private readonly NotificationService _notificationService;

    public NotificationServiceTests()
    {
        _clientsMock = new Mock<IHubCallerClients<INotificationService>>();
        _contextMock = new Mock<HubCallerContext>();
        _mockClientProxy = new Mock<INotificationService>();

        _notificationService = new NotificationService
        {
            Clients = _clientsMock.Object,
            Context = _contextMock.Object,
        };
    }

    [Fact]
    public async Task OnConnectedAsync_WithValidUser_ShouldSendMessage()
    {
        // Arrange
        var userId = "test-user-id";
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId) };
        var claimsIdentity = new ClaimsIdentity(claims);
        var user = new ClaimsPrincipal(claimsIdentity);

        _contextMock.Setup(c => c.User).Returns(user);
        _clientsMock.Setup(c => c.User(userId)).Returns(_mockClientProxy.Object);

        // Act
        await _notificationService.OnConnectedAsync();

        // Assert
        _mockClientProxy.Verify(c => c.SendMessageAsync("Welcome back!"), Times.Once);
    }

    [Fact]
    public async Task OnConnectedAsync_WithInvalidUser_ShouldNotSendMessage()
    {
        // Arrange
        var user = new ClaimsPrincipal(); // No claims

        _contextMock.Setup(c => c.User).Returns(user);

        // Act
        await _notificationService.OnConnectedAsync();

        // Assert
        _mockClientProxy.Verify(c => c.SendMessageAsync(It.IsAny<string>()), Times.Never);
    }
}
