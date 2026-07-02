using ProDiabHis.Application.Pharmacy.Prescriptions;
using ProDiabHis.Application.Pharmacy.Warehouse;

namespace ProDiabHis.Application.Pharmacy;

// ── DDI Checker ──────────────────────────────────────────────────────────────
public interface IDdiChecker
{
    /// <summary>Kiem tra tuong tac thuoc cho danh sach drug_id.</summary>
    Task<IReadOnlyList<DdiWarning>> CheckAsync(IReadOnlyList<int> drugIds, CancellationToken ct = default);
}

// ── USB Token Signer (server-side verify only; client holds private key) ─────
public interface IUsbTokenSigner
{
    /// <summary>
    /// Verify PKCS#7 detached signature sent by client.
    /// Dev/mock: accept any non-empty base64, return fake serial.
    /// Prod: verify cert chain + CRL.
    /// </summary>
    Task<SignatureVerifyResult> VerifyAsync(string base64Signature, string certificateThumbprint, CancellationToken ct = default);
}

public record SignatureVerifyResult(bool IsValid, string? SerialNumber, string? SubjectName, string? ErrorReason = null);

// ── FEFO Strategy ─────────────────────────────────────────────────────────────
public interface IFefoStrategy
{
    /// <summary>
    /// Auto-pick batches for a drug using FEFO (earliest expiry first).
    /// Returns list of (batch_no, expiry_date, qty_to_pick) or throws if insufficient.
    /// </summary>
    Task<IReadOnlyList<BatchPick>> PickAsync(int warehouseId, int tenantId, int drugId, decimal quantityNeeded, CancellationToken ct = default);
}

public record BatchPick(string BatchNo, DateOnly ExpiryDate, decimal Quantity, decimal UnitCost);

// ── Drug CucQld Sync ──────────────────────────────────────────────────────────
public interface IDrugCucQldSync
{
    /// <summary>Sync drug master with CSDL Duoc Quoc Gia. mode: FULL | INCREMENTAL.</summary>
    Task<Guid> EnqueueSyncJobAsync(string mode, DateTime? since, CancellationToken ct = default);
}

// ── Cuc QLD Lien Thong ────────────────────────────────────────────────────────
public interface ICucQldLienThong
{
    /// <summary>Bao nhap kho voi Cuc QLD (GPP). Mock in sprint 6-7.</summary>
    Task ReportImportAsync(Guid grnId, CancellationToken ct = default);

    /// <summary>Bao xuat kho voi Cuc QLD (GPP). Mock in sprint 6-7.</summary>
    Task ReportExportAsync(Guid dispenseRecordId, CancellationToken ct = default);
}

// ── Excel Importer ────────────────────────────────────────────────────────────
public interface IExcelImporter
{
    Task<DrugImportResult> ImportDrugsAsync(Stream excelStream, string mode, int tenantId, int userId, CancellationToken ct = default);
}

public record DrugImportResult(int TotalRows, int Inserted, int Updated, int Failed, IReadOnlyList<DrugImportError> Errors);
public record DrugImportError(int Row, string Message);

// ── DTQG Client ───────────────────────────────────────────────────────────────
public interface IDtqgClient
{
    Task<DtqgSubmitResult> SubmitPrescriptionAsync(DtqgSubmitPayload payload, CancellationToken ct = default);
    Task<DtqgStatusResult> GetStatusAsync(string maDonThuoc, CancellationToken ct = default);
    Task<bool> CancelAsync(string maDonThuoc, string reason, CancellationToken ct = default);
    Task<DtqgPingResult> PingAsync(CancellationToken ct = default);
}

public record DtqgSubmitPayload(int TenantId, int PrescriptionId, string CskcbId, string PartnerCode, object PrescriptionData);
public record DtqgSubmitResult(bool Success, string? MaDonThuoc, string? ErrorCode, string? ErrorMessage);
public record DtqgStatusResult(string Status, string? MaDonThuoc, string? ErrorCode);
public record DtqgPingResult(bool Ok, int LatencyMs, string? PortalResponse);

// ── QR Generator ─────────────────────────────────────────────────────────────
public interface IDtqgQrGenerator
{
    /// <summary>Generate QR PNG bytes from ma_don_thuoc and portal URL.</summary>
    byte[] GenerateQrPng(string maDonThuoc, string portalUrl);
}
