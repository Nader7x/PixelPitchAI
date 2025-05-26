namespace Domain.Interfaces;

public interface INotificationService
{
    Task SendNotificationAsync(string title, string message, string type, int userId);
    Task SendNotificationAsync(string title, string message, string type, IEnumerable<int> userIds);
    Task SendMessageAsync(string message);
}