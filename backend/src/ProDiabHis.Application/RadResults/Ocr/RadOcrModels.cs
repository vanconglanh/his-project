using MediatR;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.RadResults.Ocr;

// ═══════════════════════════════════════════════════════════════════════════
// MODEL / DTO cho tinh nang OCR doc file phieu ket qua CDHA (X-quang/Sieu am/CT...).
//
// Khac Lab OCR (trich GIA TRI SO theo ten xet nghiem dang cho): phieu CDHA la
// VAN BAN MO TA TU DO — 2 truong chinh:
//   - "Mo ta" / "Ket qua" / "Nhan xet" / "Hinh anh"   -> findings (Mo ta hinh anh)
//   - "Ket luan" / "Chan doan"                         -> conclusion (Ket luan)
//   - "De nghi" / "Khuyen nghi" (neu co)               -> recommendations
// Parser lay TOAN BO doan text tu sau nhan toi nhan ke tiep (hoac het van ban),
// khong tach so don le.
//
// Nguyen tac an toan (giong Lab/InBody): LUON qua man xac nhan truoc khi ghi RadResult.
// Field khong doc duoc -> de trong, nguoi dung tu dien/sua tay truoc khi luu.
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Ket qua parse thuan tu chuoi text da OCR (khong ghi DB).</summary>
public sealed record RadOcrParseResult(
    string  RawText,
    string? Findings,
    string? Impression,
    string? Conclusion,
    string? Recommendations)
{
    public bool HasAnyExtracted =>
        !string.IsNullOrWhiteSpace(Findings) ||
        !string.IsNullOrWhiteSpace(Conclusion) ||
        !string.IsNullOrWhiteSpace(Recommendations);
}

// ─────────── Response DTO tra ve UI ───────────
public record RadOcrExtractResponse(
    string? Findings,
    string? Impression,
    string? Conclusion,
    string? Recommendations,
    bool    HasAnyExtracted,
    string  RawText);

public record RadOcrConfirmResponse(Guid Id, string Status);

// ─────────── Commands ───────────
/// <summary>Upload file (PDF/anh) + OCR + parse tach 2 doan Mo ta/Ket luan. KHONG ghi DB.</summary>
public record ExtractRadResultOcrCommand(Stream FileStream, string FileName, string ContentType)
    : IRequest<Result<RadOcrExtractResponse>>;

/// <summary>Xac nhan (da sua tay neu can) -> tao RadResult qua luong CreateRadResult san co.</summary>
public record ConfirmRadResultOcrCommand(
    Guid     RadOrderId,
    string   Findings,
    string?  Impression,
    string   Conclusion,
    string?  Recommendations,
    DateTime PerformedAt)
    : IRequest<Result<RadOcrConfirmResponse>>;
