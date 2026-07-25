using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Reklio.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFnFeatures : Migration
    {
        // T5.1 — fn_features je jedini izvor istine za sve feature-e (trening i serving).
        // Point-in-time (T5.3): svaki agregat filtrira c2.SubmittedAt < c.SubmittedAt,
        // nikad GETDATE(); strogo "<" isključuje i samu reklamaciju i budućnost.
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE OR ALTER FUNCTION dbo.fn_features(@claim_id INT)
RETURNS TABLE
AS
RETURN
(
    WITH target AS (
        SELECT
            c.Id              AS ClaimId,
            c.UserId          AS UserId,
            c.PurchaseId      AS PurchaseId,
            c.SubmittedAt     AS T,
            p.Amount          AS Amount,
            p.Branch          AS Branch,
            p.PurchaseDate    AS PurchaseDate,
            pr.Category       AS Category,
            pr.WarrantyMonths AS WarrantyMonths,
            u.RegisteredAt    AS RegisteredAt
        FROM Claims c
        JOIN Purchases p    ON p.Id  = c.PurchaseId
        JOIN Products  pr   ON pr.Id = p.ProductId
        JOIN AspNetUsers u  ON u.Id  = c.UserId
        WHERE c.Id = @claim_id
    )
    SELECT
        t.ClaimId,

        -- veza sa kupovinom
        (SELECT COUNT(*)
           FROM Claims c2
          WHERE c2.PurchaseId = t.PurchaseId
            AND c2.SubmittedAt < t.T)                                   AS prior_claims_on_purchase,

        (CASE WHEN EXISTS (
                SELECT 1 FROM Claims c2
                 WHERE c2.PurchaseId = t.PurchaseId
                   AND c2.UserId <> t.UserId
                   AND c2.SubmittedAt < t.T)
              THEN 1 ELSE 0 END)                                        AS purchase_claimed_by_other_account,

        (SELECT COUNT(DISTINCT c2.UserId)
           FROM Claims c2
          WHERE c2.PurchaseId = t.PurchaseId
            AND c2.SubmittedAt < t.T)                                   AS distinct_accounts_on_purchase,

        -- vremenski
        DATEDIFF(DAY, t.PurchaseDate, t.T)                             AS days_purchase_to_claim,

        CAST(100.0 * DATEDIFF(DAY, t.PurchaseDate, t.T)
             / NULLIF(DATEDIFF(DAY, t.PurchaseDate,
                       DATEADD(MONTH, t.WarrantyMonths, t.PurchaseDate)), 0)
             AS FLOAT)                                                  AS warranty_period_used_pct,

        (CASE WHEN DATEDIFF(DAY, t.PurchaseDate, t.T) <= 7
              THEN 1 ELSE 0 END)                                        AS claimed_within_first_n_days,

        -- ponašanje naloga
        (SELECT COUNT(*)
           FROM Claims c2
          WHERE c2.UserId = t.UserId
            AND c2.SubmittedAt < t.T)                                   AS total_claims,

        (SELECT COUNT(*)
           FROM Claims c2
          WHERE c2.UserId = t.UserId
            AND c2.SubmittedAt < t.T
            AND c2.SubmittedAt >= DATEADD(DAY, -30, t.T))               AS claims_last_30d,

        (SELECT COUNT(*)
           FROM Claims c2
          WHERE c2.UserId = t.UserId
            AND c2.SubmittedAt < t.T
            AND c2.SubmittedAt >= DATEADD(DAY, -90, t.T))               AS claims_last_90d,

        (SELECT CAST(DATEDIFF(DAY, MIN(c2.SubmittedAt), MAX(c2.SubmittedAt)) AS FLOAT)
                / NULLIF(COUNT(*) - 1, 0)
           FROM Claims c2
          WHERE c2.UserId = t.UserId
            AND c2.SubmittedAt < t.T)                                   AS mean_days_between_claims,

        DATEDIFF(DAY, t.RegisteredAt, t.T)                             AS account_age_days,

        (SELECT CAST(SUM(CASE WHEN c2.Status IN ('AutoRejected','OperatorRejected') THEN 1 ELSE 0 END) AS FLOAT)
                / NULLIF(SUM(CASE WHEN c2.Status IN ('AutoApproved','AutoRejected','OperatorApproved','OperatorRejected') THEN 1 ELSE 0 END), 0)
           FROM Claims c2
          WHERE c2.UserId = t.UserId
            AND c2.SubmittedAt < t.T)                                   AS prior_rejection_rate,

        -- vrijednost / raspršenost
        CAST(t.Amount AS FLOAT)                                         AS claim_amount,

        CAST(t.Amount AS FLOAT)
          / NULLIF((SELECT AVG(p2.Amount)
                      FROM Claims c2
                      JOIN Purchases p2 ON p2.Id = c2.PurchaseId
                     WHERE c2.UserId = t.UserId
                       AND c2.SubmittedAt < t.T), 0)                    AS amount_vs_user_mean,

        (SELECT COUNT(DISTINCT pr2.Category)
           FROM Claims c2
           JOIN Purchases p2  ON p2.Id  = c2.PurchaseId
           JOIN Products  pr2 ON pr2.Id = p2.ProductId
          WHERE c2.UserId = t.UserId
            AND c2.SubmittedAt < t.T)                                   AS distinct_categories,

        (SELECT COUNT(DISTINCT p2.Branch)
           FROM Claims c2
           JOIN Purchases p2 ON p2.Id = c2.PurchaseId
          WHERE c2.UserId = t.UserId
            AND c2.SubmittedAt < t.T)                                   AS distinct_stores
    FROM target t
);
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS dbo.fn_features;");
        }
    }
}
