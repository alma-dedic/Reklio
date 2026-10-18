using Reklio.Api.DTOs.Ai;
using Reklio.Api.Models;
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
        var vision = signals.Vision;
        var policy = signals.Policy;
        var risk = signals.Fraud?.RiskScore ?? 0.0;

        // Strategija provjere zavisi od tipa problema:
        // kvar se ne vidi na slici, a "Ostalo" nije modelirano pa uvijek ide čovjeku.
        var isMalfunction = signals.IssueType == IssueTypes.Malfunction;
        var isOther = signals.IssueType == IssueTypes.Other;

        // ── 1. TVRDA ČINJENICA → AUTO-ODBIJ (čist datumski proračun, bez AI-a) ──
        // Kupovina je razriješena i validirana pri unosu (Submit/resolve to nameću),
        // pa je ovdje uvijek prisutna — jedino tvrdo odbijanje je istekla garancija.
        if (signals.WarrantyExpired)
        {
            return DecisionResult.Rejected(
                "OUT_OF_WARRANTY_EXPIRED",
                "Garancija je istekla (proračun po datumu kupovine i trajanju garancije).");
        }

        // ── 2. ČIST SLUČAJ + NIZAK RIZIK → AUTO-ODOBRI (svi uslovi moraju vrijediti) ──
        // Kod kvara nema vizuelne potvrde (proizvod izgleda ispravno) — odlučuju
        // razriješena kupovina, važeća garancija, pokrivenost i rizik.
        var visualOk = isMalfunction
            || (vision is { DamageConfirmed: true } && !signals.ProductMismatch);

        if (!isOther
            && risk < RiskThreshold
            && visualOk
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

        if (isOther)
        {
            factors.Add("Tip problema \"Ostalo\" — slučaj nije modeliran, potrebna ljudska trijaža.");
        }
        // Kod kvara odsustvo vidljivog oštećenja NIJE razlog za sumnju (proizvod izgleda
        // ispravno) — zato se taj faktor dodaje samo kad se oštećenje očekuje na slici.
        else if (!isMalfunction && (vision is null || !vision.DamageConfirmed))
        {
            factors.Add("Oštećenje nije potvrđeno na fotografiji.");
        }

        if (signals.ProductMismatch)
        {
            factors.Add("Slika ne odgovara izabranom proizvodu (tip sa slike ≠ kupljeni proizvod).");
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
