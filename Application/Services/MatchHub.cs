using Microsoft.AspNetCore.SignalR;

namespace Application.Services;

public class MatchHub : Hub
{
    public async Task JoinMatchGroup(int matchId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, matchId.ToString());
        await Clients.Caller.SendAsync("JoinedMatchGroup", matchId);
    }

    public async Task LeaveMatchGroup(int matchId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, matchId.ToString());
        await Clients.Caller.SendAsync("LeftMatchGroup", matchId);
    }
}