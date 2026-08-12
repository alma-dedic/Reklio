namespace Reklio.Api.DTOs.Responses;

public record NotificationResponse(
    int Id,
    string Message,
    bool IsRead,
    int ClaimId,
    string ClaimReference,
    DateTime CreatedAt);
