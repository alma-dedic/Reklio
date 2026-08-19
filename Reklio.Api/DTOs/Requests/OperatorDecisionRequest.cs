namespace Reklio.Api.DTOs.Requests;

// Operaterska odluka. Razlog je obavezan za odbijanje (prikazuje se kupcu), opcionalan za odobrenje.
public class OperatorDecisionRequest
{
    public string? Reason { get; set; }
}
