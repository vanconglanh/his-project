using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace ProDiabHis.Application.RadResults.Ocr;

/// <summary>
/// Parser thuan (khong phu thuoc thu vien doc file) — tach 2 doan van ban chinh tren phieu ket qua
/// CDHA tu chuoi text da OCR/doc san: MO TA (findings) va KET LUAN (conclusion), kem De nghi
/// (recommendations) neu co. Tach rieng khoi viec doc file de unit test bang chuoi mau.
///
/// Khac Lab OCR: KHONG trich so don le theo ten xet nghiem. Phieu CDHA la van ban mo ta tu do, moi
/// doan dai nhieu dong.
///
/// Chien luoc "marker": tim vi tri cac NHAN section ("Mo ta", "Ket luan", "De nghi"...) va nhan KET
/// THUC ("Bac si thuc hien"...) trong text. Moi nhan chi tinh khi theo sau la dau ":" (co the cach
/// khoang trang) HOAC chiem tron 1 dong (nhan tieu de tren dong rieng). Rang buoc dau ":" nay loai
/// bo va cham voi TIEU DE phieu ("PHIEU KET QUA X-QUANG", "KHOA CHAN DOAN HINH ANH") — nhung cho
/// nay khong co dau ":" sau nhan. Text giua 2 marker lien tiep -> gan vao section cua marker dau.
///
/// Ly do dung marker thay vi tach dong: engine doc PDF text-layer (PdfPig page.Text) thuong tra ve
/// CA TRANG tren 1 dong khong co ky tu xuong dong -> tach theo dong se khong hoat dong. Marker-based
/// chay dung ca khi co lan khong co xuong dong.
///
/// GIU NGUYEN DAU tieng Viet trong output: chi chuan hoa khong dau (1-1 index voi text goc) de DO
/// KHOP nhan, con noi dung tra ve la text goc.
/// </summary>
public static class RadResultOcrParser
{
    private enum Section { Findings, Conclusion, Recommendations, Stop }

    // Nhan (chuan hoa khong dau, lowercase). Uu tien khop nhan DAI truoc (longest-match).
    private static readonly (string Label, Section Sec)[] Markers =
    {
        // Ket luan / chan doan -> conclusion
        ("ket luan va de nghi", Section.Conclusion),
        ("ket luan - de nghi",  Section.Conclusion),
        ("ket luan chan doan",  Section.Conclusion),
        ("chan doan hinh anh",  Section.Conclusion),
        ("ket luan",            Section.Conclusion),
        ("chan doan",           Section.Conclusion),
        ("impression",          Section.Conclusion),
        ("conclusion",          Section.Conclusion),
        // De nghi / khuyen nghi -> recommendations
        ("de nghi",             Section.Recommendations),
        ("khuyen nghi",         Section.Recommendations),
        ("loi khuyen",          Section.Recommendations),
        ("recommendation",      Section.Recommendations),
        // Mo ta / ket qua / nhan xet / hinh anh -> findings
        ("mo ta hinh anh",      Section.Findings),
        ("mo ta ton thuong",    Section.Findings),
        ("mo ta",               Section.Findings),
        ("ket qua",             Section.Findings),
        ("nhan xet",            Section.Findings),
        ("hinh anh ghi nhan",   Section.Findings),
        ("hinh anh",            Section.Findings),
        ("khao sat",            Section.Findings),
        ("findings",            Section.Findings),
        // KET THUC phan noi dung y khoa (chu ky, hanh chinh) -> Stop
        ("bac si thuc hien",    Section.Stop),
        ("bac si doc ket qua",  Section.Stop),
        ("bac si chan doan",    Section.Stop),
        ("nguoi thuc hien",     Section.Stop),
        ("nguoi doc ket qua",   Section.Stop),
        ("nguoi doc",           Section.Stop),
        ("bac si dieu tri",     Section.Stop),
        ("ky ten",              Section.Stop),
        ("ngay tra ket qua",    Section.Stop),
        ("ngay in",             Section.Stop),
    };

    // Sap xep san theo do dai giam dan de longest-match tai moi vi tri.
    private static readonly (string Label, Section Sec)[] MarkersByLen =
        Markers.OrderByDescending(m => m.Label.Length).ToArray();

    private sealed record Hit(int LabelStart, int ContentStart, Section Sec);

