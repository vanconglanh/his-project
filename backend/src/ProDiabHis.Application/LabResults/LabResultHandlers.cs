using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProDiabHis.Application.Auth;
using ProDiabHis.Application.Common;
using ProDiabHis.Domain.Entities;

namespace ProDiabHis.Application.LabResults;

// ═══════════════════════════════════════════════
// COMMANDS / QUERIES
// ═══════════════════════════════════════════════

public record ListLabResultsQuery(
    Guid? PatientId, Guid? EncounterId, Guid? LabOrderId,
    string? Status, string? Flag,
    DateTime? FromDate, DateTime? ToDate,
    int Page, int PageSize)
    : IRequest<Result<(IReadOnlyList<LabResultResponse> Items, int Total)>>;

public record CreateLabResultCommand(LabResultCreateRequest Req)
    : IRequest<Result<LabResultResponse>>;

public record UpdateLabResultCommand(Guid Id, LabResultUpdateRequest Req)
    : IRequest<Result<LabResultResponse>>;

public record VerifyLabResultCommand(Guid Id)
    : IRequest<Result<bool>>;

public record UnverifyLabResultCommand(Guid Id)
    : IRequest<Result<bool>>;

public record ImportLabResultsCommand(Stream FileStream, string Format, bool AutoVerify)
    : IRequest<Result<ImportLabResultsResponse>>;

public record GetAbnormalLabResultsQuery(string Severity, DateTime? FromDate, DateTime? ToDate)
    : IRequest<Result<IReadOnlyList<LabResultResponse>>>;

public record GetLabResultHistoryTrendQuery(Guid PatientId, string TestCode, DateTime? FromDate, DateTime? ToDate)
    : IRequest<Result<LabResultTrendResponse>>;

public record ExportLabResultPdfQuery(Guid Id)
    : IRequest<Result<byte[]>>;

public record BatchVerifyLabResultsCommand(IReadOnlyList<Guid> ResultIds)
    : IRequest<Result<BatchVerifyResponse>>;

// ═══════════════════════════════════════════════
// HELPERS
// ═══════════════════════════════════════════════
file static class Mapper
{
    public static LabResultResponse Map(LabResult e) => new(
        e.Id,
        Guid.TryParse(e.LabOrderId, out var oid) ? oid : Guid.Empty,
        Guid.TryParse(e.LabOrderItemId, out var oiid) ? oiid : e.Id,
        Guid.TryParse(e.PatientId, out var pid) ? pid : Guid.Empty,
        Guid.TryParse(e.EncounterId, out var eid) ? eid : Guid.Empty,
        e.TestCode,
        e.TestName,
        e.Value,
        e.ValueNumeric,
        e.Unit,
        e.ReferenceRangeLow,
        e.ReferenceRangeHigh,
        e.Flag,
        e.Method,
        e.PerformedAt,
        Guid.TryParse(e.PerformedBy, out var pbid) ? pbid : (Guid?)null,
        e.Status,
        e.VerifiedAt,
        Guid.TryParse(e.VerifiedBy, out var vbid) ? vbid : (Guid?)null,
        e.Note,
        e.Source,
        e.CreatedAt,
        e.UpdatedAt);
}

