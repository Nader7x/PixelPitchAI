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
        Console.WriteLine(Context.User.GetNameId());
        var userId = Context.UserIdentifier;
        if (userId != null)
        {
            Clients.User(userId).SendMessageAsync("Welcome back!");
        }
        return base.OnConnectedAsync();
    }
}