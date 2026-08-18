namespace ProDiabHis.Application.Common;

/// <summary>Ket qua backfill ma hoa PII</summary>
/// <param name="PatientsScanned">So ban ghi benh nhan da quet</param>
/// <param name="PatientsEncrypted">So ban ghi benh nhan da ma hoa moi (chua ma hoa truoc do)</param>
/// <param name="PatientsBlindIndexed">So ban ghi benh nhan duoc cap nhat blind index</param>
/// <param name="InsurancesBlindIndexed">So the bao hiem duoc cap nhat blind index</param>
/// <param name="Errors">Danh sach loi (id + mo ta)</param>
public record PiiBackfillResult(
    int PatientsScanned,
    int PatientsEncrypted,
    int PatientsBlindIndexed,
    int InsurancesBlindIndexed,
    IReadOnlyList<string> Errors);

/// <summary>
/// Backfill du lieu PII cu dang plaintext -> ciphertext + blind index.
/// Idempotent: nhan biet ban ghi da ma hoa qua tien to marker nen chay lai nhieu lan an toan.
/// </summary>
public interface IPiiBackfillService
{
    /// <summary>Chay backfill cho 1 tenant (batchSize ban ghi moi vong)</summary>
    Task<PiiBackfillResult> RunAsync(int tenantId, int batchSize, bool dryRun, CancellationToken ct = default);
}
