using MediatR;
using NSubstitute;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.Documents;
using Xunit;

namespace ProDiabHis.UnitTests.Documents;

/// <summary>
/// Kiem tra RIENG lop batch (SmartUploadBatchCommandHandler): moi file duoc goi lai SmartUploadCommand
/// DOC LAP, ket qua gom theo tung file (khong gop), 1 file loi khong lam hong cac file khac, va cap
/// so luong. Dung NSubstitute cho IMediator — khong can DB/OCR that.
/// </summary>
public class SmartUploadBatchCommandHandlerTests
{
    private static SmartUploadResponse Resp(DocumentType type, double confidence)
    {
        var cls = new DocumentClassifyResult(type, confidence, new[] { "evi" },
            new[] { new DocumentTypeCandidate(type, confidence, new[] { "evi" }) });
        return new SmartUploadResponse(cls, false, "preview", null, null, null);
    }

    private static SmartUploadFileInput File(string name) =>
        new(new byte[] { 1, 2, 3 }, name, "application/pdf");

    [Fact]
    public async Task MultipleFiles_EachRoutedIndependently_ResultsNotMerged()
    {
        var mediator = Substitute.For<IMediator>();
        // Moi file phan loai KHAC nhau — chung minh ket qua rieng tung file, khong lan lon.
        mediator.Send(Arg.Is<SmartUploadCommand>(c => c.FileName == "inbody.pdf"), Arg.Any<CancellationToken>())
            .Returns(Result<SmartUploadResponse>.Success(Resp(DocumentType.InBody, 0.9)));
        mediator.Send(Arg.Is<SmartUploadCommand>(c => c.FileName == "lab.pdf"), Arg.Any<CancellationToken>())
            .Returns(Result<SmartUploadResponse>.Success(Resp(DocumentType.LabResult, 0.75)));
        mediator.Send(Arg.Is<SmartUploadCommand>(c => c.FileName == "unknown.pdf"), Arg.Any<CancellationToken>())
            .Returns(Result<SmartUploadResponse>.Success(Resp(DocumentType.Legacy, 0.5)));

        var handler = new SmartUploadBatchCommandHandler(mediator);
        var cmd = new SmartUploadBatchCommand(Guid.NewGuid(), null,
            new[] { File("inbody.pdf"), File("lab.pdf"), File("unknown.pdf") });

        var result = await handler.Handle(cmd, default);

        Assert.True(result.IsSuccess);
        var items = result.Value!.Items;
        Assert.Equal(3, items.Count);
        Assert.All(items, i => Assert.True(i.Success));
        Assert.Equal(DocumentType.InBody, items[0].Result!.Classification.Type);
        Assert.Equal(DocumentType.LabResult, items[1].Result!.Classification.Type);
        Assert.Equal(DocumentType.Legacy, items[2].Result!.Classification.Type);
        await mediator.Received(3).Send(Arg.Any<SmartUploadCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OneFileOcrFails_OthersStillProcessed()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Is<SmartUploadCommand>(c => c.FileName == "ok.pdf"), Arg.Any<CancellationToken>())
            .Returns(Result<SmartUploadResponse>.Success(Resp(DocumentType.InBody, 0.9)));
        mediator.Send(Arg.Is<SmartUploadCommand>(c => c.FileName == "bad.pdf"), Arg.Any<CancellationToken>())
            .Returns(Result<SmartUploadResponse>.Failure("DOC_OCR_FAILED", "Không đọc được nội dung tài liệu"));

        var handler = new SmartUploadBatchCommandHandler(mediator);
        var cmd = new SmartUploadBatchCommand(Guid.NewGuid(), null,
            new[] { File("ok.pdf"), File("bad.pdf") });

        var result = await handler.Handle(cmd, default);

        Assert.True(result.IsSuccess);
        var items = result.Value!.Items;
        Assert.Equal(2, items.Count);
        Assert.True(items[0].Success);
        Assert.NotNull(items[0].Result);
        Assert.False(items[1].Success);
        Assert.Equal("DOC_OCR_FAILED", items[1].ErrorCode);
        Assert.Null(items[1].Result);
    }

    [Fact]
    public async Task TooManyFiles_ReturnsDocTooManyFiles()
    {
        var mediator = Substitute.For<IMediator>();
        var handler = new SmartUploadBatchCommandHandler(mediator);
        var files = Enumerable.Range(0, 21).Select(i => File($"f{i}.pdf")).ToArray();

        var result = await handler.Handle(new SmartUploadBatchCommand(Guid.NewGuid(), null, files), default);

        Assert.False(result.IsSuccess);
        Assert.Equal("DOC_TOO_MANY_FILES", result.ErrorCode);
        await mediator.DidNotReceive().Send(Arg.Any<SmartUploadCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EmptyFiles_ReturnsDocUploadFailed()
    {
        var mediator = Substitute.For<IMediator>();
        var handler = new SmartUploadBatchCommandHandler(mediator);

        var result = await handler.Handle(
            new SmartUploadBatchCommand(Guid.NewGuid(), null, Array.Empty<SmartUploadFileInput>()), default);

        Assert.False(result.IsSuccess);
        Assert.Equal("DOC_UPLOAD_FAILED", result.ErrorCode);
    }
}