// ═══════════════════════════════════════════════
// LIST
// ═══════════════════════════════════════════════
public class ListLabResultsQueryHandler
    : IRequestHandler<ListLabResultsQuery, Result<(IReadOnlyList<LabResultResponse>, int)>>
{
    private readonly IApplicationDbContext _db;

    public ListLabResultsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<(IReadOnlyList<LabResultResponse>, int)>> Handle(
        ListLabResultsQuery q, CancellationToken ct)
    {
        var query = _db.LabResults.AsQueryable();

        if (q.PatientId.HasValue)   query = query.Where(e => e.PatientId   == q.PatientId.Value.ToString());
        if (q.EncounterId.HasValue) query = query.Where(e => e.EncounterId == q.EncounterId.Value.ToString());
        if (q.LabOrderId.HasValue)  query = query.Where(e => e.LabOrderId  == q.LabOrderId.Value.ToString());
        if (!string.IsNullOrEmpty(q.Status)) query = query.Where(e => e.Status == q.Status);
        if (!string.IsNullOrEmpty(q.Flag))   query = query.Where(e => e.Flag   == q.Flag);
        if (q.FromDate.HasValue) query = query.Where(e => e.PerformedAt >= q.FromDate.Value);
        if (q.ToDate.HasValue)   query = query.Where(e => e.PerformedAt <= q.ToDate.Value.AddDays(1));

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(e => e.PerformedAt)
            .Skip((q.Page - 1) * q.PageSize)
            .Take(q.PageSize)
            .Select(e => Mapper.Map(e))
            .ToListAsync(ct);

        return Result<(IReadOnlyList<LabResultResponse>, int)>.Success((items, total));
    }
}

// ═══════════════════════════════════════════════
// CREATE
// ═══════════════════════════════════════════════
public class CreateLabResultCommandHandler
    : IRequestHandler<CreateLabResultCommand, Result<LabResultResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _user;
    private readonly IAuditService _audit;
    private readonly ILabResultFlagCalculator _flagCalc;

    public CreateLabResultCommandHandler(IApplicationDbContext db, ITenantProvider tenant,
        ICurrentUser user, IAuditService audit, ILabResultFlagCalculator flagCalc)
    { _db = db; _tenant = tenant; _user = user; _audit = audit; _flagCalc = flagCalc; }

    public async Task<Result<LabResultResponse>> Handle(CreateLabResultCommand cmd, CancellationToken ct)
    {
        var req = cmd.Req;

        // Lookup lab order
        var labOrder = await _db.LabOrders
            .FirstOrDefaultAsync(o => o.Id == req.LabOrderItemId, ct);

        if (labOrder is null)
            return Result<LabResultResponse>.Failure("LAB_RESULT_NOT_FOUND", "Không tìm thấy chỉ định XN");

        // Lookup encounter to get patient_id
        var encounter = await _db.Encounters
            .FirstOrDefaultAsync(e => e.Id.ToString() == labOrder.EncounterId, ct);

        var patientId   = encounter?.PatientId ?? string.Empty;
        var encounterId = labOrder.EncounterId;

        // Basic flag calculation (no dict lookup for simplicity)
        var flag = _flagCalc.Calculate(req.ValueNumeric, null, null);

        var entity = new LabResult
        {
            TenantId      = _tenant.TenantId,
            LabOrderId    = labOrder.Id.ToString(),
            LabOrderItemId = req.LabOrderItemId.ToString(),
            PatientId     = patientId,
            EncounterId   = encounterId,
            TestCode      = labOrder.TestCode,
            TestName      = labOrder.TestName,
            Value         = req.Value,
            ValueNumeric  = req.ValueNumeric,
            Unit          = req.Unit,
            Flag          = flag,
            Method        = req.Method,
            PerformedAt   = req.PerformedAt,
            PerformedBy   = _user.UserId?.ToString(),
            Status        = LabResultStatus.Draft,
            Source        = LabResultSource.Manual,
            Note          = req.Note,
            CreatedBy     = _user.UserId,
        };

        _db.LabResults.Add(entity);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync("CREATE", "LabResult", entity.Id.ToString(), new { req.LabOrderItemId }, ct);

        return Result<LabResultResponse>.Success(Mapper.Map(entity));
    }
}

