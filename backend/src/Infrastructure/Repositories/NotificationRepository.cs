using Domain.Models;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class NotificationRepository(FootballDbContext context)
    : Repository<Notification>(context),
        INotificationRepository
{
    private readonly FootballDbContext _context = context;

    public async Task<IEnumerable<Notification>> GetNotificationsAsync(string userId)
    {
        return await _context
            .Notifications.Where(n => n.UserId == userId)
            .OrderByDescending(n => n.Time)
            .ToListAsync();
    }

    public async Task<int> GetUnreadNotificationsCountAsync(string userId)
    {
        return await _context.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);
    }
}
