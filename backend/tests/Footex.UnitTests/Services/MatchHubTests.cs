using Application.Interfaces;
using Application.Services;
using Microsoft.AspNetCore.SignalR;
using Moq;
using Xunit;

namespace Footex.UnitTests.Services;

public class MatchHubTests
{
    private readonly Mock<IHubCallerClients<IMatchHub>> _clientsMock;
    private readonly Mock<HubCallerContext> _contextMock;
    private readonly Mock<IGroupManager> _groupsMock;
    private readonly MatchHub _matchHub;
    private readonly Mock<IMatchHub> _mockClientProxy;

    public MatchHubTests()
    {
        _clientsMock = new Mock<IHubCallerClients<IMatchHub>>();
        _contextMock = new Mock<HubCallerContext>();
        _groupsMock = new Mock<IGroupManager>();
        _mockClientProxy = new Mock<IMatchHub>();

        _matchHub = new MatchHub
        {
            Clients = _clientsMock.Object,
            Context = _contextMock.Object,
            Groups = _groupsMock.Object,
        };

        _clientsMock.Setup(c => c.Caller).Returns(_mockClientProxy.Object);
    }

    [Fact]
    public async Task JoinMatchGroup_ShouldAddToGroup()
    {
        // Arrange
        var matchId = 1;
        var connectionId = "test-connection-id";
        _contextMock.Setup(c => c.ConnectionId).Returns(connectionId);

        // Act
        await _matchHub.JoinMatchGroup(matchId);

        // Assert
        _groupsMock.Verify(
            g => g.AddToGroupAsync(connectionId, matchId.ToString(), default),
            Times.Once
        );
        _mockClientProxy.Verify(
            c => c.SendAsync("JoinedMatchGroup", $"Joined Match {matchId}"),
            Times.Once
        );
    }

    [Fact]
    public async Task LeaveMatchGroup_ShouldRemoveFromGroup()
    {
        // Arrange
        var matchId = 1;
        var connectionId = "test-connection-id";
        _contextMock.Setup(c => c.ConnectionId).Returns(connectionId);

        // Act
        await _matchHub.LeaveMatchGroup(matchId);

        // Assert
        _groupsMock.Verify(
            g => g.RemoveFromGroupAsync(connectionId, matchId.ToString(), default),
            Times.Once
        );
        _mockClientProxy.Verify(
            c => c.SendAsync("LeftMatchGroup", $"Left Match {matchId}"),
            Times.Once
        );
    }
}
