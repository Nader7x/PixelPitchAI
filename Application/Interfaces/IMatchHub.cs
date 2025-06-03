using Domain.Models;

namespace Application.Interfaces;

public interface IMatchHub
{
    Task JoinMatchGroupAsync(int matchId);
    Task LeaveMatchGroupAsync(int matchId);
    Task SendAsync(string method, string message);
    Task SendMatchEventAsync(string method, int matchId, FootballMatchEvent? data);
    Task SendMatchStatisticsAsync(string method, int matchId, object? data);
}