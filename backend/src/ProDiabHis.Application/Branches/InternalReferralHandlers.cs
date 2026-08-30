using Dapper;
using MediatR;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.Branches;

// ─── Commands & Queries (BR-29) ────────────────────────────────────────────────

public record CreateInternalReferralCommand(CreateInternalReferralRequest Request) : IRequest<Result<InternalReferralDto>>;

public record ListIncomingInternalReferralsQuery(string? Status) : IRequest<Result<List<InternalReferralDto>>>;

public record UpdateInternalReferralStatusCommand(int Id, UpdateInternalReferralStatusRequest Request) : IRequest<Result<InternalReferralDto>>;

file static class ReferralSql
{
    public const string Select = @"
        SELECT r.id, r.tenant_id, r.patient_id, p.full_name AS patient_name,
               r.source_branch_id, sb.name AS source_branch_name,
               r.target_branch_id, tb.name AS target_branch_name,
               r.encounter_id, r.referring_doctor_id, r.reason, r.status, r.note,
               r.created_at, r.updated_at
          FROM diab_his_clinic_internal_referrals r
          LEFT JOIN diab_his_pat_patients p ON p.id = r.patient_id
          LEFT JOIN diab_his_sys_branches sb ON sb.id = r.source_branch_id
          LEFT JOIN diab_his_sys_branches tb ON tb.id = r.target_branch_id";
}

file static class ReferralMapper
{
    public static InternalReferralDto Map(dynamic r) => new(
        (int)r.id,
        (int)r.tenant_id,
        (string)r.patient_id,
        (string?)r.patient_name,
        (int)r.source_branch_id,
        (string?)r.source_branch_name,
        (int)r.target_branch_id,
        (string?)r.target_branch_name,
        (string?)r.encounter_id,
        r.referring_doctor_id != null ? Guid.Parse((string)r.referring_doctor_id) : (Guid?)null,
        (string?)r.reason,
        (string)r.status,
        (string?)r.note,
        (DateTime)r.created_at,
        (DateTime)r.updated_at);
}

public class CreateInternalReferralHandler : IRequestHandler<CreateInternalReferralCommand, Result<InternalReferralDto>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;
    private readonly IBranchProvider _branchProvider;

    public CreateInternalReferralHandler(IDapperConnectionFactory db, ICurrentUser currentUser, IBranchProvider branchProvider)
    {
        _db = db; _currentUser = currentUser; _branchProvider = branchProvider;
    }

    public async Task<Result<InternalReferralDto>> Handle(CreateInternalReferralCommand cmd, CancellationToken ct)
    {
        var req = cmd.Request;
        using var conn = _db.CreateConnection();
        var tenantId = _currentUser.TenantId!.Value;
        var sourceBranchId = _branchProvider.BranchId;

        if (sourceBranchId <= 0)
            return Result<InternalReferralDto>.Failure("VALIDATION_ERROR", "Không xác định được chi nhánh nguồn hiện tại");
        if (string.IsNullOrWhiteSpace(req.PatientId))
            return Result<InternalReferralDto>.Failure("VALIDATION_ERROR", "Thiếu mã bệnh nhân");
        if (req.TargetBranchId == sourceBranchId)
            return Result<InternalReferralDto>.Failure("VALIDATION_ERROR", "Chi nhánh đích phải khác chi nhánh nguồn");

        var targetExists = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM diab_his_sys_branches WHERE id = @id AND tenant_id = @tenantId AND deleted_at IS NULL",
            new { id = req.TargetBranchId, tenantId });
        if (targetExists == 0)
            return Result<InternalReferralDto>.Failure("BRANCH_NOT_FOUND", "Không tìm thấy chi nhánh đích");

        var patientExists = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM diab_his_pat_patients WHERE id = @id AND tenant_id = @tenantId AND deleted_at IS NULL",
            new { id = req.PatientId, tenantId });
        if (patientExists == 0)
            return Result<InternalReferralDto>.Failure("PATIENT_NOT_FOUND", "Không tìm thấy bệnh nhân");

        var userId = _currentUser.UserId?.ToString();
        var id = await conn.ExecuteScalarAsync<int>(
            @"INSERT INTO diab_his_clinic_internal_referrals
                (tenant_id, patient_id, source_branch_id, target_branch_id, encounter_id, referring_doctor_id,
                 reason, status, note, created_at, created_by, updated_at, updated_by)
              VALUES
                (@tenantId, @patientId, @sourceBranchId, @targetBranchId, @encounterId, @referringDoctorId,
                 @reason, 'SENT', @note, NOW(), @userId, NOW(), @userId);
              SELECT LAST_INSERT_ID();",
            new
            {
                tenantId,
                patientId = req.PatientId,
                sourceBranchId,
                targetBranchId = req.TargetBranchId,
                encounterId = req.EncounterId,
                referringDoctorId = userId,
                reason = req.Reason,
                note = req.Note,
                userId
            });

        var row = await conn.QueryFirstAsync<dynamic>($"{ReferralSql.Select} WHERE r.id = @id", new { id });
        return Result<InternalReferralDto>.Success(ReferralMapper.Map(row));
    }
}

