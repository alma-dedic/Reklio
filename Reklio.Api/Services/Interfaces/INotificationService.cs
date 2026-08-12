using Reklio.Api.Models;

namespace Reklio.Api.Services.Interfaces;

public interface INotificationService
{
    Task<Notification> CreateAsync(Notification notification);

    // Notifikacije korisnika (sa Claim za referencu), najnovije prvo.
    Task<IReadOnlyList<Notification>> GetByUserAsync(string userId);

    Task<int> GetUnreadCountAsync(string userId);

    // Označi jednu pročitanom — scoped na korisnika (ne može tuđu).
    Task MarkReadAsync(int notificationId, string userId);

    Task MarkAllReadAsync(string userId);
}
