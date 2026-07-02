using Hangfire;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Infrastructure.Jobs;

/// <summary>Hangfire implementation cua IBackgroundJobEnqueuer</summary>
public class HangfireBackgroundJobEnqueuer : IBackgroundJobEnqueuer
{
    private readonly IBackgroundJobClient _client;

    public HangfireBackgroundJobEnqueuer(IBackgroundJobClient client)
        => _client = client;

    public string EnqueueSendOutbound(string outboundId, int tenantId)
        => _client.Enqueue<SendOutboundJob>(j => j.Execute(outboundId, tenantId));

    public string EnqueueProcessInbound(string inboundId)
        => _client.Enqueue<ProcessInboundJob>(j => j.Execute(inboundId));

    public string EnqueueBhytGenerateXml(int exportId, int tenantId, string periodMonth, string? scopeFilterJson)
        => _client.Enqueue<BhytGenerateXmlJob>(j =>
            j.ExecuteAsync(exportId, tenantId, periodMonth, scopeFilterJson));

    public string EnqueueBhytReconcileParse(string uploadId, int exportId, int tenantId, string filePath)
        => _client.Enqueue<BhytReconcileParseJob>(j =>
            j.ExecuteAsync(uploadId, exportId, tenantId, filePath));
}
