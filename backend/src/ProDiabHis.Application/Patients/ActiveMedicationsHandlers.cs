using System.Data;
using Dapper;
using MediatR;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.Patients;

// BM-32: "Thuoc dang dung khac" - danh sach thuoc tu CAC don thuoc DA KY (SIGNED) khac cua
// cung benh nhan (khong gioi han 1 encounter), con hieu luc tinh theo signed_at + so_ngay_dung.
// Dung cho tab Don thuoc man Kham benh de bac si biet benh nhan dang dung thuoc gi truoc khi
// ke them (tranh trung lap / tuong tac). Doc qua Dapper (read-only), tenant-scoped.
public record GetActiveMedicationsQuery(Guid PatientId) : IRequest<Result<IReadOnlyList<ActiveMedicationResponse>>>;

public record ActiveMedicationResponse(
    Guid PrescriptionId,
    Guid PrescriptionItemId,
    string DrugName,
    string? Strength,
    string DosageSummary,
    DateTime PrescribedAt,
    DateTime? ActiveUntil);

public class GetActiveMedicationsQueryHandler
    : IRequestHandler<GetActiveMedicationsQuery, Result<IReadOnlyList<ActiveMedicationResponse>>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;

    public GetActiveMedicationsQueryHandler(IDapperConnectionFactory db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<ActiveMedicationResponse>>> Handle(
        GetActiveMedicationsQuery q, CancellationToken ct)
    {
        using var conn = (IDbConnection)_db.CreateConnection();
        var tenantId = _currentUser.TenantId!.Value;

        var patientExists = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM diab_his_pat_patients WHERE id = @id AND tenant_id = @tenantId AND deleted_at IS NULL",
            new { id = q.PatientId.ToString(), tenantId });
        if (patientExists == 0)
            return Result<IReadOnlyList<ActiveMedicationResponse>>.Failure("PATIENT_NOT_FOUND", "Khong tim thay benh nhan.");

        // "Con hieu luc" = signed_at + duration_days >= hom nay (hoac khong co duration_days
        // thi coi la con hieu luc, vi khong the tinh duoc ngay het han).
        var rows = await conn.QueryAsync<ActiveMedicationRow>(
            @"SELECT p.id AS PrescriptionId, i.id AS PrescriptionItemId,
                     i.drug_name AS DrugName, i.drug_strength AS Strength,
                     i.dosage AS Dosage, i.frequency AS Frequency,
                     p.signed_at AS PrescribedAt, i.duration_days AS DurationDays
              FROM diab_his_pha_prescriptions p
              JOIN diab_his_pha_prescription_items i ON i.prescription_id = p.id AND i.tenant_id = p.tenant_id
              WHERE p.tenant_id = @tenantId AND p.patient_id = @patientId
                AND p.status = 'SIGNED' AND p.deleted_at IS NULL AND i.deleted_at IS NULL
                AND (i.duration_days IS NULL OR DATE_ADD(p.signed_at, INTERVAL i.duration_days DAY) >= CURDATE())
              ORDER BY p.signed_at DESC",
            new { tenantId, patientId = q.PatientId.ToString() });

        var result = rows.Select(r => new ActiveMedicationResponse(
            Guid.TryParse(r.PrescriptionId, out var pid) ? pid : Guid.Empty,
            Guid.TryParse(r.PrescriptionItemId, out var iid) ? iid : Guid.Empty,
            r.DrugName ?? "",
            r.Strength,
            string.IsNullOrWhiteSpace(r.Dosage) ? (r.Frequency ?? "") : $"{r.Dosage} - {r.Frequency}",
            r.PrescribedAt,
            r.DurationDays.HasValue ? r.PrescribedAt.AddDays(r.DurationDays.Value) : null))
            .ToList();

        return Result<IReadOnlyList<ActiveMedicationResponse>>.Success(result.AsReadOnly());
    }

    private class ActiveMedicationRow
    {
        public string? PrescriptionId { get; set; }
        public string? PrescriptionItemId { get; set; }
        public string? DrugName { get; set; }
        public string? Strength { get; set; }
        public string? Dosage { get; set; }
        public string? Frequency { get; set; }
        public DateTime PrescribedAt { get; set; }
        public int? DurationDays { get; set; }
    }
}