public class ListIncomingInternalReferralsHandler : IRequestHandler<ListIncomingInternalReferralsQuery, Result<List<InternalReferralDto>>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;
    private readonly IBranchProvider _branchProvider;

    public ListIncomingInternalReferralsHandler(IDapperConnectionFactory db, ICurrentUser currentUser, IBranchProvider branchProvider)
    {
        _db = db; _currentUser = currentUser; _branchProvider = branchProvider;
    }

    public async Task<Result<List<InternalReferralDto>>> Handle(ListIncomingInternalReferralsQuery q, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var tenantId = _currentUser.TenantId!.Value;

        // BR-60 pattern: user chi thay ban ghi ma nguon HOAC dich nam trong pham vi chi nhanh duoc gan,
        // tru khi co branch.cross_view (IgnoreBranchFilter=true).
        var where = "WHERE r.tenant_id = @tenantId AND r.deleted_at IS NULL";
        if (!_branchProvider.IgnoreBranchFilter)
        {
            if (_branchProvider.AllowedBranchIds.Count == 0)
                where += " AND 1 = 0";
            else
                where += " AND (r.source_branch_id IN @allowedIds OR r.target_branch_id IN @allowedIds)";
        }

        var statuses = string.IsNullOrWhiteSpace(q.Status)
            ? new[] { "SENT", "ACCEPTED" }
            : new[] { q.Status };
        where += " AND r.status IN @statuses";

        var rows = await conn.QueryAsync<dynamic>(
            $"{ReferralSql.Select} {where} ORDER BY r.created_at DESC",
            new { tenantId, allowedIds = _branchProvider.AllowedBranchIds, statuses });

        return Result<List<InternalReferralDto>>.Success(rows.Select(ReferralMapper.Map).ToList());
    }
}

public class UpdateInternalReferralStatusHandler : IRequestHandler<UpdateInternalReferralStatusCommand, Result<InternalReferralDto>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;
    private readonly IBranchProvider _branchProvider;

    public UpdateInternalReferralStatusHandler(IDapperConnectionFactory db, ICurrentUser currentUser, IBranchProvider branchProvider)
    {
        _db = db; _currentUser = currentUser; _branchProvider = branchProvider;
    }

    public async Task<Result<InternalReferralDto>> Handle(UpdateInternalReferralStatusCommand cmd, CancellationToken ct)
    {
        var req = cmd.Request;
        if (!Domain.Entities.InternalReferralStatus.All.Contains(req.Status) || req.Status == "SENT")
            return Result<InternalReferralDto>.Failure("VALIDATION_ERROR", "Trạng thái không hợp lệ");

        using var conn = _db.CreateConnection();
        var tenantId = _currentUser.TenantId!.Value;

        var existing = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT id, source_branch_id, target_branch_id, status FROM diab_his_clinic_internal_referrals WHERE id = @id AND tenant_id = @tenantId AND deleted_at IS NULL",
            new { id = cmd.Id, tenantId });
        if (existing == null)
            return Result<InternalReferralDto>.Failure("REFERRAL_NOT_FOUND", "Không tìm thấy phiếu chuyển cơ sở");

        if (!_branchProvider.IgnoreBranchFilter)
        {
            int src = (int)existing.source_branch_id, tgt = (int)existing.target_branch_id;
            if (!_branchProvider.AllowedBranchIds.Contains(src) && !_branchProvider.AllowedBranchIds.Contains(tgt))
                return Result<InternalReferralDto>.Failure("BRANCH_ACCESS_DENIED", "Bạn không có quyền truy cập phiếu chuyển cơ sở này");
        }

        var currentStatus = (string)existing.status;
        if (currentStatus is "COMPLETED" or "CANCELLED")
            return Result<InternalReferralDto>.Failure("REFERRAL_LOCKED", "Phiếu chuyển cơ sở đã kết thúc, không thể cập nhật");

        var userId = _currentUser.UserId?.ToString();
        await conn.ExecuteAsync(
            "UPDATE diab_his_clinic_internal_referrals SET status = @status, note = COALESCE(@note, note), updated_at = NOW(), updated_by = @userId WHERE id = @id AND tenant_id = @tenantId",
            new { id = cmd.Id, tenantId, status = req.Status, note = req.Note, userId });

        var row = await conn.QueryFirstAsync<dynamic>($"{ReferralSql.Select} WHERE r.id = @id", new { id = cmd.Id });
        return Result<InternalReferralDto>.Success(ReferralMapper.Map(row));
    }
}
