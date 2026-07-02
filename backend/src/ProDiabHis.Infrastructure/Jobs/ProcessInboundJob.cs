using System.Text.Json;
using Dapper;
using Microsoft.Extensions.Logging;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.LabResults;

namespace ProDiabHis.Infrastructure.Jobs;

/// <summary>
/// Hangfire job: parse payload inbound -> tao lab_results.
/// Idempotent: neu da PROCESSED thi skip.
/// </summary>
public class ProcessInboundJob
{
    private readonly IDapperConnectionFactory _db;
    private readonly ILabResultFlagCalculator _flagCalc;
    private readonly IHl7v25Parser _hl7Parser;
    private readonly ILogger<ProcessInboundJob> _logger;

    public ProcessInboundJob(IDapperConnectionFactory db, ILabResultFlagCalculator flagCalc,
        IHl7v25Parser hl7Parser, ILogger<ProcessInboundJob> logger)
    { _db = db; _flagCalc = flagCalc; _hl7Parser = hl7Parser; _logger = logger; }

    public async Task Execute(string inboundId)
    {
        using var conn = _db.CreateConnection();
        var inbound = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT * FROM cli_lab_inbound WHERE id=@Id", new { Id = inboundId });

        if (inbound is null)
        {
            _logger.LogWarning("ProcessInboundJob: inbound {Id} not found", inboundId);
            return;
        }
        if ((string)inbound.status == "PROCESSED")
        {
            _logger.LogInformation("ProcessInboundJob: inbound {Id} already PROCESSED, skip", inboundId);
            return;
        }

        var now = DateTime.UtcNow;
        int processedCount = 0;

        try
        {
            List<(string TestCode, string Value, decimal? ValueNum, string? Unit,
                  decimal? RefLow, decimal? RefHigh, DateTime PerformedAt)> items = new();

            // Parse JSON payload
            if ((string?)inbound.payload_json is { } payloadStr && !string.IsNullOrEmpty(payloadStr))
            {
                var root = JsonSerializer.Deserialize<JsonElement>(payloadStr);
                if (root.TryGetProperty("results", out var results))
                {
                    foreach (var r in results.EnumerateArray())
                    {
                        var testCode    = r.TryGetProperty("test_code", out var tc) ? tc.GetString() ?? "" : "";
                        var value       = r.TryGetProperty("value", out var v) ? v.GetString() ?? "" : "";
                        decimal? valNum = r.TryGetProperty("value_numeric", out var vn) && vn.ValueKind != JsonValueKind.Null
                            ? vn.GetDecimal() : null;
                        var unit        = r.TryGetProperty("unit", out var u) ? u.GetString() : null;
                        decimal? refLow = r.TryGetProperty("reference_range_low", out var rl) && rl.ValueKind != JsonValueKind.Null
                            ? rl.GetDecimal() : null;
                        decimal? refHigh = r.TryGetProperty("reference_range_high", out var rh) && rh.ValueKind != JsonValueKind.Null
                            ? rh.GetDecimal() : null;
                        var performedAt = r.TryGetProperty("performed_at", out var pa)
                            ? pa.GetDateTime() : now;

                        items.Add((testCode, value, valNum, unit, refLow, refHigh, performedAt));
                    }
                }
            }
            // Parse HL7 nếu có
            else if ((string?)inbound.raw_hl7_message is { } hl7 && !string.IsNullOrEmpty(hl7))
            {
                var parsed = _hl7Parser.Parse(hl7);
                items.AddRange(parsed.Select(r => (r.TestCode, r.Value, r.ValueNumeric, r.Unit,
                    (decimal?)null, (decimal?)null, r.PerformedAt)));
            }

            // Lookup outbound de lay lab_order_id
            string? labOrderId = null;
            if ((string?)inbound.outbound_id is { } outboundIdStr)
            {
                var outbound = await conn.QueryFirstOrDefaultAsync<dynamic>(
                    "SELECT lab_order_id FROM cli_lab_outbound WHERE id=@Id", new { Id = outboundIdStr });
                labOrderId = (string?)outbound?.lab_order_id;
            }

            foreach (var (testCode, value, valNum, unit, refLow, refHigh, performedAt) in items)
            {
                if (string.IsNullOrWhiteSpace(testCode)) continue;

                var dict = await conn.QueryFirstOrDefaultAsync<dynamic>(
                    "SELECT name, reference_range_low, reference_range_high, unit AS dict_unit FROM diab_his_dict_lab_tests WHERE code=@Code",
                    new { Code = testCode });

                var actualRefLow  = refLow  ?? (decimal?)dict?.reference_range_low;
                var actualRefHigh = refHigh ?? (decimal?)dict?.reference_range_high;
                var flag          = _flagCalc.Calculate(valNum, actualRefLow, actualRefHigh);

                // Lay patient + encounter tu lab order neu co
                string? patientId   = null;
                string? encounterId = null;
                if (labOrderId is not null)
                {
                    var orderInfo = await conn.QueryFirstOrDefaultAsync<dynamic>(
                        @"SELECT lo.encounter_id, v.patient_id FROM diab_his_cli_lab_orders lo
                          JOIN cli_visits v ON v.id = lo.encounter_id
                          WHERE lo.id = @Id", new { Id = labOrderId });
                    patientId   = (string?)orderInfo?.patient_id;
                    encounterId = (string?)orderInfo?.encounter_id;
                }

                var resultId = Guid.NewGuid().ToString();
                await conn.ExecuteAsync(@"
                    INSERT INTO cli_lab_results
                        (id, tenant_id, lab_order_id, lab_order_item_id, patient_id, encounter_id,
                         test_code, test_name, value, value_numeric, unit,
                         reference_range_low, reference_range_high,
                         flag, performed_at, status, source,
                         created_at, updated_at)
                    VALUES
                        (@Id, @TId, @OId, @OId, @PatId, @EId,
                         @Code, @Name, @Val, @ValNum, @Unit,
                         @RefLow, @RefHigh,
                         @Flag, @PerAt, 'DRAFT', 'PARTNER',
                         @Now, @Now)",
                    new
                    {
                        Id = resultId, TId = (int)inbound.tenant_id,
                        OId = labOrderId ?? resultId, PatId = patientId ?? resultId,
                        EId = encounterId ?? resultId,
                        Code = testCode, Name = (string?)dict?.name ?? testCode,
                        Val = value, ValNum = valNum, Unit = unit ?? (string?)dict?.dict_unit,
                        RefLow = actualRefLow, RefHigh = actualRefHigh,
                        Flag = flag, PerAt = performedAt, Now = now
                    });

                processedCount++;
            }

            await conn.ExecuteAsync(@"
                UPDATE cli_lab_inbound SET
                    status='PROCESSED', processed_at=@Now,
                    processed_result_count=@Count, updated_at=@Now
                WHERE id=@Id",
                new { Now = now, Count = processedCount, Id = inboundId });

            _logger.LogInformation("ProcessInboundJob: inbound {Id} processed {Count} results", inboundId, processedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ProcessInboundJob: inbound {Id} failed", inboundId);
            await conn.ExecuteAsync(
                "UPDATE cli_lab_inbound SET status='FAILED', error_message=@Err, updated_at=@Now WHERE id=@Id",
                new { Err = ex.Message, Now = now, Id = inboundId });
        }
    }
}
