using Dapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProDiabHis.Application.Auth;
using ProDiabHis.Application.Common;
using ProDiabHis.Domain.Entities;

namespace ProDiabHis.Application.Billing;

public record IssueEInvoiceCommand(IssueEInvoiceRequest Request) : IRequest<Result<EInvoiceResponse>>;
public record GetEInvoiceQuery(Guid Id) : IRequest<Result<EInvoiceResponse>>;
public record ListEInvoicesQuery(
    string? Status, string? Provider, Guid? BillingId,
    DateOnly? FromDate, DateOnly? ToDate, int Page, int PageSize)
    : IRequest<Result<PagedResult<EInvoiceResponse>>>;
public record CancelEInvoiceCommand(Guid Id, CancelEInvoiceRequest Request) : IRequest<Result>;
public record DownloadEInvoiceXmlQuery(Guid Id) : IRequest<Result<byte[]>>;

internal static class EInvoiceMapper
{
    public static EInvoiceResponse ToDto(EInvoice e) => new(
        e.Id, e.TenantId, e.BillingId, e.Provider, e.InvoiceNo,
        e.InvoiceSeries, e.CqtCode, e.IssueDate, e.TotalAmount,
        e.VatAmount, e.Status, e.PdfUrl, e.XmlUrl, e.SignedAt,
        e.CancelReason, e.CancelledAt, e.CreatedAt, e.CreatedBy);
}

public class IssueEInvoiceHandler : IRequestHandler<IssueEInvoiceCommand, Result<EInvoiceResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _user;
    private readonly IEnumerable<IEInvoiceProvider> _providers;

    public IssueEInvoiceHandler(
        IApplicationDbContext db, ITenantProvider tenant,
        ICurrentUser user, IEnumerable<IEInvoiceProvider> providers)
    {
        _db = db; _tenant = tenant; _user = user; _providers = providers;
    }

    public async Task<Result<EInvoiceResponse>> Handle(IssueEInvoiceCommand cmd, CancellationToken ct)
    {
        var req = cmd.Request;

        // Check already issued
        var already = await _db.EInvoices
            .AnyAsync(e => e.BillingId == req.BillingId
                && e.Status == "ISSUED" && e.TenantId == _tenant.TenantId, ct);
        if (already) return Result<EInvoiceResponse>.Failure("EINVOICE_ALREADY_ISSUED", "Da phat hanh hoa don dien tu");

        var billing = await _db.Billings.Include(b => b.Items)
            .FirstOrDefaultAsync(b => b.Id == req.BillingId && b.TenantId == _tenant.TenantId, ct);
        if (billing == null) return Result<EInvoiceResponse>.Failure("BILLING_NOT_FOUND", "Khong tim thay hoa don");

        var provider = _providers.FirstOrDefault(p =>
            p.ProviderName.Equals(req.Provider, StringComparison.OrdinalIgnoreCase));
        if (provider == null) return Result<EInvoiceResponse>.Failure("PROVIDER_NOT_FOUND", "Provider khong tim thay");

        var issueReq = new EInvoiceIssueRequest(
            billing.Id, billing.PatientPayable, billing.VatTotal,
            req.Buyer?.Name, req.Buyer?.TaxCode, req.Buyer?.Address,
            req.Buyer?.Email, req.Buyer?.Phone, req.SendEmail);

        EInvoiceIssueResult issueResult;
        try
        {
            issueResult = await provider.IssueAsync(issueReq, ct);
        }
        catch (Exception ex)
        {
            return Result<EInvoiceResponse>.Failure("EINVOICE_PROVIDER_ERROR", ex.Message);
        }

        var einvoice = new EInvoice
        {
            TenantId = _tenant.TenantId,
            BillingId = billing.Id,
            Provider = req.Provider,
            InvoiceNo = issueResult.InvoiceNo,
            InvoiceSeries = issueResult.InvoiceSeries,
            CqtCode = issueResult.CqtCode,
            IssueDate = DateTime.UtcNow,
            TotalAmount = billing.PatientPayable,
            VatAmount = billing.VatTotal,
            Status = "ISSUED",
            PdfUrl = issueResult.PdfUrl,
            XmlUrl = issueResult.XmlUrl,
            SignedAt = DateTime.UtcNow,
            CreatedBy = _user.UserId
        };
        _db.EInvoices.Add(einvoice);
        await _db.SaveChangesAsync(ct);
        return Result<EInvoiceResponse>.Success(EInvoiceMapper.ToDto(einvoice));
    }
}

public class GetEInvoiceHandler : IRequestHandler<GetEInvoiceQuery, Result<EInvoiceResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantProvider _tenant;

    public GetEInvoiceHandler(IApplicationDbContext db, ITenantProvider tenant)
    {
        _db = db; _tenant = tenant;
    }

    public async Task<Result<EInvoiceResponse>> Handle(GetEInvoiceQuery query, CancellationToken ct)
    {
        var e = await _db.EInvoices.FirstOrDefaultAsync(
            x => x.Id == query.Id && x.TenantId == _tenant.TenantId, ct);
        if (e == null) return Result<EInvoiceResponse>.Failure("EINVOICE_NOT_FOUND", "Khong tim thay HDDT");
        return Result<EInvoiceResponse>.Success(EInvoiceMapper.ToDto(e));
    }
}

