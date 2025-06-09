using Application.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Application.Services;

[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class MatchHub : Hub<IMatchHub>
{
    public override Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst("nameid")?.Value;
        if (userId != null) Clients.User(userId).SendAsync("Welcome", "Welcome to the Match Hub!");
        return base.OnConnectedAsync();
    }

    public async Task JoinMatchGroup(int matchId )
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"{matchId.ToString()}");
        await Clients.Caller.SendAsync("JoinedMatchGroup", $"Joined Match {matchId.ToString()}");
    }

    public async Task LeaveMatchGroup(int matchId )
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"{matchId.ToString()}");
        await Clients.Caller.SendAsync("LeftMatchGroup", $"Left Match {matchId.ToString()}");
    }
}