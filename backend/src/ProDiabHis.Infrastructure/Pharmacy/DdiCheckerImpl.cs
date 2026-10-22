using System.Data;
using Dapper;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.Pharmacy;
using ProDiabHis.Application.Pharmacy.Prescriptions;

namespace ProDiabHis.Infrastructure.Pharmacy;

public class DdiCheckerImpl : IDdiChecker
{
    private readonly IDapperConnectionFactory _db;

    public DdiCheckerImpl(IDapperConnectionFactory db)
    {
        _db = db;
    }

    /// <summary>
    /// drugIds la GUID thuoc (bang active diab_his_pha_drugs). Ten thuoc lay tu bang nay;
    /// DdiWarning giu Drug1Id/Drug2Id kieu int de khong doi hop dong FE (GUID khong map duoc -> 0,
    /// giong nhanh CDSS trong PrescriptionHandlers).
    /// </summary>
    public async Task<IReadOnlyList<DdiWarning>> CheckAsync(IReadOnlyList<string> drugIds, CancellationToken ct = default)
    {
        if (drugIds.Count < 2) return [];

        using var conn = (IDbConnection)_db.CreateConnection();

        var rows = await conn.QueryAsync<dynamic>(
            @"SELECT COALESCE(NULLIF(d1.name_vi, ''), d1.name) AS drug1_name,
                      COALESCE(NULLIF(d2.name_vi, ''), d2.name) AS drug2_name,
                      r.severity, r.description, r.evidence_level
               FROM diab_his_pha_ddi_rules r
               JOIN diab_his_pha_drugs d1 ON d1.id = r.drug1_id
               JOIN diab_his_pha_drugs d2 ON d2.id = r.drug2_id
               WHERE r.drug1_id IN @ids AND r.drug2_id IN @ids
                 AND r.deleted_at IS NULL",
            new { ids = drugIds });

        return rows.Select(r => new DdiWarning(
            0, (string)r.drug1_name,
            0, (string)r.drug2_name,
            (string)r.severity, (string)(r.description ?? ""), (string)(r.evidence_level ?? ""))).ToList();
    }
}
