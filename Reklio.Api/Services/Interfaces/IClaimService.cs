using Reklio.Api.Models;

namespace Reklio.Api.Services.Interfaces;

public interface IClaimService
{
    Task<Claim?> GetByIdAsync(int id);

    Task<Claim> CreateAsync(Claim claim);

    // Reklamacije jednog korisnika (sa Purchase+Product) — za listu na dashboardu.
    Task<IReadOnlyList<Claim>> GetByUserAsync(string userId);

    // Jedna reklamacija sa Purchase+Product+User — za ekran detalja / operatera.
    Task<Claim?> GetDetailAsync(int id);

    // Eskalirane reklamacije, sortirane po riziku (najviši prvo) — operaterov red.
    Task<IReadOnlyList<Claim>> GetEscalatedAsync();

    // Operaterska odluka: Escalated → OperatorApproved/Rejected + upiše operatera.
    Task ResolveByOperatorAsync(int claimId, string operatorId, ClaimStatus newStatus);

    // Mijenja status uz provjeru state machine-a (T2.3). Baca ako je prelaz nevalidan.
    Task UpdateStatusAsync(int claimId, ClaimStatus newStatus);

    // Veže reklamaciju za kupovinu (fizička se razrješava tek nakon OCR-a u pipeline-u).
    Task LinkPurchaseAsync(int claimId, int purchaseId);

    // T9 — atomski upisuje rizik i fiksira odluku gate-a (prelaz uz provjeru).
    Task ApplyDecisionAsync(int claimId, double? riskScore, ClaimStatus newStatus);

    // T9.3 — upisuje operatersko LLM objašnjenje nakon fiksirane odluke.
    Task SetExplanationAsync(int claimId, string aiExplanation);

    // Upisuje sažetak AI nalaza (AnalysisJson) za ekran detalja.
    Task SaveAnalysisAsync(int claimId, string analysisJson);

    // Ažurira samo korisničko objašnjenje unutar AnalysisJson (npr. nakon operaterske odluke).
    Task UpdateCustomerExplanationAsync(int claimId, string text);
}