public class ListEInvoicesHandler : IRequestHandler<ListEInvoicesQuery, Result<PagedResult<EInvoiceResponse>>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;

    public ListEInvoicesHandler(IDapperConnectionFactory db, ITenantProvider tenant)
    {
        _db = db; _tenant = tenant;
    }

    public async Task<Result<PagedResult<EInvoiceResponse>>> Handle(ListEInvoicesQuery query, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var where = "WHERE tenant_id = @tenantId";
        var p = new DynamicParameters();
        p.Add("tenantId", _tenant.TenantId);

        if (!string.IsNullOrEmpty(query.Status)) { where += " AND status = @status"; p.Add("status", query.Status); }
        if (!string.IsNullOrEmpty(query.Provider)) { where += " AND provider = @provider"; p.Add("provider", query.Provider); }
        if (query.BillingId.HasValue) { where += " AND billing_id = @bid"; p.Add("bid", query.BillingId.ToString()); }
        if (query.FromDate.HasValue) { where += " AND DATE(created_at) >= @from"; p.Add("from", query.FromDate.Value); }
        if (query.ToDate.HasValue) { where += " AND DATE(created_at) <= @to"; p.Add("to", query.ToDate.Value); }

        var total = await conn.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM diab_his_bil_einvoices {where}", p);
        var offset = (query.Page - 1) * query.PageSize;
        p.Add("limit", query.PageSize); p.Add("offset", offset);

        var rows = await conn.QueryAsync<dynamic>(
            $"SELECT * FROM diab_his_bil_einvoices {where} ORDER BY created_at DESC LIMIT @limit OFFSET @offset", p);

        var items = rows.Select(r => new EInvoiceResponse(
            Guid.Parse((string)r.id), (int)r.tenant_id,
            Guid.Parse((string)r.billing_id), (string)r.provider,
            (string?)r.invoice_no, (string?)r.invoice_series,
            (string?)r.cqt_code,
            r.issue_date == null ? null : (DateTime?)r.issue_date,
            (decimal)r.total_amount, (decimal)r.vat_amount, (string)r.status,
            (string?)r.pdf_url, (string?)r.xml_url,
            r.signed_at == null ? null : (DateTime?)r.signed_at,
            (string?)r.cancel_reason,
            r.cancelled_at == null ? null : (DateTime?)r.cancelled_at,
            (DateTime)r.created_at,
            r.created_by == null ? null : Guid.Parse((string)r.created_by))).ToList();

        return Result<PagedResult<EInvoiceResponse>>.Success(
            new PagedResult<EInvoiceResponse>(items, query.Page, query.PageSize, total));
    }
}

public class CancelEInvoiceHandler : IRequestHandler<CancelEInvoiceCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantProvider _tenant;
    private readonly IEnumerable<IEInvoiceProvider> _providers;

    public CancelEInvoiceHandler(IApplicationDbContext db, ITenantProvider tenant, IEnumerable<IEInvoiceProvider> providers)
    {
        _db = db; _tenant = tenant; _providers = providers;
    }

    public async Task<Result> Handle(CancelEInvoiceCommand cmd, CancellationToken ct)
    {
        var e = await _db.EInvoices
            .FirstOrDefaultAsync(x => x.Id == cmd.Id && x.TenantId == _tenant.TenantId, ct);
        if (e == null) return Result.Failure("EINVOICE_NOT_FOUND", "Khong tim thay HDDT");

        var provider = _providers.FirstOrDefault(p =>
            p.ProviderName.Equals(e.Provider, StringComparison.OrdinalIgnoreCase));
        if (provider != null && e.InvoiceNo != null)
            await provider.CancelAsync(e.InvoiceNo, cmd.Request.Reason, ct);

        e.Status = "CANCELLED";
        e.CancelReason = cmd.Request.Reason;
        e.CancelledAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public class DownloadEInvoiceXmlHandler : IRequestHandler<DownloadEInvoiceXmlQuery, Result<byte[]>>
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantProvider _tenant;
    private readonly IEnumerable<IEInvoiceProvider> _providers;

    public DownloadEInvoiceXmlHandler(IApplicationDbContext db, ITenantProvider tenant, IEnumerable<IEInvoiceProvider> providers)
    {
        _db = db; _tenant = tenant; _providers = providers;
    }

    public async Task<Result<byte[]>> Handle(DownloadEInvoiceXmlQuery query, CancellationToken ct)
    {
        var e = await _db.EInvoices
            .FirstOrDefaultAsync(x => x.Id == query.Id && x.TenantId == _tenant.TenantId, ct);
        if (e == null) return Result<byte[]>.Failure("EINVOICE_NOT_FOUND", "Khong tim thay HDDT");

        var provider = _providers.FirstOrDefault(p =>
            p.ProviderName.Equals(e.Provider, StringComparison.OrdinalIgnoreCase));
        if (provider == null || e.InvoiceNo == null)
            return Result<byte[]>.Failure("PROVIDER_ERROR", "Provider khong ho tro XML download");

        var xml = await provider.GetXmlAsync(e.InvoiceNo, ct);
        return Result<byte[]>.Success(System.Text.Encoding.UTF8.GetBytes(xml));
    }
}
