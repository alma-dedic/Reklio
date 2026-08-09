namespace Reklio.Api.DTOs.Ai;

public record RagQuery(
    string Question,
    string? ProductCategory,
    string? IssueType);

// Odgovara /analyze/policy (T7.5). Covered=false kada se primjenjuje izuzetak.
public record RagResult(
    bool Covered,
    string? ApplicableExclusion,
    string? CitedArticle,
    string Answer);