// ═══════════════════════════════════════════════
// UPDATE
// ═══════════════════════════════════════════════
public class UpdateLabResultCommandHandler
    : IRequestHandler<UpdateLabResultCommand, Result<LabResultResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;
    private readonly IAuditService _audit;
    private readonly ILabResultFlagCalculator _flagCalc;

    public UpdateLabResultCommandHandler(IApplicationDbContext db,
        ICurrentUser user, IAuditService audit, ILabResultFlagCalculator flagCalc)
    { _db = db; _user = user; _audit = audit; _flagCalc = flagCalc; }

    public async Task<Result<LabResultResponse>> Handle(UpdateLabResultCommand cmd, CancellationToken ct)
    {
        var entity = await _db.LabResults.FirstOrDefaultAsync(e => e.Id == cmd.Id, ct);
        if (entity is null)
            return Result<LabResultResponse>.Failure("LAB_RESULT_NOT_FOUND", "Không tìm thấy kết quả XN");

        var now = DateTime.UtcNow;
        if (entity.Status == LabResultStatus.Verified)
        {
            if (entity.VerifiedAt.HasValue && (now - entity.VerifiedAt.Value).TotalMinutes > 15)
                return Result<LabResultResponse>.Failure("LAB_RESULT_EDIT_TIMEOUT",
                    "Đã quá 15 phút sau khi xác thực, không thể sửa. Vui lòng dùng AMEND.");
        }

        var req = cmd.Req;
        var newStatus = entity.Status == LabResultStatus.Verified ? LabResultStatus.Amended : entity.Status;
        var valNum = req.ValueNumeric ?? entity.ValueNumeric;
        var flag   = _flagCalc.Calculate(valNum, entity.ReferenceRangeLow, entity.ReferenceRangeHigh);

        if (req.Value is not null)    entity.Value         = req.Value;
        if (req.ValueNumeric.HasValue) entity.ValueNumeric = req.ValueNumeric;
        if (req.Unit is not null)     entity.Unit          = req.Unit;
        if (req.Method is not null)   entity.Method        = req.Method;
        if (req.Note is not null)     entity.Note          = req.Note;
        entity.Flag      = flag;
        entity.Status    = newStatus;
        entity.UpdatedBy = _user.UserId;

        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("UPDATE", "LabResult", cmd.Id.ToString(),
            new { newStatus, req.AmendReason }, ct);

        return Result<LabResultResponse>.Success(Mapper.Map(entity));
    }
}

// ═══════════════════════════════════════════════
// VERIFY
// ═══════════════════════════════════════════════
public class VerifyLabResultCommandHandler
    : IRequestHandler<VerifyLabResultCommand, Result<bool>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;
    private readonly IAuditService _audit;

    public VerifyLabResultCommandHandler(IApplicationDbContext db, ICurrentUser user, IAuditService audit)
    { _db = db; _user = user; _audit = audit; }

    public async Task<Result<bool>> Handle(VerifyLabResultCommand cmd, CancellationToken ct)
    {
        var entity = await _db.LabResults.FirstOrDefaultAsync(e => e.Id == cmd.Id, ct);
        if (entity is null)
            return Result<bool>.Failure("LAB_RESULT_NOT_FOUND", "Không tìm thấy kết quả XN");
        if (entity.Status == LabResultStatus.Verified)
            return Result<bool>.Failure("LAB_RESULT_ALREADY_VERIFIED", "Kết quả đã được xác thực");

        var now = DateTime.UtcNow;
        entity.Status     = LabResultStatus.Verified;
        entity.VerifiedAt = now;
        entity.VerifiedBy = _user.UserId?.ToString();
        entity.UpdatedBy  = _user.UserId;

        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("VERIFY", "LabResult", cmd.Id.ToString(), null, ct);

        return Result<bool>.Success(true);
    }
}

