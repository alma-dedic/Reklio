using Reklio.Api.Models;

namespace Reklio.Api.Services;

// Determinirane poruke ishoda kupcu (obavještenje + obrazloženje + naredni korak).
// Koriste ih i pipeline (auto-odluke) i OperatorController (operaterske odluke),
// da kupčeva poruka bude dosljedna bez obzira ko je odlučio.
public static class ClaimOutcomeMessages
{
    // Puno obrazloženje za ekran detalja. Kod kvara je naredni korak provjera u servisu.
    public static string Detail(
        ClaimStatus status, string? reason, string reference, string? issueType = null) => status switch
    {
        ClaimStatus.AutoApproved or ClaimStatus.OperatorApproved =>
            "Vaša reklamacija je prihvaćena."
            + (string.IsNullOrWhiteSpace(reason) ? string.Empty : $" Razlog: {EnsurePeriod(reason)}")
            + (issueType == IssueTypes.Malfunction
                ? $" Sa brojem {reference} i računom dođite u ovlašteni servis radi provjere kvara."
                : $" Sačuvajte broj {reference} za dalji postupak."),
        ClaimStatus.AutoRejected or ClaimStatus.OperatorRejected =>
            $"Reklamacija je odbijena. Razlog: {EnsurePeriod(reason)} " +
            "Ako se ne slažete, možete uložiti prigovor putem korisničke podrške.",
        ClaimStatus.Escalated =>
            "Vaša reklamacija je proslijeđena operateru na pregled. Javit ćemo vam ishod.",
        _ => "Vaša reklamacija je zaprimljena i u obradi je.",
    };

    // Kratka notifikacija (zvonce).
    public static string Notification(ClaimStatus status, string? reason, string reference) => status switch
    {
        ClaimStatus.AutoApproved or ClaimStatus.OperatorApproved =>
            $"Reklamacija {reference} je prihvaćena.",
        ClaimStatus.AutoRejected or ClaimStatus.OperatorRejected =>
            $"Reklamacija {reference} je odbijena: {EnsurePeriod(reason)}",
        ClaimStatus.Escalated =>
            $"Reklamacija {reference} je proslijeđena operateru na pregled.",
        _ => $"Reklamacija {reference}: status je ažuriran.",
    };

    // Kupcu prijateljski razlog za auto-odbijanje iz mašinskog reason_code-a.
    public static string ReasonForCode(string reasonCode) => reasonCode switch
    {
        "OUT_OF_WARRANTY_EXPIRED" => "garancija na proizvodu je istekla",
        _ => "uslovi za prihvatanje reklamacije nisu ispunjeni",
    };

    private static string EnsurePeriod(string? text)
    {
        text = text?.Trim();
        if (string.IsNullOrEmpty(text))
        {
            return "reklamacija nije prihvaćena.";
        }
        return ".!?".Contains(text[^1]) ? text : text + ".";
    }
}
