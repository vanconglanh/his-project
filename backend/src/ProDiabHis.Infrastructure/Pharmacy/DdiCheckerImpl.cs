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

    public async Task<IReadOnlyList<DdiWarning>> CheckAsync(IReadOnlyList<int> drugIds, CancellationToken ct = default)
    {
        if (drugIds.Count < 2) return [];

        using var conn = (IDbConnection)_db.CreateConnection();

        var idList = string.Join(",", drugIds);

        var rows = await conn.QueryAsync<dynamic>(
            $@"SELECT r.drug1_id, d1.DRUG_NAME as drug1_name,
                      r.drug2_id, d2.DRUG_NAME as drug2_name,
                      r.severity, r.description, r.evidence_level
               FROM diab_his_pha_ddi_rules r
               JOIN pha_drug_master d1 ON d1.ID = r.drug1_id
               JOIN pha_drug_master d2 ON d2.ID = r.drug2_id
               WHERE r.drug1_id IN ({idList}) AND r.drug2_id IN ({idList})
                 AND r.deleted_at IS NULL");

        return rows.Select(r => new DdiWarning(
            (int)r.drug1_id, (string)r.drug1_name,
            (int)r.drug2_id, (string)r.drug2_name,
            (string)r.severity, (string)r.description, (string)r.evidence_level)).ToList();
    }
}
