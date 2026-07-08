using System.Data;
using Dapper;
using MediatR;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.Cdss;

// ─── Commands & Queries ────────────────────────────────────────────────────
public record EvaluatePrescriptionCdssQuery(CdssCheckRequest Req) : IRequest<Result<CdssCheckResponse>>;

public record RecordCdssOverrideCommand(CdssOverrideRequest Req) : IRequest<Result<Guid>>;

public record ListCdssRulesQuery() : IRequest<Result<IReadOnlyList<CdssRuleResponse>>>;

public record UpsertCdssRuleCommand(CdssRuleUpsertRequest Req) : IRequest<Result<Guid>>;

// ─── Handlers ───────────────────────────────────────────────────────────────
public class EvaluatePrescriptionCdssQueryHandler
    : IRequestHandler<EvaluatePrescriptionCdssQuery, Result<CdssCheckResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;
    private readonly ICdssEngine _engine;

    public EvaluatePrescriptionCdssQueryHandler(IDapperConnectionFactory db, ICurrentUser currentUser, ICdssEngine engine)
    {
        _db = db;
        _currentUser = currentUser;
        _engine = engine;
    }

    public async Task<Result<CdssCheckResponse>> Handle(EvaluatePrescriptionCdssQuery q, CancellationToken ct)
    {
        using var conn = (IDbConnection)_db.CreateConnection();
        var tenantId = _currentUser.TenantId!.Value;

        var drugs = new List<PrescribedDrug>();
        foreach (var item in q.Req.Items)
        {
            var ingredient = item.Ingredient;
            var atcCode = item.AtcCode;

            if (string.IsNullOrWhiteSpace(ingredient) && !string.IsNullOrWhiteSpace(item.DrugId))
            {
                var row = await conn.QueryFirstOrDefaultAsync<(string? GenericName, string? AtcCode)?>(
                    "SELECT generic_name AS GenericName, atc_code AS AtcCode FROM diab_his_pha_drugs WHERE id = @id",
                    new { id = item.DrugId });
                if (row.HasValue)
                {
                    ingredient ??= row.Value.GenericName;
                    atcCode ??= row.Value.AtcCode;
                }
            }

            drugs.Add(new PrescribedDrug(item.DrugId ?? "", ingredient, atcCode));
        }

        var evalCtx = new CdssEvaluationContext(tenantId, q.Req.PatientId, q.Req.EncounterId, q.Req.PrescriptionId, drugs);
        var response = await _engine.EvaluateAsync(evalCtx, "CHECK", logEvents: true, ct);

        return Result<CdssCheckResponse>.Success(response);
    }
}

public class RecordCdssOverrideCommandHandler : IRequestHandler<RecordCdssOverrideCommand, Result<Guid>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;

    public RecordCdssOverrideCommandHandler(IDapperConnectionFactory db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<Guid>> Handle(RecordCdssOverrideCommand cmd, CancellationToken ct)
    {
        var req = cmd.Req;
        if (string.IsNullOrWhiteSpace(req.OverrideReason))
            return Result<Guid>.Failure("CDSS_OVERRIDE_REASON_REQUIRED", "Vui lòng nhập lý do vượt cảnh báo CDSS");

        using var conn = (IDbConnection)_db.CreateConnection();
        var tenantId = _currentUser.TenantId!.Value;
        var userId = _currentUser.UserId;

        var id = Guid.NewGuid();
        await conn.ExecuteAsync(
            @"INSERT INTO diab_his_cdss_alert_override_log
                (id, tenant_id, prescription_id, encounter_id, rule_type, rule_code, severity,
                 override_reason, reason_code, overridden_by, signed_at)
              VALUES
                (@id, @tenantId, @presId, @encId, @ruleType, @ruleCode, @severity,
                 @reason, @reasonCode, @overriddenBy, NOW())",
            new
            {
                id = id.ToString(),
                tenantId,
                presId = req.PrescriptionId?.ToString(),
                encId = req.EncounterId?.ToString(),
                ruleType = req.RuleType,
                ruleCode = req.RuleCode,
                severity = req.Severity,
                reason = req.OverrideReason,
                reasonCode = req.ReasonCode,
                overriddenBy = userId?.ToString()
            });

        return Result<Guid>.Success(id);
    }
}