// ═══════════════════════════════════════════════
// UNVERIFY
// ═══════════════════════════════════════════════
public class UnverifyLabResultCommandHandler
    : IRequestHandler<UnverifyLabResultCommand, Result<bool>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;
    private readonly IAuditService _audit;

    public UnverifyLabResultCommandHandler(IApplicationDbContext db, ICurrentUser user, IAuditService audit)
    { _db = db; _user = user; _audit = audit; }

    public async Task<Result<bool>> Handle(UnverifyLabResultCommand cmd, CancellationToken ct)
    {
        var entity = await _db.LabResults.FirstOrDefaultAsync(e => e.Id == cmd.Id, ct);
        if (entity is null)
            return Result<bool>.Failure("LAB_RESULT_NOT_FOUND", "Không tìm thấy kết quả XN");
        if (entity.Status != LabResultStatus.Verified)
            return Result<bool>.Failure("LAB_RESULT_NOT_VERIFIED", "Kết quả chưa ở trạng thái VERIFIED");

        if (entity.VerifiedAt.HasValue && (DateTime.UtcNow - entity.VerifiedAt.Value).TotalMinutes > 30)
            return Result<bool>.Failure("LAB_RESULT_EDIT_TIMEOUT", "Đã quá 30 phút, không thể hủy xác thực");

        entity.Status     = LabResultStatus.Draft;
        entity.VerifiedAt = null;
        entity.VerifiedBy = null;
        entity.UpdatedBy  = _user.UserId;

        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("UNVERIFY", "LabResult", cmd.Id.ToString(), null, ct);

        return Result<bool>.Success(true);
    }
}

// ═══════════════════════════════════════════════
// IMPORT CSV / HL7
// ═══════════════════════════════════════════════
public class ImportLabResultsCommandHandler
    : IRequestHandler<ImportLabResultsCommand, Result<ImportLabResultsResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _user;
    private readonly IHl7v25Parser _hl7Parser;
    private readonly ILabResultFlagCalculator _flagCalc;
    private readonly ILogger<ImportLabResultsCommandHandler> _logger;

    public ImportLabResultsCommandHandler(IApplicationDbContext db, ITenantProvider tenant,
        ICurrentUser user, IHl7v25Parser hl7Parser, ILabResultFlagCalculator flagCalc,
        ILogger<ImportLabResultsCommandHandler> logger)
    { _db = db; _tenant = tenant; _user = user; _hl7Parser = hl7Parser; _flagCalc = flagCalc; _logger = logger; }

    public async Task<Result<ImportLabResultsResponse>> Handle(ImportLabResultsCommand cmd, CancellationToken ct)
    {
        List<(string? OrderId, string TestCode, string Value, decimal? ValueNum, string? Unit, DateTime PerformedAt)> rows;

        try
        {
            if (cmd.Format == "CSV")
                rows = ParseCsv(cmd.FileStream);
            else if (cmd.Format == "HL7_ORU")
                rows = ParseHl7(cmd.FileStream);
            else
                return Result<ImportLabResultsResponse>.Failure("LAB_IMPORT_INVALID_FORMAT",
                    "Dinh dang khong hop le. Dung CSV hoac HL7_ORU");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Import parse error");
            return Result<ImportLabResultsResponse>.Failure("LAB_IMPORT_PARSE_ERROR",
                $"Loi parse file: {ex.Message}");
        }

        var errors  = new List<ImportErrorItem>();
        var success = 0;
        var now     = DateTime.UtcNow;

        for (int i = 0; i < rows.Count; i++)
        {
            var (orderId, testCode, value, valNum, unit, performedAt) = rows[i];
            try
            {
                if (string.IsNullOrWhiteSpace(orderId) || string.IsNullOrWhiteSpace(testCode))
                {
                    errors.Add(new(i + 2, "Thieu lab_order_id hoac test_code"));
                    continue;
                }

                var order = await _db.LabOrders
                    .FirstOrDefaultAsync(o => o.Id.ToString() == orderId, ct);

                if (order is null)
                {
                    errors.Add(new(i + 2, $"Khong tim thay lab_order_id={orderId}"));
                    continue;
                }

                var encounter = await _db.Encounters
                    .FirstOrDefaultAsync(e => e.Id.ToString() == order.EncounterId, ct);

                var flag   = _flagCalc.Calculate(valNum, null, null);
                var status = cmd.AutoVerify ? LabResultStatus.Verified : LabResultStatus.Draft;

                var entity = new LabResult
                {
                    TenantId     = _tenant.TenantId,
                    LabOrderId   = order.Id.ToString(),
                    LabOrderItemId = orderId,
                    PatientId    = encounter?.PatientId ?? string.Empty,
                    EncounterId  = order.EncounterId,
                    TestCode     = testCode,
                    TestName     = testCode,
                    Value        = value,
                    ValueNumeric = valNum,
                    Unit         = unit,
                    Flag         = flag,
                    PerformedAt  = performedAt,
                    PerformedBy  = _user.UserId?.ToString(),
                    Status       = status,
                    Source       = LabResultSource.Import,
                    VerifiedAt   = cmd.AutoVerify ? now : null,
                    VerifiedBy   = cmd.AutoVerify ? _user.UserId?.ToString() : null,
                    CreatedBy    = _user.UserId,
                };

                _db.LabResults.Add(entity);
                success++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Import row {Row} failed", i + 2);
                errors.Add(new(i + 2, ex.Message));
            }
        }

        if (success > 0)
            await _db.SaveChangesAsync(ct);

        return Result<ImportLabResultsResponse>.Success(
            new(rows.Count, success, errors.Count, errors));
    }

    private static List<(string?, string, string, decimal?, string?, DateTime)> ParseCsv(Stream stream)
    {
        using var reader = new System.IO.StreamReader(stream);
        var result = new List<(string?, string, string, decimal?, string?, DateTime)>();
        reader.ReadLine(); // skip header
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var parts = line.Split(',');
            if (parts.Length < 3) throw new FormatException($"Dong khong du cot: {line}");
            var orderId     = parts[0].Trim();
            var testCode    = parts[1].Trim();
            var value       = parts[2].Trim();
            var unit        = parts.Length > 3 ? parts[3].Trim() : null;
            var performedAt = parts.Length > 4 && DateTime.TryParse(parts[4].Trim(), out var dt) ? dt : DateTime.UtcNow;
            decimal.TryParse(value, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var valNum);
            result.Add((orderId, testCode, value,
                valNum == 0 && !decimal.TryParse(value, out _) ? null : valNum,
                string.IsNullOrEmpty(unit) ? null : unit, performedAt));
        }
        return result;
    }

    private List<(string?, string, string, decimal?, string?, DateTime)> ParseHl7(Stream stream)
    {
        using var reader = new System.IO.StreamReader(stream);
        var content = reader.ReadToEnd();
        var parsed  = _hl7Parser.Parse(content);
        return parsed.Select(r => (r.LabOrderId, r.TestCode, r.Value, r.ValueNumeric, r.Unit, r.PerformedAt)).ToList();
    }
}

