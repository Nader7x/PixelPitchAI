using Domain.Interfaces;
using Domain.Models;

namespace Domain.Repositories;

public interface INotificationRepository : IRepository<Notification>
{
    Task<IEnumerable<Notification>> GetNotificationsAsync(string userId);
    Task<int> GetUnreadNotificationsCountAsync(string userId);
}