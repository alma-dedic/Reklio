using Microsoft.EntityFrameworkCore;
using Reklio.Api.Data;
using Reklio.Api.Models;
using Reklio.Api.Services.Interfaces;

namespace Reklio.Api.Services;

public class NotificationService : INotificationService
{
    private readonly ReklioDbContext _db;

    public NotificationService(ReklioDbContext db)
    {
        _db = db;
    }

    public async Task<Notification> CreateAsync(Notification notification)
    {
        _db.Notifications.Add(notification);
        await _db.SaveChangesAsync();
        return notification;
    }

    public async Task<IReadOnlyList<Notification>> GetByUserAsync(string userId)
    {
        return await _db.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.Id)
            .ToListAsync();
    }

    public async Task MarkAsReadAsync(int notificationId)
    {
        var notification = await _db.Notifications.FindAsync(notificationId)
            ?? throw new KeyNotFoundException($"Notifikacija {notificationId} ne postoji.");

        notification.IsRead = true;
        await _db.SaveChangesAsync();
    }
}