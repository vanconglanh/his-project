using Dapper;
using MediatR;
using Microsoft.Extensions.Logging;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.Common.Interfaces;

namespace ProDiabHis.Application.LabResults.Ocr;

// ═══════════════════════════════════════════════════════════════════════════
// EXTRACT — upload file (PDF/anh) -> OCR -> parse theo cac XN dang cho ket qua
// cua encounter. KHONG ghi DB (stateless), chi tra ve de UI xac nhan.
// ═══════════════════════════════════════════════════════════════════════════
public class ExtractLabResultOcrCommandHandler
    : IRequestHandler<ExtractLabResultOcrCommand, Result<LabOcrExtractResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    private readonly ILabOcrTextProvider _ocr;
    private readonly ILogger<ExtractLabResultOcrCommandHandler> _logger;

    // Chap nhan PDF + cac dinh dang anh scan pho bien (Tesseract doc duoc qua SkiaSharp)
    private static readonly string[] AllowedMimes =
    {
        "application/pdf", "image/png", "image/jpeg", "image/jpg", "image/webp", "image/bmp", "image/tiff"
    };
    private const long MaxBytes = 20L * 1024 * 1024;

    public ExtractLabResultOcrCommandHandler(IDapperConnectionFactory db, ITenantProvider tenant,
        ILabOcrTextProvider ocr, ILogger<ExtractLabResultOcrCommandHandler> logger)
    { _db = db; _tenant = tenant; _ocr = ocr; _logger = logger; }

    public async Task<Result<LabOcrExtractResponse>> Handle(ExtractLabResultOcrCommand cmd, CancellationToken ct)
    {
        var contentType = (cmd.ContentType ?? string.Empty).ToLowerInvariant();
        if (!AllowedMimes.Contains(contentType))
            return Result<LabOcrExtractResponse>.Failure("LAB_OCR_INVALID_FORMAT",
                "Chỉ chấp nhận file PDF hoặc ảnh (PNG/JPG/WEBP/BMP/TIFF)");

        using var buffer = new MemoryStream();
        await cmd.FileStream.CopyToAsync(buffer, ct);
        if (buffer.Length == 0)
            return Result<LabOcrExtractResponse>.Failure("LAB_OCR_UPLOAD_FAILED", "Tải tệp thất bại, vui lòng thử lại");
        if (buffer.Length > MaxBytes)
            return Result<LabOcrExtractResponse>.Failure("LAB_OCR_TOO_LARGE", "File vượt quá dung lượng tối đa 20MB");

        // Lay danh sach XN dang cho ket qua cua dung encounter nay (chua co LabResult, chua bi huy)
        var pending = await LoadPendingTestsAsync(cmd.EncounterId, ct);
        if (pending.Count == 0)
            return Result<LabOcrExtractResponse>.Failure("LAB_OCR_NO_PENDING",
                "Lượt khám này không còn chỉ định xét nghiệm nào đang chờ kết quả");

        // Trich text qua ha tang OCR da co (PDF: PdfPig + fallback; anh: Tesseract)
        var extract = await _ocr.ExtractTextAsync(buffer.ToArray(), cmd.FileName, contentType, ct);
        if (!extract.IsSuccess)
            return Result<LabOcrExtractResponse>.Failure(extract.ErrorCode!, extract.ErrorMessage!);

        var parseResult = LabResultOcrParser.Parse(extract.Value, pending);

        _logger.LogInformation("Lab OCR extract encounter={EncounterId} pending={Pending} extracted={Extracted}",
            cmd.EncounterId, pending.Count, parseResult.ExtractedCount);

        var fields = parseResult.Fields
            .Select(f => new LabOcrExtractFieldDto(f.LabOrderItemId, f.TestCode, f.TestName,
                f.RawValue, f.ValueNumeric, f.Unit, f.Extracted))
            .ToList();

        return Result<LabOcrExtractResponse>.Success(new LabOcrExtractResponse(
            cmd.EncounterId, pending.Count, parseResult.ExtractedCount, fields));
    }

    private async Task<IReadOnlyList<LabOcrPendingTest>> LoadPendingTestsAsync(Guid encounterId, CancellationToken ct)
    {
        // Cung dieu kien voi ListPendingLabOrderItemsQuery nhung khoanh theo 1 encounter.
        const string sql = @"
            SELECT  o.id         AS Id,
                    o.test_code  AS TestCode,
                    o.test_name  AS TestName
            FROM diab_his_cli_lab_orders o
            WHERE o.tenant_id = @TId
              AND o.encounter_id = @EncId
              AND o.deleted_at IS NULL
              AND o.status <> 'cancelled'
              AND NOT EXISTS (
                    SELECT 1 FROM diab_his_lab_results r
                    WHERE r.lab_order_item_id = o.id
                      AND r.tenant_id = o.tenant_id
                      AND r.deleted_at IS NULL)
            ORDER BY o.ordered_at";

        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<dynamic>(new CommandDefinition(sql,
            new { TId = _tenant.TenantId, EncId = encounterId.ToString() }, cancellationToken: ct));

        var list = new List<LabOcrPendingTest>();
        foreach (var r in rows)
        {
            if (!Guid.TryParse((string?)r.Id, out var itemId)) continue;
            list.Add(new LabOcrPendingTest(itemId, (string?)r.TestCode ?? string.Empty, (string?)r.TestName ?? string.Empty));
        }
        return list;
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// CONFIRM — nguoi dung xac nhan/sua tay roi luu. TAI DUNG CreateLabResultCommand
// (da co payment gate G02, tinh flag, SoD, audit) — khong nhan doi logic tao KQ.
// ═══════════════════════════════════════════════════════════════════════════
public class ConfirmLabResultOcrCommandHandler
    : IRequestHandler<ConfirmLabResultOcrCommand, Result<LabOcrConfirmResponse>>
{
    private readonly IMediator _mediator;

    public ConfirmLabResultOcrCommandHandler(IMediator mediator) => _mediator = mediator;

    public async Task<Result<LabOcrConfirmResponse>> Handle(ConfirmLabResultOcrCommand cmd, CancellationToken ct)
    {
        var included = cmd.Items.Where(i => i.Include).ToList();
        if (included.Count == 0)
            return Result<LabOcrConfirmResponse>.Failure("LAB_OCR_NOTHING_TO_SAVE",
                "Chưa chọn kết quả nào để lưu");

        var errors = new List<ImportErrorItem>();
        var created = 0;
        var row = 0;

        foreach (var item in included)
        {
            row++;
            if (string.IsNullOrWhiteSpace(item.Value))
            {
                errors.Add(new ImportErrorItem(row, $"XN dòng {row}: giá trị trống, bỏ qua"));
                continue;
            }

            var req = new LabResultCreateRequest(
                item.LabOrderItemId,
                item.Value.Trim(),
                item.ValueNumeric,
                string.IsNullOrWhiteSpace(item.Unit) ? null : item.Unit!.Trim(),
                string.IsNullOrWhiteSpace(item.Method) ? "Đọc từ file kết quả (OCR)" : item.Method!.Trim(),
                cmd.PerformedAt,
                Note: null);

            var result = await _mediator.Send(new CreateLabResultCommand(req), ct);
            if (result.IsSuccess) created++;
            else errors.Add(new ImportErrorItem(row, $"{result.ErrorCode}: {result.ErrorMessage}"));
        }

        return Result<LabOcrConfirmResponse>.Success(
            new LabOcrConfirmResponse(created, errors.Count, errors));
    }
}