    public static RadOcrParseResult Parse(string? rawText)
    {
        var text = rawText ?? string.Empty;
        var norm = Normalize(text); // cung do dai voi text

        var hits = FindHits(norm);

        var buffers = new Dictionary<Section, StringBuilder>
        {
            [Section.Findings]        = new StringBuilder(),
            [Section.Conclusion]      = new StringBuilder(),
            [Section.Recommendations] = new StringBuilder(),
        };

        for (var i = 0; i < hits.Count; i++)
        {
            var hit = hits[i];
            var end = i + 1 < hits.Count ? hits[i + 1].LabelStart : text.Length;
            if (hit.Sec == Section.Stop) continue; // bo qua noi dung phan chu ky/hanh chinh

            var content = text[hit.ContentStart..end].Trim();
            content = CleanContent(content);
            if (content.Length == 0) continue;

            var sb = buffers[hit.Sec];
            if (sb.Length > 0) sb.Append('\n');
            sb.Append(content);
        }

        string? Clean(Section s)
        {
            var v = buffers[s].ToString().Trim();
            return string.IsNullOrWhiteSpace(v) ? null : v;
        }

        return new RadOcrParseResult(text, Clean(Section.Findings), null,
            Clean(Section.Conclusion), Clean(Section.Recommendations));
    }

    private static List<Hit> FindHits(string norm)
    {
        var hits = new List<Hit>();
        var i = 0;
        while (i < norm.Length)
        {
            // KHONG yeu cau ranh gioi truoc nhan: engine doc PDF text-layer hay DAN LIEN tu ("thangMo ta")
            // nen nhan co the dinh sau chu cai. Guard chinh la rang buoc DAU ':' (hoac het dong) SAU nhan
            // — du manh de loai va cham tieu de. Nhan tieu de tren dong rieng (khong ':') chi tinh khi
            // BAT DAU o dau dong (tranh khop long giua cau tu do).
            Hit? matched = null;
            foreach (var (label, sec) in MarkersByLen)
            {
                if (i + label.Length > norm.Length) continue;
                if (string.CompareOrdinal(norm, i, label, 0, label.Length) != 0) continue;

                var k = i + label.Length;
                while (k < norm.Length && (norm[k] == ' ' || norm[k] == '\t')) k++;

                if (k < norm.Length && norm[k] == ':')
                {
                    // Co dau ':' -> content bat dau sau ':'
                    var c = k + 1;
                    while (c < norm.Length && (norm[c] == ' ' || norm[c] == '\t')) c++;
                    matched = new Hit(i, c, sec);
                }
                else if ((k >= norm.Length || norm[k] == '\n' || norm[k] == '\r')
                         && (i == 0 || norm[i - 1] == '\n' || norm[i - 1] == '\r'))
                {
                    // Nhan chiem tron dong (khong ':') va o dau dong -> content tu dong ke tiep
                    matched = new Hit(i, k, sec);
                }
                // Nguoc lai (theo sau la chu/so khac) -> khong phai nhan section (vd tieu de) -> bo qua

                if (matched != null) break;
            }

            if (matched != null)
            {
                hits.Add(matched);
                // Nhay qua het nhan (toi vi tri content) de tranh khop long nhau
                i = matched.ContentStart > matched.LabelStart ? matched.ContentStart : matched.LabelStart + 1;
                continue;
            }
            i++;
        }

        // Sap theo vi tri xuat hien
        hits.Sort((a, b) => a.LabelStart.CompareTo(b.LabelStart));
        return hits;
    }

    /// <summary>Bo dau tieng Viet, đ->d, lowercase. GIU nguyen do dai ky tu (1-1 map) de cat content
    /// tren text goc dung vi tri.</summary>
    private static string Normalize(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;

        var sb = new StringBuilder(input.Length);
        foreach (var ch in input)
        {
            var c = ch switch { 'đ' => 'd', 'Đ' => 'd', _ => ch };
            var decomposed = c.ToString().Normalize(NormalizationForm.FormD);
            var baseChar = decomposed[0]; // ky tu goc, bo dau ket hop phia sau
            sb.Append(char.ToLowerInvariant(baseChar));
        }
        return sb.ToString();
    }

    /// <summary>Don gian hoa content: gom khoang trang thua, bo dau ':' / '-' dau doan neu con sot.</summary>
    private static string CleanContent(string content)
    {
        var trimmed = content.TrimStart(' ', '\t', ':', '-', '.', ')');
        // Gom khoang trang lien tiep (khong dung newline de van giu xuong dong giua cac dong mo ta)
        return Regex.Replace(trimmed, @"[ \t]{2,}", " ").Trim();
    }
}
