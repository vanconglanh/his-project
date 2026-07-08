using System.Data;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Dapper;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using ProDiabHis.Application.Cdss;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Infrastructure.Cdss;

/// <summary>
/// Trien khai CDSS engine: DDI thuoc-thuoc, thuoc-di ung, trung hoat chat,
/// drug-lab, critical-lab. Rule/danh muc dung chung (tenant NULL) + rieng tenant
/// duoc cache in-memory (TTL 10 phut) de giam tai truy van lap lai.
/// </summary>
public class CdssEngineImpl : ICdssEngine
{
    private readonly IDapperConnectionFactory _db;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CdssEngineImpl> _logger;

    private static readonly string[] SeverityOrder = ["CONTRAINDICATED", "MAJOR", "MODERATE", "MINOR"];

    public CdssEngineImpl(IDapperConnectionFactory db, IMemoryCache cache, ILogger<CdssEngineImpl> logger)
    {
        _db = db;
        _cache = cache;
        _logger = logger;
    }

    public async Task<CdssCheckResponse> EvaluateAsync(
        CdssEvaluationContext ctx, string context, bool logEvents, CancellationToken ct = default)
    {
        using var conn = (IDbConnection)_db.CreateConnection();

        var alerts = new List<CdssAlert>();

        // Chuan hoa danh sach thuoc (bo qua item khong co ingredient)
        var normDrugs = ctx.Drugs
            .Select(d => (Original: d, Norm: Normalize(d.Ingredient)))
            .Where(x => !string.IsNullOrEmpty(x.Norm))
            .ToList();

        // 1) DDI thuoc-thuoc
        var ddiPairs = await GetDdiPairsAsync(conn, ctx.TenantId, ct);
        var seenPairs = new HashSet<string>();
        for (int i = 0; i < normDrugs.Count; i++)
        {
            for (int j = i + 1; j < normDrugs.Count; j++)
            {
                var a = normDrugs[i].Norm!;
                var b = normDrugs[j].Norm!;
                if (a == b) continue; // trung hoat chat xu ly rieng ben duoi
                var (lo, hi) = string.CompareOrdinal(a, b) <= 0 ? (a, b) : (b, a);
                var key = $"{lo}|{hi}";
                if (!seenPairs.Add(key)) continue;

                if (ddiPairs.TryGetValue(key, out var pair))
                {
                    alerts.Add(new CdssAlert(
                        "DRUG_DRUG", null, pair.Severity,
                        IsInterruptiveSeverity(pair.Severity),
                        $"Tương tác thuốc: {lo} + {hi}",
                        pair.Mechanism ?? "Có tương tác thuốc-thuốc cần lưu ý.",
                        pair.Management,
                        [normDrugs[i].Original.DrugId, normDrugs[j].Original.DrugId]));
                }
            }
        }

        // 2) Trung hoat chat (duplicate ingredient)
        foreach (var group in normDrugs.GroupBy(x => x.Norm))
        {
            if (group.Count() > 1)
            {
                alerts.Add(new CdssAlert(
                    "DUPLICATE_INGREDIENT", null, "MODERATE", false,
                    $"Trùng hoạt chất: {group.Key}",
                    $"Đơn thuốc có {group.Count()} mục cùng hoạt chất '{group.Key}'. Cân nhắc loại bỏ trùng lặp.",
                    "Rà soát đơn, loại bỏ mục trùng hoặc gộp liều.",
                    group.Select(g => g.Original.DrugId).ToList()));
            }
        }

        // 3) Thuoc - di ung
        if (ctx.PatientId.HasValue)
        {
            var allergies = await conn.QueryAsync<AllergyRow>(
                @"SELECT allergen_ingredient AS AllergenIngredient, atc_code AS AtcCode, allergen_name AS AllergenName
                  FROM diab_his_cli_allergies
                  WHERE tenant_id = @tenantId AND patient_id = @patientId AND is_active = 1 AND deleted_at IS NULL",
                new { tenantId = ctx.TenantId, patientId = ctx.PatientId.Value.ToString() });

            var allergyList = allergies.ToList();
            foreach (var (drug, norm) in normDrugs)
            {
                foreach (var allergy in allergyList)
                {
                    var allergyNorm = Normalize(allergy.AllergenIngredient);
                    var atcMatch = !string.IsNullOrWhiteSpace(allergy.AtcCode) && !string.IsNullOrWhiteSpace(drug.AtcCode)
                        && string.Equals(allergy.AtcCode, drug.AtcCode, StringComparison.OrdinalIgnoreCase);
                    var ingredientMatch = !string.IsNullOrEmpty(allergyNorm) && allergyNorm == norm;

                    if (atcMatch || ingredientMatch)
                    {
                        alerts.Add(new CdssAlert(
                            "DRUG_ALLERGY", null, "MAJOR", true,
                            $"Dị ứng thuốc: {allergy.AllergenName}",
                            $"Bệnh nhân có tiền sử dị ứng '{allergy.AllergenName}', trùng với thuốc kê ('{norm}').",
                            "Ngừng kê thuốc gây dị ứng hoặc thay thế nhóm khác; xác nhận lại tiền sử với bệnh nhân.",
                            [drug.DrugId]));
                    }
                }
            }
        }

        // 4) DRUG_LAB / CRITICAL_LAB
        if (ctx.PatientId.HasValue)
        {
            var labRules = await GetLabRulesAsync(conn, ctx.TenantId, ct);
            if (labRules.Count > 0)
            {
                var labCodes = labRules.Select(r => r.LabCode).Distinct().ToList();
                var latestLabs = await GetLatestLabValuesAsync(conn, ctx.TenantId, ctx.PatientId.Value, labCodes, ct);

                foreach (var rule in labRules)
                {
                    if (!latestLabs.TryGetValue(rule.LabCode, out var labValue)) continue;

                    if (rule.RuleType == "DRUG_LAB")
                    {
                        if (string.IsNullOrEmpty(rule.Ingredient)) continue;
                        var match = normDrugs.FirstOrDefault(d => d.Norm == Normalize(rule.Ingredient));
                        if (match.Original is null) continue;
                        if (EvaluateOp(labValue, rule.Op, rule.Threshold))
                        {
                            alerts.Add(new CdssAlert(
                                "DRUG_LAB", rule.Code, rule.Severity, rule.IsInterruptive || IsInterruptiveSeverity(rule.Severity),
                                rule.RuleName, rule.MessageVi ?? rule.RuleName, rule.ManagementVi,
                                [match.Original.DrugId]));
                        }
                    }
                    else if (rule.RuleType == "CRITICAL_LAB")
                    {
                        if (EvaluateOp(labValue, rule.Op, rule.Threshold))
                        {
                            alerts.Add(new CdssAlert(
                                "CRITICAL_LAB", rule.Code, rule.Severity, rule.IsInterruptive || IsInterruptiveSeverity(rule.Severity),
                                rule.RuleName, rule.MessageVi ?? rule.RuleName, rule.ManagementVi,
                                []));
                        }
                    }
                }
            }
        }

        // Sort theo do nghiem trong
        var sorted = alerts
            .OrderBy(a => Array.IndexOf(SeverityOrder, a.Severity) is var idx && idx >= 0 ? idx : SeverityOrder.Length)
            .ToList();

        var hasInterruptive = sorted.Any(a => a.IsInterruptive);

        if (logEvents && sorted.Count > 0)
        {
            foreach (var alert in sorted)
            {
                try
                {
                    await conn.ExecuteAsync(
                        @"INSERT INTO diab_his_cdss_alert_events
                            (id, tenant_id, patient_id, encounter_id, prescription_id, rule_type, rule_code,
                             severity, is_interruptive, title, detail, payload_json, context, fired_at)
                          VALUES
                            (UUID(), @tenantId, @patientId, @encounterId, @prescriptionId, @ruleType, @ruleCode,
                             @severity, @isInterruptive, @title, @detail, @payload, @context, NOW(3))",
                        new
                        {
                            tenantId = ctx.TenantId,
                            patientId = ctx.PatientId?.ToString(),
                            encounterId = ctx.EncounterId?.ToString(),
                            prescriptionId = ctx.PrescriptionId?.ToString(),
                            ruleType = alert.RuleType,
                            ruleCode = alert.RuleCode,
                            severity = alert.Severity,
                            isInterruptive = alert.IsInterruptive,
                            title = alert.Title,
                            detail = alert.Detail,
                            payload = JsonSerializer.Serialize(alert.DrugRefs),
                            context
                        });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Loi ghi cdss alert event (khong chan luong danh gia)");
                }
            }
        }

        return new CdssCheckResponse(sorted.AsReadOnly(), hasInterruptive);
    }

