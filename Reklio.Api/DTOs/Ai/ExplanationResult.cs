namespace Reklio.Api.DTOs.Ai;

// AI preporuka za eskaliranu reklamaciju (savjetodavna, operater odlučuje):
// lean smjera + obrazloženje za operatera + draft razloga za kupca.
public record ExplanationResult(
    string Recommendation,
    string OperatorText,
    string CustomerReason);