public class ListCdssRulesQueryHandler : IRequestHandler<ListCdssRulesQuery, Result<IReadOnlyList<CdssRuleResponse>>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;

    public ListCdssRulesQueryHandler(IDapperConnectionFactory db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<CdssRuleResponse>>> Handle(ListCdssRulesQuery q, CancellationToken ct)
    {
        using var conn = (IDbConnection)_db.CreateConnection();
        var tenantId = _currentUser.TenantId!.Value;

        var rows = await conn.QueryAsync<CdssRuleRow>(
            @"SELECT id, tenant_id AS TenantId, code, rule_name AS RuleName, rule_type AS RuleType,
                     category, definition_json AS DefinitionJson, message_vi AS MessageVi,
                     management_vi AS ManagementVi, severity, is_interruptive AS IsInterruptive,
                     priority, is_active AS IsActive
              FROM diab_his_cdss_rules
              WHERE (tenant_id = @tenantId OR tenant_id IS NULL) AND deleted_at IS NULL
              ORDER BY priority, rule_name",
            new { tenantId });

        var result = rows.Select(r => new CdssRuleResponse(
            Guid.Parse(r.Id), r.TenantId, r.Code, r.RuleName, r.RuleType, r.Category,
            r.DefinitionJson, r.MessageVi, r.ManagementVi, r.Severity, r.IsInterruptive, r.Priority, r.IsActive))
            .ToList();

        return Result<IReadOnlyList<CdssRuleResponse>>.Success(result.AsReadOnly());
    }

    private class CdssRuleRow
    {
        public string Id { get; set; } = "";
        public int? TenantId { get; set; }
        public string Code { get; set; } = "";
        public string RuleName { get; set; } = "";
        public string RuleType { get; set; } = "";
        public string? Category { get; set; }
        public string? DefinitionJson { get; set; }
        public string? MessageVi { get; set; }
        public string? ManagementVi { get; set; }
        public string Severity { get; set; } = "";
        public bool IsInterruptive { get; set; }
        public int Priority { get; set; }
        public bool IsActive { get; set; }
    }
}

public class UpsertCdssRuleCommandHandler : IRequestHandler<UpsertCdssRuleCommand, Result<Guid>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;

    public UpsertCdssRuleCommandHandler(IDapperConnectionFactory db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<Guid>> Handle(UpsertCdssRuleCommand cmd, CancellationToken ct)
    {
        using var conn = (IDbConnection)_db.CreateConnection();
        var tenantId = _currentUser.TenantId!.Value;
        var req = cmd.Req;

        if (req.Id.HasValue)
        {
            await conn.ExecuteAsync(
                @"UPDATE diab_his_cdss_rules SET
                    rule_name = @ruleName, rule_type = @ruleType, category = @category,
                    definition_json = @definitionJson, message_vi = @messageVi, management_vi = @managementVi,
                    severity = @severity, is_interruptive = @isInterruptive, priority = @priority,
                    is_active = @isActive, updated_at = NOW()
                  WHERE id = @id AND tenant_id = @tenantId",
                new
                {
                    id = req.Id.Value.ToString(), tenantId,
                    ruleName = req.RuleName, ruleType = req.RuleType, category = req.Category,
                    definitionJson = req.DefinitionJson, messageVi = req.MessageVi, managementVi = req.ManagementVi,
                    severity = req.Severity, isInterruptive = req.IsInterruptive, priority = req.Priority,
                    isActive = req.IsActive
                });
            return Result<Guid>.Success(req.Id.Value);
        }

        var newId = Guid.NewGuid();
        await conn.ExecuteAsync(
            @"INSERT INTO diab_his_cdss_rules
                (id, tenant_id, code, rule_name, rule_type, category, definition_json,
                 message_vi, management_vi, severity, is_interruptive, priority, is_active)
              VALUES
                (@id, @tenantId, @code, @ruleName, @ruleType, @category, @definitionJson,
                 @messageVi, @managementVi, @severity, @isInterruptive, @priority, @isActive)",
            new
            {
                id = newId.ToString(), tenantId, code = req.Code,
                ruleName = req.RuleName, ruleType = req.RuleType, category = req.Category,
                definitionJson = req.DefinitionJson, messageVi = req.MessageVi, managementVi = req.ManagementVi,
                severity = req.Severity, isInterruptive = req.IsInterruptive, priority = req.Priority,
                isActive = req.IsActive
            });

        return Result<Guid>.Success(newId);
    }
}
