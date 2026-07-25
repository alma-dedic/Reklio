namespace Reklio.Api.DTOs.Ai;

// Upit i odgovor nad korpusom pravilnika/garancije (implementacija: EPIC 7).
public record RagQuery(
    string Question,
    string? ProductCategory,
    string? IssueType);

public record RagCitation(
    string SourceName,
    string Excerpt);

public record RagResult(
    string Answer,
    IReadOnlyList<RagCitation> Citations,
    double Confidence);