using Reklio.Api.Models;

namespace Reklio.Api.DTOs.Ai;

// Ishod decision gate-a (T9.1). Status je konačna odluka, ReasonCode mašinski
// razlog, Factors ljudski čitljivi signali (ulaz za LLM objašnjenje i operatera).
public record DecisionResult(
    ClaimStatus Status,
    string ReasonCode,
    IReadOnlyList<string> Factors)
{
    public static DecisionResult Rejected(string code, string detail) =>
        new(ClaimStatus.AutoRejected, code, [detail]);

    public static DecisionResult Approved() =>
        new(ClaimStatus.AutoApproved, "CLEAN_LOW_RISK",
            ["Validan račun, nizak rizik, potvrđeno oštećenje i pokriveno pravilnikom."]);

    public static DecisionResult Escalated(IReadOnlyList<string> factors) =>
        new(ClaimStatus.Escalated, "NEEDS_REVIEW", factors);
}
