using MediatR;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.LabResults.Ocr;

// ═══════════════════════════════════════════════════════════════════════════
// MODEL / DTO cho tinh nang OCR doc file ket qua xet nghiem (CLS/Lab).
//
// Khac InBody (9 chi so co dinh, mapping label->field cung): phieu ket qua XN
// KHONG co form co dinh, moi doi tac lab in khac nhau. Loi the: he thong DA BIET
// truoc dang cho ket qua cho DUNG nhung LabOrderItem nao (bang diab_his_cli_lab_orders).
// -> Parser chi tim gia tri so gan ten/ma cua nhung XN dang cho trong dung 1 lan
//    upload, thay vi co hieu toan bo layout phieu.
//
// Nguyen tac an toan (giong InBody): LUON qua man xac nhan truoc khi ghi LabResult,
// khong tu dong ghi gia tri y te. Field khong doc duoc -> de trong, tag "Chua doc duoc",
// khong chan cac field con lai.
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>1 chi dinh XN dang cho ket qua (input cho parser — lay tu pending lab order items).</summary>
public sealed record LabOcrPendingTest(Guid LabOrderItemId, string TestCode, string TestName);

/// <summary>Ket qua trich xuat OCR cho 1 chi dinh XN.</summary>
public sealed record LabOcrFieldResult(
    Guid     LabOrderItemId,
    string   TestCode,
    string   TestName,
    string?  RawValue,       // chuoi gia tri doc duoc nguyen ban (vd "7.2")
    decimal? ValueNumeric,   // gia tri so parse duoc (null neu khong phai so)
    string?  Unit,           // don vi doc duoc gan ben (co the null)
    bool     Extracted);     // true = doc duoc so; false = khong tim thay -> "Chua doc duoc"

/// <summary>Ket qua parse toan bo 1 file OCR.</summary>
public sealed record LabOcrParseResult(string RawText, IReadOnlyList<LabOcrFieldResult> Fields)
{
    public bool HasAnyExtracted => Fields.Any(f => f.Extracted);
    public int ExtractedCount   => Fields.Count(f => f.Extracted);
}

// ─────────── Response DTO tra ve UI ───────────
public record LabOcrExtractFieldDto(
    Guid     LabOrderItemId,
    string   TestCode,
    string   TestName,
    string?  Value,
    decimal? ValueNumeric,
    string?  Unit,
    bool     Extracted,
    // GAP-3: co canh bao gia tri ngoai khoang VAT LY KHA DI (chan OCR doc nham dau thap phan).
    bool     OutOfPlausibleRange = false,
    string?  PlausibleRangeNote  = null);

public record LabOcrExtractResponse(
    Guid                              EncounterId,
    int                               PendingCount,
    int                               ExtractedCount,
    IReadOnlyList<LabOcrExtractFieldDto> Fields,
    // GAP-8: id file goc da luu tren MinIO (fil_files.id). FE giu tam va gui lai o buoc confirm.
    Guid?    SourceFileId = null);

public record LabOcrConfirmItem(
    Guid     LabOrderItemId,
    string   Value,
    decimal? ValueNumeric,
    string?  Unit,
    string?  Method,
    bool     Include,
    // GAP-2: gia tri OCR goc FE gui lai (de luu neu nguoi dung sua khac). Null = nhap tay.
    string?  OcrRawValue = null);

public record LabOcrConfirmResponse(
    int                       CreatedCount,
    int                       FailedCount,
    IReadOnlyList<ImportErrorItem> Errors);

// ─────────── Commands ───────────
/// <summary>Upload file (PDF/anh) + OCR + parse theo cac XN dang cho cua encounter. KHONG ghi DB.</summary>
public record ExtractLabResultOcrCommand(Guid EncounterId, Stream FileStream, string FileName, string ContentType)
    : IRequest<Result<LabOcrExtractResponse>>;

/// <summary>Xac nhan cac gia tri (da sua tay neu can) -> tao LabResult qua luong CreateLabResult san co.</summary>
public record ConfirmLabResultOcrCommand(DateTime PerformedAt, IReadOnlyList<LabOcrConfirmItem> Items, Guid? SourceFileId = null)
    : IRequest<Result<LabOcrConfirmResponse>>;
