namespace Reklio.Api.Services;

// Istek proizvođačke garancije — isti proračun kao warranty_period_used_pct u
// fn_features (WarrantyMonths + PurchaseDate naspram SubmittedAt), čist kod bez
// RAG-a. Istekla = dan podnošenja je poslije zadnjeg dana garancije (used_pct > 100).
public static class WarrantyCalculator
{
    public static bool IsExpired(DateTime purchaseDate, int warrantyMonths, DateTime submittedAt) =>
        submittedAt.Date > purchaseDate.AddMonths(warrantyMonths).Date;
}
