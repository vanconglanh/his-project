using System.Globalization;
using System.Text.Json;
using System.Xml.Linq;
using ProDiabHis.Application.Bhyt;

namespace ProDiabHis.Infrastructure.Bhyt;

/// <summary>
/// Serialize du lieu Bang 1-5 (JSON per-row, xem <see cref="BhytXmlGeneratorImpl"/>) ra file XML
/// nop giam dinh BHYT theo QD 3176/QD-BYT (29/10/2024, dang ap dung 2026; ke thua Phu luc 01 kem
/// Cong van 47/BHXH-CNTT ky so 08/01/2026 cua BHXH Viet Nam).
///
/// LUU Y VE SCHEMA: cac file backend/src/ProDiabHis.Api/Resources/Xsd/qd3176/bang*.xsd trong repo
/// la PLACEHOLDER (tu ghi chu "Can thay the bang XSD chinh thuc tu BYT khi co san") — KHONG phai
/// XSD chinh thuc cua Bo Y te, chi dinh nghia <BangN><Row index="N"><xs:any lax/></Row></BangN>.
/// Vi vay khung phan tu (GIAMDINHHS/THONGTUYEN/BangN/Row) va cac ten the da co san trong repo
/// (MA_LIEN_KET, MA_BENH_CHINH, MA_BENH_KT, ...) duoc dung lam ten the XML; CHUA the doi chieu 100%
/// voi XSD chinh thuc vi repo khong co ban that.
/// </summary>
public class BhytXmlSerializerImpl : IBhytXmlSerializer
{
    public string Serialize(int exportId, string tenantCode, string periodMonth,
        IReadOnlyList<BhytExportItemData> items)
    {
        var now = DateTime.UtcNow;

        var header = new XElement("THONGTUYEN",
            new XElement("MA_CSKCB", tenantCode),
            new XElement("KY_BAO_CAO", periodMonth),
            new XElement("NGAY_LAP", now.ToString("yyyyMMddHHmm", CultureInfo.InvariantCulture)),
            new XElement("SO_LUONG_HO_SO", items.Select(i => i.MaLienKet).Distinct().Count()));

        var root = new XElement("GIAMDINHHS",
            new XAttribute("exportId", exportId),
            header);

        for (int tableNo = 1; tableNo <= 5; tableNo++)
        {
            var bangEl = new XElement($"Bang{tableNo}");

            // Bang 4 (dich vu CDHA rieng) chua co nguon du lieu trong pipeline GenerateAsync hien tai
            // (xem TODO trong BhytXmlGeneratorImpl) -> luon rong nhung hop le (minOccurs=0 tren Row).
            var rows = items.Where(i => i.TableNo == tableNo).OrderBy(i => i.RecordIndex);

            foreach (var item in rows)
            {
                var rowEl = new XElement("Row", new XAttribute("index", item.RecordIndex));
                using var doc = JsonDocument.Parse(item.RowDataJson);
                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    rowEl.Add(new XElement(prop.Name, FormatValue(prop.Value)));
                }
                bangEl.Add(rowEl);
            }

            root.Add(bangEl);
        }

        var xdoc = new XDocument(new XDeclaration("1.0", "utf-8", null), root);
        using var sw = new Utf8StringWriter();
        xdoc.Save(sw);
        return sw.ToString();
    }

    /// <summary>
    /// Dinh dang gia tri XML theo QD 3176: ngay-gio -> yyyyMMddHHmm, ngay -> yyyyMMdd,
    /// so tien/so luong -> khong phan cach hang nghin, null -> rong.
    /// </summary>
    private static string FormatValue(JsonElement value)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.Null:
            case JsonValueKind.Undefined:
                return "";
            case JsonValueKind.True:
                return "1";
            case JsonValueKind.False:
                return "0";
            case JsonValueKind.Number:
                return value.GetRawText();
            case JsonValueKind.String:
                var s = value.GetString() ?? "";
                if (DateTime.TryParse(s, CultureInfo.InvariantCulture,
                        DateTimeStyles.RoundtripKind, out var dt))
                {
                    // Chuoi ngay thuan "yyyy-MM-dd" (khong gio) -> yyyyMMdd; con lai (co gio) -> yyyyMMddHHmm
                    return s.Length == 10 && s.IndexOf('T') < 0
                        ? dt.ToString("yyyyMMdd", CultureInfo.InvariantCulture)
                        : dt.ToString("yyyyMMddHHmm", CultureInfo.InvariantCulture);
                }
                return s;
            default:
                return value.GetRawText();
        }
    }

    private sealed class Utf8StringWriter : System.IO.StringWriter
    {
        public override System.Text.Encoding Encoding => System.Text.Encoding.UTF8;
    }
}
