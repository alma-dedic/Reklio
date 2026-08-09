namespace Reklio.Api.DTOs.Ai;

// Dva teksta LLM objašnjenja (T9.3): jedan za korisnika, jedan za operatera.
public record ExplanationResult(
    string UserText,
    string OperatorText);
