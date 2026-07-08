using System.Data;
using Dapper;
using MediatR;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.ChronicCare;

// ─── Queries & Commands ─────────────────────────────────────────────────────
public record ListRecallQuery(string? Status, DateOnly? DueBefore, int Page, int PageSize)
    : IRequest<Result<PagedResult<RecallItem>>>;

public record UpdateRecallStatusCommand(Guid Id, string Status, string? Note, string? Channel)
    : IRequest<Result<bool>>;

public record GetCarePathwayTargetsQuery(string Code) : IRequest<Result<IReadOnlyList<CarePathwayTargetDto>>>;

// ─── Handlers ───────────────────────────────────────────────────────────────
public class ListRecallQueryHandler : IRequestHandler<ListRecallQuery, Result<PagedResult<RecallItem>>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;

    public ListRecallQueryHandler(IDapperConnectionFactory db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<PagedResult<RecallItem>>> Handle(ListRecallQuery q, CancellationToken ct)
    {
        using var conn = (IDbConnection)_db.CreateConnection();
        var tenantId = _currentUser.TenantId!.Value;

        var where = new List<string> { "r.tenant_id = @tenantId", "r.deleted_at IS NULL" };
        var prm = new DynamicParameters();
        prm.Add("tenantId", tenantId);

        if (!string.IsNullOrWhiteSpace(q.Status)) { where.Add("r.status = @status"); prm.Add("status", q.Status); }
        if (q.DueBefore.HasValue) { where.Add("r.due_date <= @dueBefore"); prm.Add("dueBefore", q.DueBefore.Value); }

        var whereClause = string.Join(" AND ", where);

        var total = await conn.ExecuteScalarAsync<int>(
            $"SELECT COUNT(*) FROM diab_his_cli_followup_recall r WHERE {whereClause}", prm);

        var page = q.Page < 1 ? 1 : q.Page;
        var pageSize = q.PageSize < 1 ? 20 : q.PageSize;
        prm.Add("offset", (page - 1) * pageSize);
        prm.Add("limit", pageSize);

        var rows = await conn.QueryAsync<RecallRow>(
            $@"SELECT r.id AS Id, r.patient_id AS PatientId, pat.code AS PatientCode, pat.full_name AS PatientFullName,
                      pat.phone AS Phone, r.recall_type AS RecallType, r.due_date AS DueDate,
                      r.priority AS Priority, r.status AS Status, r.channel AS Channel, r.note AS Note,
                      r.contacted_at AS ContactedAt, r.created_at AS CreatedAt
               FROM diab_his_cli_followup_recall r
               JOIN diab_his_pat_patients pat ON pat.id = r.patient_id AND pat.tenant_id = r.tenant_id
               WHERE {whereClause}
               ORDER BY r.priority DESC, r.due_date ASC
               LIMIT @limit OFFSET @offset", prm);

        var items = rows.Select(r => new RecallItem(
            Guid.Parse(r.Id), Guid.Parse(r.PatientId), r.PatientCode, r.PatientFullName, r.Phone,
            r.RecallType, r.DueDate.HasValue ? DateOnly.FromDateTime(r.DueDate.Value) : null,
            r.Priority, r.Status, r.Channel, r.Note, r.ContactedAt, r.CreatedAt)).ToList();

        return Result<PagedResult<RecallItem>>.Success(new PagedResult<RecallItem>(items, page, pageSize, total));
    }

    private class RecallRow
    {
        public string Id { get; set; } = "";
        public string PatientId { get; set; } = "";
        public string PatientCode { get; set; } = "";
        public string PatientFullName { get; set; } = "";
        public string? Phone { get; set; }
        public string RecallType { get; set; } = "";
        public DateTime? DueDate { get; set; }
        public string Priority { get; set; } = "";
        public string Status { get; set; } = "";
        public string? Channel { get; set; }
        public string? Note { get; set; }
        public DateTime? ContactedAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

public class UpdateRecallStatusCommandHandler : IRequestHandler<UpdateRecallStatusCommand, Result<bool>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;

    private static readonly string[] ValidStatuses = ["PENDING", "CONTACTED", "SCHEDULED", "DONE", "DISMISSED"];

    public UpdateRecallStatusCommandHandler(IDapperConnectionFactory db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<bool>> Handle(UpdateRecallStatusCommand cmd, CancellationToken ct)
    {
        if (!ValidStatuses.Contains(cmd.Status))
            return Result<bool>.Failure("RECALL_INVALID_STATUS", "Trạng thái recall không hợp lệ");

        using var conn = (IDbConnection)_db.CreateConnection();
        var tenantId = _currentUser.TenantId!.Value;
        var userId = _currentUser.UserId;

        var isContactAction = cmd.Status is "CONTACTED" or "SCHEDULED" or "DONE";

        var affected = await conn.ExecuteAsync(
            @"UPDATE diab_his_cli_followup_recall SET
                status = @status, note = COALESCE(@note, note), channel = COALESCE(@channel, channel),
                contacted_at = CASE WHEN @isContactAction THEN NOW(3) ELSE contacted_at END,
                contacted_by = CASE WHEN @isContactAction THEN @userId ELSE contacted_by END,
                updated_at = NOW(3)
              WHERE id = @id AND tenant_id = @tenantId AND deleted_at IS NULL",
            new
            {
                id = cmd.Id.ToString(), tenantId, status = cmd.Status, note = cmd.Note, channel = cmd.Channel,
                isContactAction, userId = userId?.ToString()
            });

        if (affected == 0) return Result<bool>.Failure("RECALL_NOT_FOUND", "Không tìm thấy recall");
        return Result<bool>.Success(true);
    }
}

public class GetCarePathwayTargetsQueryHandler
    : IRequestHandler<GetCarePathwayTargetsQuery, Result<IReadOnlyList<CarePathwayTargetDto>>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;

    public GetCarePathwayTargetsQueryHandler(IDapperConnectionFactory db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<CarePathwayTargetDto>>> Handle(GetCarePathwayTargetsQuery q, CancellationToken ct)
    {
        using var conn = (IDbConnection)_db.CreateConnection();
        var tenantId = _currentUser.TenantId!.Value;

        var rows = await conn.QueryAsync<(string Param, string TargetOp, decimal TargetValue, string? Unit, string? Note, int? TenantId)>(
            @"SELECT param AS Param, target_op AS TargetOp, target_value AS TargetValue, unit AS Unit, note AS Note, tenant_id AS TenantId
              FROM diab_his_cli_care_pathway_target
              WHERE code = @code AND (tenant_id = @tenantId OR tenant_id IS NULL)
              ORDER BY (tenant_id IS NULL) ASC", new { code = q.Code, tenantId });

        var byParam = new Dictionary<string, CarePathwayTargetDto>();
        foreach (var r in rows)
        {
            if (!byParam.ContainsKey(r.Param))
                byParam[r.Param] = new CarePathwayTargetDto(r.Param, r.TargetOp, r.TargetValue, r.Unit, r.Note);
        }

        return Result<IReadOnlyList<CarePathwayTargetDto>>.Success(byParam.Values.ToList().AsReadOnly());
    }
}
