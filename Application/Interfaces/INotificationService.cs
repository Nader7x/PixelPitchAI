using Domain.Models;

namespace Domain.Interfaces;

public interface INotificationService
{
    Task SendNotificationAsync(Notification notification);
    Task SendNotificationAsync(string title, string message, string type, IEnumerable<int> userIds);
    Task SendMatchStartNotificationAsync(Notification notification , string simulationId);

    Task SendMessageAsync(string message);
}