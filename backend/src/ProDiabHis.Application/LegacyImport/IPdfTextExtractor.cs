using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.LegacyImport;

/// <summary>
/// Trich text tu 1 file PDF ho so giay cu (may scan van phong xuat ra PDF la pho bien nhat).
/// Chien luoc 2 tang:
///   1. Doc TEXT LAYER truc tiep bang PdfPig (nhanh, chinh xac 100% neu PDF da co text -
///      vd PDF "in" tu may scan co OCR san, hoac PDF so hoa).
///   2. Neu tang 1 khong ra du text (PDF la anh scan thuan, khong co text layer) -> render
///      tung trang PDF thanh anh roi chay Tesseract OCR (giong luong OCR anh da co), gop text
///      tat ca cac trang thanh 1 ket qua duy nhat (1 file PDF nhieu trang = 1 bo ho so 1 benh
///      nhan, khong tach item theo tung trang - de admin de quan ly/review hon).
/// </summary>
public interface IPdfTextExtractor
{
    Task<Result<string>> ExtractTextAsync(byte[] pdfBytes, string fileName, CancellationToken ct);
}
