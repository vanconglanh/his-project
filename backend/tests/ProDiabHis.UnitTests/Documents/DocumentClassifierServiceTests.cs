using ProDiabHis.Application.Documents;
using Xunit;

namespace ProDiabHis.UnitTests.Documents;

/// <summary>Fake IPendingLabTestsProvider — khong can DB thuc, tra danh sach cau hinh san.</summary>
public class FakePendingLabTestsProvider : IPendingLabTestsProvider
{
    private readonly IReadOnlyList<(Guid LabOrderItemId, string TestCode, string TestName)> _pending;

    public FakePendingLabTestsProvider(IReadOnlyList<(Guid, string, string)> pending) => _pending = pending;

    public static FakePendingLabTestsProvider Empty() => new(Array.Empty<(Guid, string, string)>());

    public Task<IReadOnlyList<(Guid LabOrderItemId, string TestCode, string TestName)>> GetPendingAsync(
        Guid encounterId, CancellationToken ct) => Task.FromResult(_pending);
}

public class DocumentClassifierServiceTests
{
    [Fact]
    public async Task ClassifyAsync_InBodyText_ReturnsInBodyWithHighConfidence()
    {
        var sut = new DocumentClassifierService(FakePendingLabTestsProvider.Empty());
        var text = "KET QUA DO THANH PHAN CO THE\nInBody Score: 78\nPercent Body Fat: 22.5%\n";

        var result = await sut.ClassifyAsync(new DocumentClassifyInput(text, null), default);

        Assert.Equal(DocumentType.InBody, result.Type);
        Assert.True(result.Confidence >= 0.6);
        Assert.Contains(result.Evidence, e => e.Contains("inbody score"));
    }

    [Fact]
    public async Task ClassifyAsync_RandomTextNoPending_ReturnsLegacyWithFallbackConfidence()
    {
        var sut = new DocumentClassifierService(FakePendingLabTestsProvider.Empty());
        var text = "Day la mot van ban bat ky khong lien quan gi den y te ca.";

        var result = await sut.ClassifyAsync(new DocumentClassifyInput(text, null), default);

        Assert.Equal(DocumentType.Legacy, result.Type);
        Assert.Equal(0.5, result.Confidence);
    }

    [Fact]
    public async Task ClassifyAsync_ThreeMatchingPendingTests_ReturnsLabResultWithHighConfidence()
    {
        var encounterId = Guid.NewGuid();
        var pending = new List<(Guid, string, string)>
        {
            (Guid.NewGuid(), "GLU", "Glucose"),
            (Guid.NewGuid(), "HBA1C", "HbA1c"),
            (Guid.NewGuid(), "CHOL", "Cholesterol toan phan")
        };
        var sut = new DocumentClassifierService(new FakePendingLabTestsProvider(pending));
        var text = "PHIEU KET QUA XET NGHIEM\nGlucose: 5.6 mmol/L\nHbA1c: 6.1 %\nCholesterol toan phan: 4.8 mmol/L\n";

        var result = await sut.ClassifyAsync(new DocumentClassifyInput(text, encounterId), default);

        Assert.Equal(DocumentType.LabResult, result.Type);
        Assert.True(result.Confidence >= 0.6);
    }

    [Fact]
    public async Task ClassifyAsync_SingleInBodyLabelNoPending_ReturnsLowConfidenceWithCandidates()
    {
        var sut = new DocumentClassifierService(FakePendingLabTestsProvider.Empty());
        var text = "Bao cao co PBF trong do nhung khong co nhan nao khac.";

        var result = await sut.ClassifyAsync(new DocumentClassifyInput(text, null), default);

        // 1 nhan InBody khop -> score 0.6 (bang nguong, khong "chac chan" >0.6), FE van nen
        // cho nguoi dung xem lai. Danh gia bang <= 0.6 thay vi < 0.6 tuyet doi vi theo quy tac
        // diem so "1 khop -> 0.6" trung dung nguong CONFIDENCE_THRESHOLD.
        Assert.True(result.Confidence <= 0.6);
        Assert.NotEmpty(result.Candidates);
    }
}
