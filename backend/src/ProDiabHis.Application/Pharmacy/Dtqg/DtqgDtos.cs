namespace ProDiabHis.Application.Pharmacy.Dtqg;

public record DtqgSubmissionResponse(
    Guid Id,
    int PrescriptionId,
    string? MaDonThuoc,
    string? QrPayload,
    string? QrImageUrl,
    string Status,
    string? ErrorCode,
    string? ErrorMessage,
    DateTime? SubmittedAt,
    DateTime? AcceptedAt,
    int RetryCount,
    DateTime? LastRetryAt);

public record DtqgCredentialsRequest(string CskcbId, string PartnerCode, string Token);

public record DtqgCredentialsResponse(
    Guid Id,
    int TenantId,
    string? CskcbId,
    string? PartnerCode,
    string? TokenMasked,
    bool IsActive,
    DateTime? LastTestedAt,
    bool? LastTestOk);

public record DtqgTestResult(bool Ok, int LatencyMs, string? PortalResponse);
