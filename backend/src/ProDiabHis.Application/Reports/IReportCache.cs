namespace ProDiabHis.Application.Reports;

public interface IReportCache
{
    Task<string?> GetAsync(string tableName, int tenantId, string periodKey, CancellationToken ct = default);
    Task SetAsync(string tableName, int tenantId, string periodKey, string dataJson, CancellationToken ct = default);
}
