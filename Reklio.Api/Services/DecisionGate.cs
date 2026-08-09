using Reklio.Api.DTOs.Ai;
using Reklio.Api.DTOs.Validation;
using Reklio.Api.Services.Interfaces;

namespace Reklio.Api.Services;

// T9.1 / T9.2 — konzervativni decision gate.
// Redoslijed: tvrde činjenice (auto-odbij) → čist slučaj + nizak rizik (auto-odobri)
// → sve ostalo (eskalacija). Odbijanje se oslanja ISKLJUČIVO na kod/SQL, nikad na
// RAG interpretaciju. Visok rizik je mek signal — vodi u eskalaciju, nikad u odbijanje.
public class DecisionGate : IDecisionGate
{
    // Prag rizika iz T5.8 (cross_val_predict na punim podacima). Tačan broj, ne zaokružen.
    public const double RiskThreshold = 0.8684;

    public DecisionResult Evaluate(ClaimAnalysisResult signals)
    {
        var validation = signals.Validation;
        var vision = signals.Vision;
        var policy = signals.Policy;
        var risk = signals.Fraud?.RiskScore ?? 0.0;

        // ── 1. TVRDE ČINJENICE → AUTO-ODBIJ (samo čiste kodne/SQL provjere) ──
        // 1a. Račun ne odgovara nijednoj kupovini (FindByDocumentNumber = null).
        if (validation is { Status: ReceiptValidationStatus.NotFound })
        {
            return DecisionResult.Rejected(
                "PURCHASE_NOT_FOUND",
                "Broj računa ne odgovara nijednoj evidentiranoj kupovini.");
        }

        // 1b. Garancija istekla — datumski proračun, bez RAG-a.
        if (signals.WarrantyExpired)
        {
            return DecisionResult.Rejected(
                "OUT_OF_WARRANTY_EXPIRED",
                "Garancija je istekla (proračun po datumu kupovine i trajanju garancije).");
        }

        // ── 2. ČIST SLUČAJ + NIZAK RIZIK → AUTO-ODOBRI (svi uslovi moraju vrijediti) ──
        // WarrantyExpired je ovdje već false — grana 1b bi ranije presjekla.
        if (validation is { Status: ReceiptValidationStatus.Valid }
            && risk < RiskThreshold
            && vision is { DamageConfirmed: true }
            && policy is { Covered: true })
        {
            return DecisionResult.Approved();
        }

        // ── 3. SVE OSTALO → ESKALACIJA (meki signali, uklj. RAG izuzetke, nikad ne odbijaju) ──
        var factors = new List<string>();

        if (risk >= RiskThreshold)
        {
            factors.Add($"Visok rizik (score {risk:0.0000} ≥ prag {RiskThreshold:0.0000}).");
        }

        if (validation is null)
        {
            factors.Add("Nema priloženog dokaza za validaciju kupovine.");
        }
        else if (validation.Status == ReceiptValidationStatus.Mismatch)
        {
            factors.Add("Račun se ne poklapa sa kupovinom (iznos ili datum van tolerancije).");
        }

        if (vision is null || !vision.DamageConfirmed)
        {
            factors.Add("Oštećenje nije potvrđeno na fotografiji.");
        }

        if (policy is { ApplicableExclusion: not null })
        {
            factors.Add($"RAG našao mogući izuzetak: {policy.ApplicableExclusion} (za ljudsku provjeru).");
        }
        else if (policy is null || !policy.Covered)
        {
            factors.Add("Pokrivenost pravilnikom nije jednoznačna.");
        }

        if (factors.Count == 0)
        {
            factors.Add("Slučaj ne zadovoljava kriterije za automatsku odluku.");
        }

        return DecisionResult.Escalated(factors);
    }
}
