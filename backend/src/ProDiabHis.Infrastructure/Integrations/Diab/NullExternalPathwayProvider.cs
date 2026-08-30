using ProDiabHis.Application.Common.Interfaces;

namespace ProDiabHis.Infrastructure.Integrations.Diab;

/// <summary>
/// §4.7.3 — Provider mac dinh khi diaB CHUA co endpoint / tenant chua cau hinh tich hop.
/// LUON tra Status=NotConfigured, KHONG goi mang, KHONG nem loi.
/// Khi diaB co API that -> thay bang DiabPathwayProvider (HttpClient + Polly circuit breaker + cache Redis),
/// chi doi dang ky DI, khong doi tang Application/UI.
/// </summary>
public sealed class NullExternalPathwayProvider : IExternalPathwayProvider
{
    public Task<ExternalPathwayResult> GetPathwayAsync(ExternalPathwayQuery query, CancellationToken ct)
        => Task.FromResult(ExternalPathwayResult.NotConfiguredResult());
}
