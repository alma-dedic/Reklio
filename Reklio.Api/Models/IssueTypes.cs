namespace Reklio.Api.Models;

// Kanonske vrijednosti tipa problema. Moraju se poklapati sa vrijednostima
// koje šalje claim-wizard (ISSUE_TYPES u claim-wizard.ts) — gate grana po njima.
public static class IssueTypes
{
    public const string PhysicalDamage = "Fizičko oštećenje";
    public const string Malfunction = "Proizvod ne radi / kvar";
    public const string Other = "Ostalo";

    public static readonly string[] All = [PhysicalDamage, Malfunction, Other];

    public static bool IsValid(string? value) => value is not null && All.Contains(value);
}
