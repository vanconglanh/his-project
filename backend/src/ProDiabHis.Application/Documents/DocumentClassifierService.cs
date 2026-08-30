using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace ProDiabHis.Application.Documents;

// ═══════════════════════════════════════════════════════════════════════════
// DocumentClassifierService — phan loai tai lieu da OCR thanh InBody / LabResult /
// Legacy / Unknown de dieu phoi sang dung luong xac nhan co san (khong ghi DB o day).
//
// Quy tac diem so:
//  - InBody: khop nhan dien theo tap nhan label dac thu may InBody. >=2 nhan -> 0.9,
//            1 nhan -> 0.6.
//  - LabResult: chi xet khi co EncounterId (moi biet pending list). Dem so ten/ma XN
//            dang cho khop trong text (word-boundary, tranh false positive voi ma <4 ky tu).
//            >=3 khop -> 0.9, 2 khop -> 0.75, 1 khop -> 0.55.
//  - Uu tien InBody khi ca 2 cung khop (nhan InBody rat dac thu, it trung voi ten XN).
//  - Khong khop gi -> Legacy, score 0.5 (fallback an toan, FE luon cho sua vi < nguong).
//  - CONFIDENCE_THRESHOLD = 0.6: neu diem cao nhat < 0.6 VA co >1 candidate canh tranh
//    (score > 0) -> Type = Unknown, van kem Candidates de FE cho chon tay.
// ═══════════════════════════════════════════════════════════════════════════
public class DocumentClassifierService : IDocumentClassifier
{
    private const double ConfidenceThreshold = 0.6;

    private static readonly string[] InBodyLabels =
    {
        "inbody score",
        "percent body fat",
        "pbf",
        "skeletal muscle mass",
        "smm",
        "body composition",
        "visceral fat",
        "segmental lean"
    };

    private readonly IPendingLabTestsProvider _pendingLabTestsProvider;

    public DocumentClassifierService(IPendingLabTestsProvider pendingLabTestsProvider)
    {
        _pendingLabTestsProvider = pendingLabTestsProvider;
    }

    public async Task<DocumentClassifyResult> ClassifyAsync(DocumentClassifyInput input, CancellationToken ct)
    {
        var normalized = Normalize(input.OcrText);

        var inBodyCandidate = ClassifyInBody(normalized);
        var labCandidate = await ClassifyLabResultAsync(normalized, input.EncounterId, ct);

        // Legacy fallback luon co mat voi score co dinh 0.5 — dam bao luon co it nhat 1 candidate.
        var legacyCandidate = new DocumentTypeCandidate(DocumentType.Legacy, 0.5, Array.Empty<string>());

        var candidates = new List<DocumentTypeCandidate> { inBodyCandidate, labCandidate, legacyCandidate }
            .Where(c => c.Score > 0)
            .OrderByDescending(c => c.Score)
            .ToList();

        if (candidates.Count == 0)
            candidates.Add(legacyCandidate);

        // Uu tien InBody khi ca InBody va Lab cung khop (nhan InBody rat dac thu).
        DocumentTypeCandidate best;
        if (inBodyCandidate.Score > 0 && labCandidate.Score > 0)
            best = inBodyCandidate;
        else
            best = candidates[0];

        var competing = candidates.Count(c => c.Score > 0);
        var type = best.Type;
        var confidence = best.Score;
        var evidence = best.Evidence;

        if (type != DocumentType.Legacy && confidence < ConfidenceThreshold && competing > 1)
        {
            type = DocumentType.Unknown;
            evidence = Array.Empty<string>();
        }

        return new DocumentClassifyResult(type, confidence, evidence, candidates);
    }

    private static DocumentTypeCandidate ClassifyInBody(string normalizedText)
    {
        var matched = InBodyLabels.Where(label => normalizedText.Contains(label)).ToList();
        double score = matched.Count switch
        {
            >= 2 => 0.9,
            1 => 0.6,
            _ => 0.0
        };
        return new DocumentTypeCandidate(DocumentType.InBody, score, matched);
    }

    private async Task<DocumentTypeCandidate> ClassifyLabResultAsync(
        string normalizedText, Guid? encounterId, CancellationToken ct)
    {
        if (encounterId is null)
            return new DocumentTypeCandidate(DocumentType.LabResult, 0.0, Array.Empty<string>());

        var pending = await _pendingLabTestsProvider.GetPendingAsync(encounterId.Value, ct);
        if (pending.Count == 0)
            return new DocumentTypeCandidate(DocumentType.LabResult, 0.0, Array.Empty<string>());

        var evidence = new List<string>();
        foreach (var test in pending)
        {
            var nameMatched = !string.IsNullOrWhiteSpace(test.TestName) && IsMatch(normalizedText, test.TestName);
            var codeMatched = !string.IsNullOrWhiteSpace(test.TestCode) && IsMatch(normalizedText, test.TestCode);
            if (nameMatched || codeMatched)
                evidence.Add(!string.IsNullOrWhiteSpace(test.TestName) ? test.TestName : test.TestCode);
        }

        double score = evidence.Count switch
        {
            >= 3 => 0.9,
            2 => 0.75,
            1 => 0.55,
            _ => 0.0
        };
        return new DocumentTypeCandidate(DocumentType.LabResult, score, evidence);
    }

    /// <summary>
    /// Khop token trong text da chuan hoa. Token <4 ky tu (vd ma XN ngan) bat buoc
    /// khop nguyen tu (word-boundary) de tranh false positive (vd "hb" khop trong
    /// "hb..." cua tu khac). Token dai hon cho phep khop substring cum tu.
    /// </summary>
    private static bool IsMatch(string normalizedText, string token)
    {
        var normalizedToken = Normalize(token);
        if (string.IsNullOrWhiteSpace(normalizedToken)) return false;

        if (normalizedToken.Length < 4)
        {
            var pattern = $@"(?<![a-z0-9]){Regex.Escape(normalizedToken)}(?![a-z0-9])";
            return Regex.IsMatch(normalizedText, pattern);
        }

        return normalizedText.Contains(normalizedToken);
    }

    /// <summary>Lower-case, bo dau tieng Viet, gop whitespace.</summary>
    private static string Normalize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        var lower = text.ToLowerInvariant();
        var decomposed = lower.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposed.Length);
        foreach (var ch in decomposed)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (category == UnicodeCategory.NonSpacingMark) continue;
            sb.Append(ch == 'đ' ? 'd' : ch);
        }
        var noDiacritics = sb.ToString().Normalize(NormalizationForm.FormC);
        return Regex.Replace(noDiacritics, @"\s+", " ").Trim();
    }
}