// ═══════════════════════════════════════════════
// ABNORMAL
// ═══════════════════════════════════════════════
public class GetAbnormalLabResultsQueryHandler
    : IRequestHandler<GetAbnormalLabResultsQuery, Result<IReadOnlyList<LabResultResponse>>>
{
    private readonly IApplicationDbContext _db;

    public GetAbnormalLabResultsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<IReadOnlyList<LabResultResponse>>> Handle(
        GetAbnormalLabResultsQuery q, CancellationToken ct)
    {
        var abnormalFlags = q.Severity == "CRITICAL_ONLY"
            ? new[] { LabResultFlag.Critical }
            : new[] { LabResultFlag.H, LabResultFlag.L, LabResultFlag.HH, LabResultFlag.LL, LabResultFlag.Critical };

        var query = _db.LabResults
            .Where(e => abnormalFlags.Contains(e.Flag));

        if (q.FromDate.HasValue) query = query.Where(e => e.PerformedAt >= q.FromDate.Value);
        if (q.ToDate.HasValue)   query = query.Where(e => e.PerformedAt <= q.ToDate.Value.AddDays(1));

        var items = await query
            .OrderByDescending(e => e.PerformedAt)
            .Take(200)
            .Select(e => Mapper.Map(e))
            .ToListAsync(ct);

        return Result<IReadOnlyList<LabResultResponse>>.Success(items);
    }
}

