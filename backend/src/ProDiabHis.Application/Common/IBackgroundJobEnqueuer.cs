namespace ProDiabHis.Application.Common;

/// <summary>
/// Abstraction de enqueue background job ma khong phu thuoc Hangfire.
/// Infrastructure inject Hangfire implementation.
/// </summary>
public interface IBackgroundJobEnqueuer
{
    /// <summary>Enqueue SendOutbound job voi outboundId va tenantId</summary>
    string EnqueueSendOutbound(string outboundId, int tenantId);

    /// <summary>Enqueue ProcessInbound job voi inboundId</summary>
    string EnqueueProcessInbound(string inboundId);

    /// <summary>Enqueue BhytGenerateXml job (long-running)</summary>
    string EnqueueBhytGenerateXml(int exportId, int tenantId, string periodMonth, string? scopeFilterJson);

    /// <summary>Enqueue BhytReconcileParse job</summary>
    string EnqueueBhytReconcileParse(string uploadId, int exportId, int tenantId, string filePath);
}
