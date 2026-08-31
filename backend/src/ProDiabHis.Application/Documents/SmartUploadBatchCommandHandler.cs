using MediatR;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.Documents;

/// <summary>
/// Handler batch (orchestrator lop ngoai) — KHONG ghi DB truc tiep, KHONG viet lai logic xu ly-1-file.
/// Nhiem vu duy nhat: xac dinh danh sach file thuc su can xu ly (giai nen ZIP neu la 1 file .zip,
/// nguoc lai dung truc tiep cac file da chon), roi goi lai <see cref="SmartUploadCommand"/> cho TUNG
/// file mot cach doc lap va gom ket qua theo tung file.
///
/// Xu ly DONG BO (tra ket qua ngay, khong polling): dung cho thao tac hang ngay (chon vai anh / 1 zip
/// nho). Voi batch lon ho so giay cu -> dung chuc nang Nhap ho so giay cu (Legacy-import) chay nen
/// bang Hangfire da co. Cap <see cref="MaxFilesPerRequest"/> de tranh timeout HTTP.
/// </summary>
public class SmartUploadBatchCommandHandler : IRequestHandler<SmartUploadBatchCommand, Result<SmartUploadBatchResponse>>
{
    private const int MaxFilesPerRequest = 20;

    // Whitelist dinh dang khi giai nen ZIP: PDF + cac dinh dang anh Tesseract/PdfPig doc duoc
    // (giong LegacyImportFileClassifier nhung KHONG gom heic/heif — smart-upload khong tao item
    // 'failed' cho, nguoi dung se thay file khong hop le bi bo qua trong ZIP).
    private static readonly string[] AllowedExts = { ".pdf", ".jpg", ".jpeg", ".png", ".tiff", ".tif", ".bmp" };

    // Gioi han giai nen ZIP cho smart-upload (chat hon Legacy-import vi day la luong dong bo dung hang ngay).
    private static readonly ZipExtractLimits ZipLimits = new(
        MaxFiles: MaxFilesPerRequest,
        MaxEntryBytes: 20L * 1024 * 1024,
        MaxTotalBytes: 100L * 1024 * 1024);

    private readonly IMediator _mediator;

    public SmartUploadBatchCommandHandler(IMediator mediator) => _mediator = mediator;

    public async Task<Result<SmartUploadBatchResponse>> Handle(SmartUploadBatchCommand cmd, CancellationToken ct)
    {
        if (cmd.Files is null || cmd.Files.Count == 0)
            return Result<SmartUploadBatchResponse>.Failure("DOC_UPLOAD_FAILED", "Tải tệp thất bại, vui lòng thử lại");

        List<SmartUploadFileInput> files;

        // 1 file duy nhat va la ZIP -> giai nen an toan; nguoc lai (nhieu file / 1 file khong phai zip)
        // -> xu ly truc tiep tung file.
        var single = cmd.Files.Count == 1 ? cmd.Files[0] : null;
        if (single is not null && IsZip(single.FileName, single.ContentType))
        {
            IReadOnlyList<ExtractedZipEntry> entries;
            try
            {
                using var ms = new MemoryStream(single.FileBytes);
                entries = await SafeZipExtractor.ExtractAsync(ms, IsAllowedName, ZipLimits, ct);
            }
            catch (Exception)
            {
                return Result<SmartUploadBatchResponse>.Failure(
                    "DOC_ZIP_INVALID", "Không đọc được tệp ZIP, vui lòng kiểm tra lại tệp");
            }

            if (entries.Count == 0)
                return Result<SmartUploadBatchResponse>.Failure(
                    "DOC_ZIP_EMPTY", "Tệp ZIP không chứa tài liệu hợp lệ (PDF hoặc ảnh)");

            files = entries
                .Select(e => new SmartUploadFileInput(e.Bytes, e.Name, GuessMime(e.Name)))
                .ToList();
        }
        else
        {
            files = cmd.Files.ToList();
        }

        if (files.Count > MaxFilesPerRequest)
            return Result<SmartUploadBatchResponse>.Failure(
                "DOC_TOO_MANY_FILES",
                $"Chỉ xử lý tối đa {MaxFilesPerRequest} tệp mỗi lần. Với số lượng lớn hơn, vui lòng dùng chức năng Nhập hồ sơ giấy cũ.");

        // Xu ly TUNG file DOC LAP — tai dung nguyen luong xu ly-1-file qua SmartUploadCommand.
        var items = new List<SmartUploadItemResult>(files.Count);
        foreach (var f in files)
        {
            ct.ThrowIfCancellationRequested();

            var r = await _mediator.Send(
                new SmartUploadCommand(cmd.PatientId, cmd.EncounterId, f.FileBytes, f.FileName, f.ContentType), ct);

            items.Add(r.IsSuccess
                ? new SmartUploadItemResult(f.FileName, true, null, null, r.Value)
                : new SmartUploadItemResult(f.FileName, false, r.ErrorCode, r.ErrorMessage, null));
        }

        return Result<SmartUploadBatchResponse>.Success(new SmartUploadBatchResponse(items));
    }

    private static bool IsZip(string name, string contentType) =>
        name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)
        || string.Equals(contentType, "application/zip", StringComparison.OrdinalIgnoreCase)
        || string.Equals(contentType, "application/x-zip-compressed", StringComparison.OrdinalIgnoreCase);

    private static bool IsAllowedName(string name) =>
        AllowedExts.Contains(Path.GetExtension(name).ToLowerInvariant());

    private static string GuessMime(string name) => Path.GetExtension(name).ToLowerInvariant() switch
    {
        ".pdf" => "application/pdf",
        ".png" => "image/png",
        ".tiff" or ".tif" => "image/tiff",
        ".bmp" => "image/bmp",
        _ => "image/jpeg"
    };
}
