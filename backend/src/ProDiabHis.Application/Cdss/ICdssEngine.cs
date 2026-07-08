namespace ProDiabHis.Application.Cdss;

/// <summary>
/// CDSS engine: danh gia canh bao lam sang khi ke don (DDI, drug-allergy,
/// trung hoat chat, drug-lab, critical lab). context = CHECK (realtime khi
/// bac si dang soan don) hoac SIGN (chot ky don, co the chan neu interruptive).
/// </summary>
public interface ICdssEngine
{
    Task<CdssCheckResponse> EvaluateAsync(
        CdssEvaluationContext ctx, string context, bool logEvents, CancellationToken ct = default);
}
