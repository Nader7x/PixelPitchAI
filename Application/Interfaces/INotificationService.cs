using Domain.Models;

namespace Application.Interfaces;

public interface INotificationService
{
    Task SendAsync(string method, Notification notification);
    Task SendNotificationAsync(Notification notification);
    Task SendNotificationAsync(string title, string message, string type, IEnumerable<int> userIds);
    Task SendMatchStartNotificationAsync(Notification notification, string simulationId);
    Task SendMatchEndNotificationAsync(Notification notification, string simulationId);
    Task SendMatchUpdateNotificationAsync(Notification notification, string simulationId);
    Task SendSimulationUpdateNotificationAsync(Notification notification, string simulationId);
    Task SendMessageAsync(string message);
}