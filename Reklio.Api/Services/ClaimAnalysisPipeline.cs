using Reklio.Api.DTOs.Ai;
using Reklio.Api.Services.Interfaces;

namespace Reklio.Api.Services;

// T4.2 — fiksni pipeline (NE agent loop): servisi se pozivaju unaprijed
// određenim redoslijedom, rezultati se agregiraju i prosljeđuju decision gate-u.
//
// NAMJERNO NIJE REGISTROVANA U DI (Program.cs): nijedan od AI interfejsa još
// nema implementaciju (dolaze u EPIC 5-8). Registracija bi oborila aplikaciju
// pri startu sa "Unable to resolve service for type 'IOcrService'".
public class ClaimAnalysisPipeline
{
    private const string ReceiptEvidenceType = "Receipt";
    private const string PhotoEvidenceType = "Photo";

    private readonly IOcrService _ocr;
    private readonly IVisionService _vision;
    private readonly IRagService _rag;
    private readonly IFraudService _fraud;
    private readonly IClaimService _claims;
    private readonly IClaimEvidenceService _evidence;
    private readonly IPurchaseService _purchases;
    private readonly IProductService _products;

    public ClaimAnalysisPipeline(
        IOcrService ocr,
        IVisionService vision,
        IRagService rag,
        IFraudService fraud,
        IClaimService claims,
        IClaimEvidenceService evidence,
        IPurchaseService purchases,
        IProductService products)
    {
        _ocr = ocr;
        _vision = vision;
        _rag = rag;
        _fraud = fraud;
        _claims = claims;
        _evidence = evidence;
        _purchases = purchases;
        _products = products;
    }

    public async Task<ClaimAnalysisResult> RunAsync(int claimId, CancellationToken cancellationToken = default)
    {
        var claim = await _claims.GetByIdAsync(claimId)
            ?? throw new KeyNotFoundException($"Reklamacija {claimId} ne postoji.");

        var evidence = await _evidence.GetByClaimAsync(claimId);

        // 1. OCR nad dokazom kupovine.
        OcrResult? ocr = null;
        var receipt = evidence.FirstOrDefault(e => e.Type == ReceiptEvidenceType);
        if (receipt is not null)
        {
            ocr = await _ocr.ExtractReceiptAsync(receipt.FilePath, cancellationToken);
        }

        // 2. Vizuelna analiza fotografija oštećenja.
        VisionResult? vision = null;
        var photos = evidence
            .Where(e => e.Type == PhotoEvidenceType)
            .Select(e => e.FilePath)
            .ToList();
        if (photos.Count > 0)
        {
            vision = await _vision.AnalyzeDamageAsync(photos, cancellationToken);
        }

        // 3. Provjera pravilnika/garancije za kategoriju proizvoda.
        RagResult? policy = null;
        var purchase = await _purchases.GetByIdAsync(claim.PurchaseId);
        if (purchase is not null)
        {
            var product = await _products.GetByIdAsync(purchase.ProductId);
            var query = new RagQuery(
                Question: claim.IssueDescription,
                ProductCategory: product?.Category,
                IssueType: claim.IssueType);

            policy = await _rag.CheckPolicyAsync(query, cancellationToken);
        }

        // 4. Procjena rizika.
        var fraud = await _fraud.ScoreClaimAsync(claimId, cancellationToken);

        var result = new ClaimAnalysisResult(claimId, ocr, vision, policy, fraud);

        // TODO (EPIC 9): proslijediti `result` decision gate-u, koji donosi odluku
        // (AutoApproved / AutoRejected / Escalated) i upisuje je na reklamaciju.

        return result;
    }
}