using MediatR;
using NSubstitute;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.Documents;
using ProDiabHis.Application.InBody;
using ProDiabHis.Application.LabResults.Ocr;
using Xunit;

namespace ProDiabHis.UnitTests.Documents;

/// <summary>
/// Kiem tra RIENG logic dieu phoi (routing) cua SmartUploadCommandHandler — dung NSubstitute cho
/// IMediator/IDocumentClassifier/ILabOcrTextProvider de kiem soat ket qua phan loai, khong can DB/OCR that.
/// Muc tieu: dam bao nhan dien loai nao thi goi DUNG command cua luong do (khong duplicate logic).
/// </summary>
public class SmartUploadCommandHandlerTests
{
    private static DocumentClassifyResult Cls(DocumentType type, double confidence) =>
        new(type, confidence, new[] { "evi" }, new[] { new DocumentTypeCandidate(type, confidence, new[] { "evi" }) });

    private static SmartUploadCommand Cmd(Guid? encounterId = null) =>
        new(Guid.NewGuid(), encounterId, new byte[] { 1, 2, 3 }, "f.pdf", "application/pdf");

    private static (SmartUploadCommandHandler Handler, IMediator Mediator, IDocumentClassifier Classifier) Build()
    {
        var mediator = Substitute.For<IMediator>();
        var ocr = Substitute.For<ILabOcrTextProvider>();
        ocr.ExtractTextAsync(Arg.Any<byte[]>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<string>.Success("noi dung da ocr"));
        var classifier = Substitute.For<IDocumentClassifier>();
        return (new SmartUploadCommandHandler(mediator, ocr, classifier), mediator, classifier);
    }

    [Fact]
    public async Task InBodyConfident_RoutesToUploadInBodyReportCommand()
    {
        var (handler, mediator, classifier) = Build();
        classifier.ClassifyAsync(Arg.Any<DocumentClassifyInput>(), Arg.Any<CancellationToken>())
            .Returns(Cls(DocumentType.InBody, 0.9));
        var inBodyResp = new InBodyReportResponse(Guid.NewGuid(), Guid.NewGuid(), null, "PENDING", null,
            Array.Empty<InBodyFieldDto>(), null, null, DateTime.UtcNow);
        mediator.Send(Arg.Any<UploadInBodyReportCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<InBodyReportResponse>.Success(inBodyResp));

        var result = await handler.Handle(Cmd(), default);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value!.InBody);
        Assert.Null(result.Value.LabResult);
        Assert.False(result.Value.RequiresEncounter);
        await mediator.Received(1).Send(Arg.Any<UploadInBodyReportCommand>(), Arg.Any<CancellationToken>());
        await mediator.DidNotReceive().Send(Arg.Any<ExtractLabResultOcrCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LabResultConfident_WithoutEncounter_SetsRequiresEncounter_NoDownstreamCall()
    {
        var (handler, mediator, classifier) = Build();
        classifier.ClassifyAsync(Arg.Any<DocumentClassifyInput>(), Arg.Any<CancellationToken>())
            .Returns(Cls(DocumentType.LabResult, 0.9));

        var result = await handler.Handle(Cmd(encounterId: null), default);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.RequiresEncounter);
        Assert.Null(result.Value.LabResult);
        await mediator.DidNotReceive().Send(Arg.Any<ExtractLabResultOcrCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LabResultConfident_WithEncounter_RoutesToExtractLabResultOcrCommand()
    {
        var (handler, mediator, classifier) = Build();
        classifier.ClassifyAsync(Arg.Any<DocumentClassifyInput>(), Arg.Any<CancellationToken>())
            .Returns(Cls(DocumentType.LabResult, 0.9));
        var labResp = new LabOcrExtractResponse(Guid.NewGuid(), 2, 1, Array.Empty<LabOcrExtractFieldDto>());
        mediator.Send(Arg.Any<ExtractLabResultOcrCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<LabOcrExtractResponse>.Success(labResp));

        var result = await handler.Handle(Cmd(encounterId: Guid.NewGuid()), default);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value!.LabResult);
        Assert.False(result.Value.RequiresEncounter);
        await mediator.Received(1).Send(Arg.Any<ExtractLabResultOcrCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Legacy_NoDownstreamRouting_ReturnsClassificationOnly()
    {
        var (handler, mediator, classifier) = Build();
        classifier.ClassifyAsync(Arg.Any<DocumentClassifyInput>(), Arg.Any<CancellationToken>())
            .Returns(Cls(DocumentType.Legacy, 0.5));

        var result = await handler.Handle(Cmd(), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(DocumentType.Legacy, result.Value!.Classification.Type);
        Assert.Null(result.Value.InBody);
        Assert.Null(result.Value.LabResult);
        await mediator.DidNotReceive().Send(Arg.Any<UploadInBodyReportCommand>(), Arg.Any<CancellationToken>());
        await mediator.DidNotReceive().Send(Arg.Any<ExtractLabResultOcrCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OcrFails_ReturnsDocOcrFailed()
    {
        var mediator = Substitute.For<IMediator>();
        var ocr = Substitute.For<ILabOcrTextProvider>();
        ocr.ExtractTextAsync(Arg.Any<byte[]>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<string>.Failure("X", "loi"));
        var classifier = Substitute.For<IDocumentClassifier>();
        var handler = new SmartUploadCommandHandler(mediator, ocr, classifier);

        var result = await handler.Handle(Cmd(), default);

        Assert.False(result.IsSuccess);
        Assert.Equal("DOC_OCR_FAILED", result.ErrorCode);
    }
}
