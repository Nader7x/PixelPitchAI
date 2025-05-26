namespace Application.Interfaces;

public interface IMatchHub
{
    Task SendAsync(string method , int  matchId, object? data = null);
}