// ═══════════════════════════════════════════════
// HISTORY TREND
// ═══════════════════════════════════════════════
public class GetLabResultHistoryTrendQueryHandler
    : IRequestHandler<GetLabResultHistoryTrendQuery, Result<LabResultTrendResponse>>
{
    private readonly IApplicationDbContext _db;

    public GetLabResultHistoryTrendQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<LabResultTrendResponse>> Handle(
        GetLabResultHistoryTrendQuery q, CancellationToken ct)
    {
        var query = _db.LabResults
            .Where(e => e.PatientId == q.PatientId.ToString()
                     && e.TestCode  == q.TestCode
                     && e.Status    == LabResultStatus.Verified);

        if (q.FromDate.HasValue) query = query.Where(e => e.PerformedAt >= q.FromDate.Value);
        if (q.ToDate.HasValue)   query = query.Where(e => e.PerformedAt <= q.ToDate.Value.AddDays(1));

        var rows = await query
            .OrderBy(e => e.PerformedAt)
            .ToListAsync(ct);

        decimal? refLow  = rows.FirstOrDefault()?.ReferenceRangeLow;
        decimal? refHigh = rows.FirstOrDefault()?.ReferenceRangeHigh;
        string?  unit    = rows.FirstOrDefault()?.Unit;
        var testName     = rows.FirstOrDefault()?.TestName ?? q.TestCode;

        var points = rows.Select(r => new TrendPoint(r.PerformedAt, r.ValueNumeric, r.Flag)).ToList();

        return Result<LabResultTrendResponse>.Success(new(
            q.TestCode, testName, unit, refLow, refHigh, points));
    }
}

// ═══════════════════════════════════════════════
// EXPORT PDF (stub)
// ═══════════════════════════════════════════════
public class ExportLabResultPdfQueryHandler
    : IRequestHandler<ExportLabResultPdfQuery, Result<byte[]>>
{
    private readonly IApplicationDbContext _db;
    private readonly ILabResultPdfExporter _pdfExporter;

    public ExportLabResultPdfQueryHandler(IApplicationDbContext db, ILabResultPdfExporter pdfExporter)
    { _db = db; _pdfExporter = pdfExporter; }

    public async Task<Result<byte[]>> Handle(ExportLabResultPdfQuery q, CancellationToken ct)
    {
        var entity = await _db.LabResults.FirstOrDefaultAsync(e => e.Id == q.Id, ct);
        if (entity is null)
            return Result<byte[]>.Failure("LAB_RESULT_NOT_FOUND", "Không tìm thấy kết quả XN");
        if (entity.Status != LabResultStatus.Verified)
            return Result<byte[]>.Failure("LAB_RESULT_NOT_VERIFIED", "Chỉ xuất PDF khi kết quả đã xác thực");

        var pdf = await _pdfExporter.ExportLabResultAsync(entity, ct);
        return Result<byte[]>.Success(pdf);
    }
}

// ═══════════════════════════════════════════════
// BATCH VERIFY
// ═══════════════════════════════════════════════
public class BatchVerifyLabResultsCommandHandler
    : IRequestHandler<BatchVerifyLabResultsCommand, Result<BatchVerifyResponse>>
{
    private readonly IMediator _mediator;

    public BatchVerifyLabResultsCommandHandler(IMediator mediator) => _mediator = mediator;

    public async Task<Result<BatchVerifyResponse>> Handle(BatchVerifyLabResultsCommand cmd, CancellationToken ct)
    {
        var errors  = new List<BatchVerifyErrorItem>();
        var success = 0;

        foreach (var id in cmd.ResultIds)
        {
            var result = await _mediator.Send(new VerifyLabResultCommand(id), ct);
            if (result.IsSuccess)
                success++;
            else
                errors.Add(new(id.ToString(), result.ErrorCode!, result.ErrorMessage!));
        }

        return Result<BatchVerifyResponse>.Success(new(success, errors.Count, errors));
    }
}
