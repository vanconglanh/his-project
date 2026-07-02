using Dapper;
using Microsoft.Extensions.Logging;
using ProDiabHis.Application.Billing;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Infrastructure.Jobs;

/// <summary>Retry hoa don dien tu that bai (status=DRAFT + retry_count < 3)</summary>
public class EInvoiceRetryJob
{
    private readonly IDapperConnectionFactory _db;
    private readonly IEnumerable<IEInvoiceProvider> _providers;
    private readonly ILogger<EInvoiceRetryJob> _logger;

    public EInvoiceRetryJob(
        IDapperConnectionFactory db,
        IEnumerable<IEInvoiceProvider> providers,
        ILogger<EInvoiceRetryJob> logger)
    {
        _db = db; _providers = providers; _logger = logger;
    }

    public async Task ExecuteAsync()
    {
        using var conn = _db.CreateConnection();
        var pending = await conn.QueryAsync<dynamic>(
            @"SELECT e.id, e.billing_id, e.provider, e.retry_count, b.patient_payable, b.vat_total
              FROM diab_his_bil_einvoices e
              JOIN diab_his_bil_billing b ON b.id = e.billing_id
              WHERE e.status = 'DRAFT' AND e.retry_count < 3
              LIMIT 50");

        foreach (var row in pending)
        {
            var provider = _providers.FirstOrDefault(p =>
                p.ProviderName.Equals((string)row.provider, StringComparison.OrdinalIgnoreCase));
            if (provider == null) continue;

            try
            {
                var result = await provider.IssueAsync(new EInvoiceIssueRequest(
                    Guid.Parse((string)row.billing_id),
                    (decimal)row.patient_payable,
                    (decimal)row.vat_total,
                    null, null, null, null, null, false));

                await conn.ExecuteAsync(
                    @"UPDATE diab_his_bil_einvoices
                      SET status = 'ISSUED', cqt_code = @cqt, invoice_no = @inv,
                          issue_date = NOW(), updated_at = NOW()
                      WHERE id = @id",
                    new { id = (string)row.id, cqt = result.CqtCode, inv = result.InvoiceNo });

                _logger.LogInformation("EInvoiceRetryJob: issued {Id}", (string)row.id);
            }
            catch (Exception ex)
            {
                await conn.ExecuteAsync(
                    @"UPDATE diab_his_bil_einvoices
                      SET retry_count = retry_count + 1, last_error = @err, updated_at = NOW()
                      WHERE id = @id",
                    new { id = (string)row.id, err = ex.Message });
                _logger.LogWarning(ex, "EInvoiceRetryJob: failed {Id}", (string)row.id);
            }
        }
    }
}
