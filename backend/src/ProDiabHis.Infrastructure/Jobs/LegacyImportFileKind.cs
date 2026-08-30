using System.Linq;

namespace ProDiabHis.Infrastructure.Jobs;

/// <summary>Phan loai 1 file entry trong ZIP nhap ho so giay cu theo phan mo rong.</summary>
public enum LegacyImportFileKind
{
    /// <summary>Khong nam trong whitelist - bo qua am tham (vd .docx, .txt lac vao zip).</summary>
    Ignored,
    /// <summary>Anh OCR truc tiep bang Tesseract (jpg/jpeg/png/tiff/tif/bmp).</summary>
    Image,
    /// <summary>PDF - xu ly qua IPdfTextExtractor (text layer hoac render+OCR fallback).</summary>
    Pdf,
    /// <summary>Dinh dang nhan dien duoc nhung chua ho tro (heic/heif) - tao item 'failed' voi
    /// thong bao ro rang, KHONG am tham bo qua.</summary>
    UnsupportedGuard
}

/// <summary>
/// Logic phan loai file dung chung boi <see cref="LegacyOcrBatchJob"/> - tach rieng thanh static
/// class de unit test doc lap, khong can dung DB/MinIO/Hangfire.
/// </summary>
public static class LegacyImportFileClassifier
{
    public static readonly string[] AllowedImageExts = { ".jpg", ".jpeg", ".png", ".tiff", ".tif", ".bmp" };
    public const string PdfExt = ".pdf";
    public static readonly string[] UnsupportedGuardExts = { ".heic", ".heif" };

    public static LegacyImportFileKind Classify(string fileName)
    {
        var ext = System.IO.Path.GetExtension(fileName).ToLowerInvariant();
        if (ext == PdfExt) return LegacyImportFileKind.Pdf;
        if (AllowedImageExts.Contains(ext)) return LegacyImportFileKind.Image;
        if (UnsupportedGuardExts.Contains(ext)) return LegacyImportFileKind.UnsupportedGuard;
        return LegacyImportFileKind.Ignored;
    }
}
