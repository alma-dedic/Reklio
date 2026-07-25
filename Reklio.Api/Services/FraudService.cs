using System.Data;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Reklio.Api.Data;
using Reklio.Api.DTOs.Ai;
using Reklio.Api.Services.Interfaces;

namespace Reklio.Api.Services;

// T5.9 — pozove fn_features, pošalje brojeve IMENOVANO ka Python servisu,
// vrati procenat rizika + top faktore.
public class FraudService : IFraudService
{
    private readonly ReklioDbContext _db;
    private readonly HttpClient _http;

    public FraudService(ReklioDbContext db, HttpClient http)
    {
        _db = db;
        _http = http;
    }

    public async Task<FraudResult> ScoreClaimAsync(int claimId, CancellationToken cancellationToken = default)
    {
        var features = await ReadFeaturesAsync(claimId, cancellationToken);

        // Šaljemo dict sa IMENIMA (nikad goli niz) — Python presloži po imenu (T5.7).
        var response = await _http.PostAsJsonAsync("/fraud/score", features, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<FraudScoreResponse>(cancellationToken)
            ?? throw new InvalidOperationException("Prazan odgovor fraud servisa.");

        return new FraudResult(result.RiskScore, result.TopFactors);
    }

    // fn_features je jedini izvor istine — čitamo kolone po imenu, ne po poziciji.
    private async Task<Dictionary<string, double?>> ReadFeaturesAsync(int claimId, CancellationToken cancellationToken)
    {
        var features = new Dictionary<string, double?>();
        var connection = _db.Database.GetDbConnection();
        var wasClosed = connection.State == ConnectionState.Closed;
        if (wasClosed)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM dbo.fn_features(@claimId)";
            var parameter = command.CreateParameter();
            parameter.ParameterName = "@claimId";
            parameter.Value = claimId;
            command.Parameters.Add(parameter);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                throw new KeyNotFoundException($"Reklamacija {claimId} ne postoji (nema feature-a).");
            }

            for (var i = 0; i < reader.FieldCount; i++)
            {
                var name = reader.GetName(i);
                if (name == "ClaimId")
                {
                    continue;
                }
                features[name] = await reader.IsDBNullAsync(i, cancellationToken)
                    ? null
                    : Convert.ToDouble(reader.GetValue(i));
            }
        }
        finally
        {
            if (wasClosed)
            {
                await connection.CloseAsync();
            }
        }

        return features;
    }
}
