namespace Reklio.Api.Models;

public enum ClaimStatus
{
    Submitted,
    Processing,
    AutoApproved,
    AutoRejected,
    Escalated,
    OperatorApproved,
    OperatorRejected
}