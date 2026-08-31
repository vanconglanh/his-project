using System.Net.Http.Headers;

namespace ProDiabHis.IntegrationTests.Infrastructure;

/// <summary>
/// Tao body cho cac endpoint nhan multipart/form-data (upload file, import Excel, OCR...).
///
/// VI SAO CAN: endpoint co [Consumes("multipart/form-data")] duoc ASP.NET kiem tra Content-Type
/// o ConsumesAttribute (mot IResourceFilter) — chay TRUOC authorization filter. Gui JSON vao
/// endpoint nay se nhan 415 UnsupportedMediaType chu KHONG phai 401/403, khien test phan quyen
/// khong con y nghia. Phai gui dung multipart thi authorization moi thuc su duoc kiem.
/// </summary>
public static class TestContent
{
    /// <summary>Multipart chua 1 file nho — du de qua duoc kiem tra Content-Type.</summary>
    public static MultipartFormDataContent File(
        string fieldName = "file",
        string fileName = "test.xlsx",
        string contentType = "application/octet-stream")
    {
        var form = new MultipartFormDataContent();
        var bytes = new byte[] { 0x50, 0x4B, 0x03, 0x04, 0x00, 0x00 }; // header ZIP/XLSX toi thieu
        var part = new ByteArrayContent(bytes);
        part.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        form.Add(part, fieldName, fileName);
        return form;
    }
}
