using Reklio.Api.Models;

namespace Reklio.Api.Services.Interfaces;

public interface INotificationService
{
    Task<Notification> CreateAsync(Notification notification);

    Task<IReadOnlyList<Notification>> GetByUserAsync(string userId);

    Task MarkAsReadAsync(int notificationId);
}