    private static bool IsInterruptiveSeverity(string severity) =>
        severity is "CONTRAINDICATED" or "MAJOR";

    private static bool EvaluateOp(decimal value, string op, decimal threshold) => op switch
    {
        "<" => value < threshold,
        "<=" => value <= threshold,
        ">" => value > threshold,
        ">=" => value >= threshold,
        "=" => value == threshold,
        _ => false
    };

    /// <summary>Chuan hoa hoat chat: lowercase, bo dau tieng Viet, trim.</summary>
    public static string? Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;
        var s = input.Trim().ToLowerInvariant();
        var normalized = s.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in normalized)
        {
            var uc = CharUnicodeInfo.GetUnicodeCategory(c);
            if (uc != UnicodeCategory.NonSpacingMark) sb.Append(c);
        }
        var result = sb.ToString().Normalize(NormalizationForm.FormC);
        result = result.Replace('đ', 'd').Replace('Đ', 'D');
        return result.Trim();
    }

    private async Task<Dictionary<string, DdiPairInfo>> GetDdiPairsAsync(IDbConnection conn, int tenantId, CancellationToken ct)
    {
        var cacheKey = $"cdss_ddi_pairs_{tenantId}";
        if (_cache.TryGetValue(cacheKey, out Dictionary<string, DdiPairInfo>? cached) && cached is not null)
            return cached;

        var rows = await conn.QueryAsync<DdiPairRow>(
            @"SELECT ingredient_a AS IngredientA, ingredient_b AS IngredientB, severity, mechanism, management
              FROM diab_his_cdss_ddi_pairs
              WHERE (tenant_id = @tenantId OR tenant_id IS NULL) AND is_active = 1 AND deleted_at IS NULL",
            new { tenantId });

        var dict = new Dictionary<string, DdiPairInfo>();
        foreach (var r in rows)
        {
            var a = Normalize(r.IngredientA) ?? r.IngredientA;
            var b = Normalize(r.IngredientB) ?? r.IngredientB;
            var (lo, hi) = string.CompareOrdinal(a, b) <= 0 ? (a, b) : (b, a);
            dict[$"{lo}|{hi}"] = new DdiPairInfo(r.Severity, r.Mechanism, r.Management);
        }

        _cache.Set(cacheKey, dict, TimeSpan.FromMinutes(10));
        return dict;
    }

    private async Task<List<LabRuleInfo>> GetLabRulesAsync(IDbConnection conn, int tenantId, CancellationToken ct)
    {
        var cacheKey = $"cdss_lab_rules_{tenantId}";
        if (_cache.TryGetValue(cacheKey, out List<LabRuleInfo>? cached) && cached is not null)
            return cached;

        var rows = await conn.QueryAsync<CdssRuleRawRow>(
            @"SELECT code, rule_name AS RuleName, rule_type AS RuleType, definition_json AS DefinitionJson,
                     message_vi AS MessageVi, management_vi AS ManagementVi, severity, is_interruptive AS IsInterruptive
              FROM diab_his_cdss_rules
              WHERE (tenant_id = @tenantId OR tenant_id IS NULL)
                AND rule_type IN ('DRUG_LAB','CRITICAL_LAB') AND is_active = 1 AND deleted_at IS NULL",
            new { tenantId });

        var result = new List<LabRuleInfo>();
        foreach (var r in rows)
        {
            if (string.IsNullOrWhiteSpace(r.DefinitionJson)) continue;
            try
            {
                using var doc = JsonDocument.Parse(r.DefinitionJson);
                var root = doc.RootElement;
                var labCode = root.TryGetProperty("lab_code", out var lc) ? lc.GetString() : null;
                var op = root.TryGetProperty("op", out var opEl) ? opEl.GetString() : null;
                var threshold = root.TryGetProperty("threshold", out var th) ? th.GetDecimal() : (decimal?)null;
                var ingredient = root.TryGetProperty("ingredient", out var ing) ? ing.GetString() : null;

                if (string.IsNullOrEmpty(labCode) || string.IsNullOrEmpty(op) || threshold is null) continue;

                result.Add(new LabRuleInfo(r.Code, r.RuleName, r.RuleType, labCode, op, threshold.Value,
                    ingredient, r.MessageVi, r.ManagementVi, r.Severity, r.IsInterruptive));
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Loi parse definition_json rule CDSS {Code}", r.Code);
            }
        }

        _cache.Set(cacheKey, result, TimeSpan.FromMinutes(10));
        return result;
    }

    private static async Task<Dictionary<string, decimal>> GetLatestLabValuesAsync(
        IDbConnection conn, int tenantId, Guid patientId, IReadOnlyList<string> labCodes, CancellationToken ct)
    {
        if (labCodes.Count == 0) return new();

        var rows = await conn.QueryAsync<(string TestCode, decimal? ValueNumeric)>(
            @"SELECT lr.test_code AS TestCode, lr.value_numeric AS ValueNumeric
              FROM diab_his_lab_results lr
              INNER JOIN (
                  SELECT test_code, MAX(performed_at) AS max_at
                  FROM diab_his_lab_results
                  WHERE tenant_id = @tenantId AND patient_id = @patientId
                    AND test_code IN @labCodes AND deleted_at IS NULL AND value_numeric IS NOT NULL
                  GROUP BY test_code
              ) latest ON latest.test_code = lr.test_code AND latest.max_at = lr.performed_at
              WHERE lr.tenant_id = @tenantId AND lr.patient_id = @patientId AND lr.deleted_at IS NULL",
            new { tenantId, patientId = patientId.ToString(), labCodes });

        var dict = new Dictionary<string, decimal>();
        foreach (var r in rows)
        {
            if (r.ValueNumeric.HasValue) dict[r.TestCode] = r.ValueNumeric.Value;
        }
        return dict;
    }

    private record DdiPairInfo(string Severity, string? Mechanism, string? Management);

    private class DdiPairRow
    {
        public string IngredientA { get; set; } = "";
        public string IngredientB { get; set; } = "";
        public string Severity { get; set; } = "";
        public string? Mechanism { get; set; }
        public string? Management { get; set; }
    }

    private class AllergyRow
    {
        public string? AllergenIngredient { get; set; }
        public string? AtcCode { get; set; }
        public string AllergenName { get; set; } = "";
    }

    private class CdssRuleRawRow
    {
        public string Code { get; set; } = "";
        public string RuleName { get; set; } = "";
        public string RuleType { get; set; } = "";
        public string? DefinitionJson { get; set; }
        public string? MessageVi { get; set; }
        public string? ManagementVi { get; set; }
        public string Severity { get; set; } = "";
        public bool IsInterruptive { get; set; }
    }

    private record LabRuleInfo(
        string Code, string RuleName, string RuleType, string LabCode, string Op, decimal Threshold,
        string? Ingredient, string? MessageVi, string? ManagementVi, string Severity, bool IsInterruptive);
}
