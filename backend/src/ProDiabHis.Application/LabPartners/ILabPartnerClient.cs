namespace ProDiabHis.Application.LabPartners;

/// <summary>HTTP REST client gui chi dinh XN toi partner va kiem tra ket noi</summary>
public interface ILabPartnerClient
{
    /// <summary>Test ket noi: GET ping endpoint cua partner, timeout 5s</summary>
    Task<TestConnectionResponse> TestConnectionAsync(string endpointUrl, string authType,
        string? apiKey, string? bearerToken, CancellationToken ct = default);

    /// <summary>Gui payload JSON toi partner endpoint, tra ve external_order_id</summary>
    Task<LabPartnerSendResult> SendOrderAsync(string endpointUrl, string authType,
        string? apiKey, string? bearerToken, object payload, CancellationToken ct = default);
}

public record LabPartnerSendResult(bool Success, string? ExternalOrderId, string? ErrorMessage);
