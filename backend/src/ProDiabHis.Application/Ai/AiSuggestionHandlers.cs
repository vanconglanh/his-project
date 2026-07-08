using System.Data;
using System.Text.Json;
using Dapper;
using MediatR;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.Ai;

// ─── Commands ───────────────────────────────────────────────────────────────
public record GenerateTreatmentSuggestionCommand(Guid PatientId, Guid? EncounterId)
    : IRequest<Result<TreatmentSuggestionResponse>>;

public record UpdateAiSuggestionStatusCommand(Guid LogId, string Status) : IRequest<Result<bool>>;

// ─── Handlers ───────────────────────────────────────────────────────────────
public class GenerateTreatmentSuggestionCommandHandler
    : IRequestHandler<GenerateTreatmentSuggestionCommand, Result<TreatmentSuggestionResponse>>
{
    private static readonly string[] ValidStatuses = ["ACCEPTED", "REJECTED", "EDITED"];

    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;
    private readonly ITreatmentSuggestionService _suggestionService;

    public GenerateTreatmentSuggestionCommandHandler(
        IDapperConnectionFactory db, ICurrentUser currentUser, ITreatmentSuggestionService suggestionService)
    {
        _db = db;
        _currentUser = currentUser;
        _suggestionService = suggestionService;
    }

    public async Task<Result<TreatmentSuggestionResponse>> Handle(GenerateTreatmentSuggestionCommand cmd, CancellationToken ct)
    {
        using var conn = (IDbConnection)_db.CreateConnection();
        var tenantId = _currentUser.TenantId!.Value;
        var patientId = cmd.PatientId.ToString();

        var assessment = await conn.QueryFirstOrDefaultAsync<(decimal? Hba1c, decimal? Egfr, int? BpSys, int? BpDia)?>(
            @"SELECT hba1c AS Hba1c, egfr AS Egfr, bp_systolic AS BpSys, bp_diastolic AS BpDia
              FROM diab_his_cli_diabetes_assessments
              WHERE tenant_id = @tenantId AND patient_id = @patientId AND deleted_at IS NULL
              ORDER BY assessed_at DESC LIMIT 1",
            new { tenantId, patientId });

        var onMetformin = await conn.ExecuteScalarAsync<int>(
            @"SELECT COUNT(*)
              FROM diab_his_pha_prescriptions p
              JOIN diab_his_pha_prescription_items pi ON pi.prescription_id = p.id AND pi.tenant_id = p.tenant_id
              JOIN diab_his_pha_drugs d ON d.id = pi.drug_id
              WHERE p.tenant_id = @tenantId AND p.patient_id = @patientId AND p.deleted_at IS NULL
                AND p.status <> 'CANCELLED' AND pi.deleted_at IS NULL
                AND LOWER(d.generic_name) LIKE '%metformin%'",
            new { tenantId, patientId }) > 0;

        var targetRows = await conn.QueryAsync<(string Param, decimal TargetValue, int? TenantId)>(
            @"SELECT param AS Param, target_value AS TargetValue, tenant_id AS TenantId
              FROM diab_his_cli_care_pathway_target
              WHERE code = 'DM_T2_5481' AND (tenant_id = @tenantId OR tenant_id IS NULL)
              ORDER BY (tenant_id IS NULL) ASC", new { tenantId });

        var targetDict = new Dictionary<string, decimal>();
        foreach (var r in targetRows)
        {
            if (!targetDict.ContainsKey(r.Param)) targetDict[r.Param] = r.TargetValue;
        }

        var state = new PatientClinicalState(
            assessment?.Hba1c, assessment?.Egfr, assessment?.BpSys, assessment?.BpDia, onMetformin);

        var targets = new CarePathwayTargetSnapshot(
            targetDict.TryGetValue("HBA1C", out var hba1cT) ? hba1cT : null,
            targetDict.TryGetValue("BP_SYS", out var bpSysT) ? (int)bpSysT : null,
            targetDict.TryGetValue("BP_DIA", out var bpDiaT) ? (int)bpDiaT : null,
            targetDict.TryGetValue("EGFR", out var egfrT) ? egfrT : null);

        var ruleDerived = GuidelineTreatmentReasoner.Reason(state, targets);

        var suggestionCtx = new TreatmentSuggestionContext(cmd.PatientId, cmd.EncounterId, state, targets, ruleDerived);
        var suggestion = await _suggestionService.SuggestAsync(suggestionCtx, ct);

        var logId = Guid.NewGuid();
        var userId = _currentUser.UserId;

        await conn.ExecuteAsync(
            @"INSERT INTO diab_his_cli_ai_suggestion_log
                (id, tenant_id, patient_id, encounter_id, context_json, rule_derived_json,
                 llm_output_text, fallback_used, disclaimer_version, status, created_at, created_by)
              VALUES
                (@id, @tenantId, @patientId, @encounterId, @contextJson, @ruleDerivedJson,
                 @bodyText, @fallbackUsed, 'v1', 'SHOWN', NOW(3), @createdBy)",
            new
            {
                id = logId.ToString(), tenantId, patientId, encounterId = cmd.EncounterId?.ToString(),
                contextJson = JsonSerializer.Serialize(state),
                ruleDerivedJson = JsonSerializer.Serialize(ruleDerived),
                bodyText = suggestion.BodyText,
                fallbackUsed = suggestion.FallbackUsed,
                createdBy = userId?.ToString()
            });

        return Result<TreatmentSuggestionResponse>.Success(new TreatmentSuggestionResponse(
            logId, suggestion.DisclaimerText, suggestion.BodyText, suggestion.FallbackUsed, ruleDerived));
    }
}

public class UpdateAiSuggestionStatusCommandHandler : IRequestHandler<UpdateAiSuggestionStatusCommand, Result<bool>>
{
    private static readonly string[] ValidStatuses = ["ACCEPTED", "REJECTED", "EDITED"];

    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;

    public UpdateAiSuggestionStatusCommandHandler(IDapperConnectionFactory db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<bool>> Handle(UpdateAiSuggestionStatusCommand cmd, CancellationToken ct)
    {
        if (!ValidStatuses.Contains(cmd.Status))
            return Result<bool>.Failure("AI_SUGGESTION_INVALID_STATUS", "Trạng thái không hợp lệ");

        using var conn = (IDbConnection)_db.CreateConnection();
        var tenantId = _currentUser.TenantId!.Value;
        var userId = _currentUser.UserId;

        var affected = await conn.ExecuteAsync(
            @"UPDATE diab_his_cli_ai_suggestion_log SET
                status = @status, reviewed_by = @reviewedBy, reviewed_at = NOW(3)
              WHERE id = @id AND tenant_id = @tenantId",
            new { id = cmd.LogId.ToString(), tenantId, status = cmd.Status, reviewedBy = userId?.ToString() });

        if (affected == 0) return Result<bool>.Failure("AI_SUGGESTION_NOT_FOUND", "Không tìm thấy gợi ý AI");
        return Result<bool>.Success(true);
    }
}
