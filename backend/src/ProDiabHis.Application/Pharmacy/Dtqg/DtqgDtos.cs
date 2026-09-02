namespace ProDiabHis.Application.Pharmacy.Dtqg;

public record DtqgSubmissionResponse(
    Guid Id,
    // BUG FIX (QC print-button audit 2026-09-02): truoc la "int PrescriptionId" nhung cot
    // diab_his_int_dtqg_submissions.prescription_id thuc te luu GUID dang CHAR(36) (khop
    // pha_prescriptions.ID) -> ep kieu (int)row.prescription_id trong MapSubmission luon
    // nem InvalidCastException -> GET /prescriptions/{id}/dtqg/status tra 500 lien tuc.
    Guid PrescriptionId,
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
