using Application.Helpers;
using Domain.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Application.Services;
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class NotificationService : Hub<INotificationService>
{
    public override Task OnConnectedAsync()
    {
        var userId = Context.User.GetNameId();
        if (userId != null)
        {
            Clients.User(userId).SendMessageAsync("Welcome back!");
        }
        return base.OnConnectedAsync();
    }
}