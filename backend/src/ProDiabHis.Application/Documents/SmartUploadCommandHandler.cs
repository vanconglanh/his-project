using MediatR;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.InBody;
using ProDiabHis.Application.LabResults.Ocr;

namespace ProDiabHis.Application.Documents;

/// <summary>
/// Handler dieu phoi (orchestrator) — KHONG ghi DB truc tiep, chi goi lai
/// UploadInBodyReportCommand / ExtractLabResultOcrCommand da co san qua IMediator.
/// </summary>
public class SmartUploadCommandHandler : IRequestHandler<SmartUploadCommand, Result<SmartUploadResponse>>
{
    private const double ConfidenceThreshold = 0.6;
    private const int PreviewLength = 500;

    private readonly IMediator _mediator;
    private readonly ILabOcrTextProvider _ocr;
    private readonly IDocumentClassifier _classifier;

    public SmartUploadCommandHandler(IMediator mediator, ILabOcrTextProvider ocr, IDocumentClassifier classifier)
    {
        _mediator = mediator;
        _ocr = ocr;
        _classifier = classifier;
    }

    public async Task<Result<SmartUploadResponse>> Handle(SmartUploadCommand cmd, CancellationToken ct)
    {
        var textResult = await _ocr.ExtractTextAsync(cmd.FileBytes, cmd.FileName, cmd.ContentType, ct);
        if (!textResult.IsSuccess)
            return Result<SmartUploadResponse>.Failure("DOC_OCR_FAILED", "Không đọc được nội dung tài liệu");

        var text = textResult.Value ?? string.Empty;
        var classification = await _classifier.ClassifyAsync(new DocumentClassifyInput(text, cmd.EncounterId), ct);

        var preview = text.Length > PreviewLength ? text[..PreviewLength] : text;

        InBodyReportResponse? inBody = null;
        LabOcrExtractResponse? labResult = null;
        var requiresEncounter = false;

        if (classification.Type == DocumentType.InBody && classification.Confidence >= ConfidenceThreshold)
        {
            var inBodyResult = await _mediator.Send(
                new UploadInBodyReportCommand(cmd.PatientId, cmd.EncounterId, new MemoryStream(cmd.FileBytes),
                    cmd.FileName, cmd.ContentType), ct);
            if (inBodyResult.IsSuccess)
                inBody = inBodyResult.Value;
        }
        else if (classification.Type == DocumentType.LabResult && classification.Confidence >= ConfidenceThreshold)
        {
            if (cmd.EncounterId is null)
            {
                requiresEncounter = true;
            }
            else
            {
                var labOcrResult = await _mediator.Send(
                    new ExtractLabResultOcrCommand(cmd.EncounterId.Value, new MemoryStream(cmd.FileBytes),
                        cmd.FileName, cmd.ContentType), ct);
                if (labOcrResult.IsSuccess)
                    labResult = labOcrResult.Value;
            }
        }

        var response = new SmartUploadResponse(classification, requiresEncounter, preview, inBody, labResult);
        return Result<SmartUploadResponse>.Success(response);
    }
}
