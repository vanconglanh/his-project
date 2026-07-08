using Dapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProDiabHis.Application.Auth;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.Reports;
using ProDiabHis.Domain.Entities;

namespace ProDiabHis.Application.Billing;

// ── Requests / Responses ────────────────────────────────────────────────────

public record PrintBillingRequest(string CopyLabel = "BAN GOC", bool Reprint = false);

public record PrintBillingResult(
    byte[] PdfBytes,
    string InvoiceNo,
    string ArchivedUrl);

public record PrintReceiptRequest(bool Reprint = false);

public record PrintReceiptResult(
    byte[] PdfBytes,
    string ReceiptNo);

// ── Commands ────────────────────────────────────────────────────────────────

public record PrintBillingCommand(Guid BillingId, PrintBillingRequest Request)
    : IRequest<Result<PrintBillingResult>>;

public record PrintReceiptCommand(Guid PaymentId, PrintReceiptRequest Request)
    : IRequest<Result<PrintReceiptResult>>;

// ── Handler interfaces cho PDF services (tranh circular dep Api -> Application) ─

/// <summary>
/// Contract de Application goi PDF service ma khong phu thuoc QuestPDF truc tiep.
/// Implementation nam o ProDiabHis.Api/Services/InvoicePdfService.
/// </summary>
public interface IInvoicePdfGenerator
{
    Task<byte[]> GenerateAsync(BillingResponse billing, string copyLabel, bool reprint, CancellationToken ct, LetterheadDto? letterhead = null);
}

/// <summary>
/// Contract de Application goi Receipt PDF service.
/// Implementation nam o ProDiabHis.Api/Services/ReceiptPdfService.
/// </summary>
public interface IReceiptPdfGenerator
{
    Task<byte[]> GenerateAsync(ReceiptPrintData data, bool reprint, CancellationToken ct);
}

public record ReceiptPrintData(
    string ReceiptNo,
    string? TenantName,
    string? TenantAddress,
    string? TenantCskcbCode,
    string? PatientCode,
    string PatientName,
    string? Phone,
    DateTime PaidAt,
    string Method,
    decimal Amount,
    string? Reference,
    string? CashierName,
    List<ReceiptLineDto> Lines,
    LetterheadDto? Letterhead = null);

public record ReceiptLineDto(string Name, decimal Quantity, decimal UnitPrice, decimal LineTotal);

// ── PrintBillingHandler ──────────────────────────────────────────────────────

public class PrintBillingHandler : IRequestHandler<PrintBillingCommand, Result<PrintBillingResult>>
{
    private readonly IApplicationDbContext _db;
    private readonly IDapperConnectionFactory _dapper;
    private readonly ITenantProvider _tenant;
    private readonly IAuditService _audit;
    private readonly IInvoicePdfGenerator _pdfGen;
    private readonly IFileStorage _fileStorage;

    public PrintBillingHandler(
        IApplicationDbContext db,
        IDapperConnectionFactory dapper,
        ITenantProvider tenant,
        IAuditService audit,
        IInvoicePdfGenerator pdfGen,
        IFileStorage fileStorage)
    {
        _db = db;
        _dapper = dapper;
        _tenant = tenant;
        _audit = audit;
        _pdfGen = pdfGen;
        _fileStorage = fileStorage;
    }

    public async Task<Result<PrintBillingResult>> Handle(PrintBillingCommand cmd, CancellationToken ct)
    {
        var tenantId = _tenant.TenantId;

        var billing = await _db.Billings
            .Include(b => b.Items)
            .FirstOrDefaultAsync(b => b.Id == cmd.BillingId && b.TenantId == tenantId, ct);

        if (billing == null)
            return Result<PrintBillingResult>.Failure("BILLING_NOT_FOUND", "Khong tim thay hoa don.");

        if (billing.Status == BillingStatus.Draft)
            return Result<PrintBillingResult>.Failure("BILLING_NOT_FINALIZED", "Hoa don chua chot, khong the in ban chinh thuc.");

        // Lay thong tin benh nhan
        PatientSummaryDto? patient = null;
        using (var conn = _dapper.CreateConnection())
        {
            var row = await conn.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT full_name, dob, gender, phone FROM diab_his_pat_patients WHERE id = @id AND deleted_at IS NULL",
                new { id = billing.PatientId.ToString() });

            if (row != null)
            {
                patient = new PatientSummaryDto(
                    (string)row.full_name,
                    row.dob == null ? null : DateOnly.FromDateTime((DateTime)row.dob),
                    (string?)row.gender,
                    (string?)row.phone,
                    null);
            }
        }

        var dto = BillingMapper.ToDto(billing, patient);

        LetterheadDto? letterhead = null;
        using (var conn = _dapper.CreateConnection())
        {
            letterhead = await conn.QueryFirstOrDefaultAsync<LetterheadDto>(
                @"SELECT name AS ClinicName, cskcb_code AS CskcbCode, company_name AS CompanyName, address AS Address,
                         phone AS Phone, email AS Email, email_support AS EmailSupport, logo_url AS LogoUrl,
                         slogan AS Slogan, website AS Website
                  FROM diab_his_sys_tenants WHERE id = @tenantId", new { tenantId });
        }

        var pdfBytes = await _pdfGen.GenerateAsync(dto, cmd.Request.CopyLabel, cmd.Request.Reprint, ct, letterhead);

        // Archive vao MinIO (best-effort — khong throw neu MinIO chua san)
        var archivedUrl = string.Empty;
        try
        {
            var now = DateTime.UtcNow;
            var objectKey = $"invoices/{tenantId}/{now:yyyy}/{now:MM}/{billing.BillNo ?? billing.Id.ToString()}.pdf";
            await _fileStorage.UploadAsync(
                "prodiab-invoices",
                objectKey,
                new MemoryStream(pdfBytes),
                "application/pdf",
                ct);
            archivedUrl = $"minio://prodiab-invoices/{objectKey}";
        }
        catch
        {
            // MinIO chua san trong dev — bo qua, khong fail request
        }

        // Audit log
        await _audit.LogAsync(
            "billing.print",
            "Billing",
            cmd.BillingId.ToString(),
            details: new { reprint = cmd.Request.Reprint, copy_label = cmd.Request.CopyLabel },
            cancellationToken: ct);

        return Result<PrintBillingResult>.Success(
            new PrintBillingResult(pdfBytes, billing.BillNo ?? billing.Id.ToString(), archivedUrl));
    }
}

// ── PrintReceiptHandler ──────────────────────────────────────────────────────

public class PrintReceiptHandler : IRequestHandler<PrintReceiptCommand, Result<PrintReceiptResult>>
{
    private readonly IApplicationDbContext _db;
    private readonly IDapperConnectionFactory _dapper;
    private readonly ITenantProvider _tenant;
    private readonly IAuditService _audit;
    private readonly IReceiptPdfGenerator _pdfGen;

    public PrintReceiptHandler(
        IApplicationDbContext db,
        IDapperConnectionFactory dapper,
        ITenantProvider tenant,
        IAuditService audit,
        IReceiptPdfGenerator pdfGen)
    {
        _db = db;
        _dapper = dapper;
        _tenant = tenant;
        _audit = audit;
        _pdfGen = pdfGen;
    }

    public async Task<Result<PrintReceiptResult>> Handle(PrintReceiptCommand cmd, CancellationToken ct)
    {
        var tenantId = _tenant.TenantId;

        var payment = await _db.Payments
            .FirstOrDefaultAsync(p => p.Id == cmd.PaymentId && p.TenantId == tenantId, ct);

        if (payment == null)
            return Result<PrintReceiptResult>.Failure("PAYMENT_NOT_FOUND", "Khong tim thay giao dich.");

        if (payment.Status == PaymentStatus.Void)
            return Result<PrintReceiptResult>.Failure("PAYMENT_VOIDED", "Giao dich da huy, khong the in bien lai.");

        // Generate so bien lai
        var receiptNo = $"BL-{payment.PaidAt?.ToString("yyMMdd") ?? DateTime.UtcNow.ToString("yyMMdd")}-{payment.Id.ToString("N")[..6].ToUpper()}";

        // Lay thong tin billing + benh nhan
        string? patientName = "-";
        string? patientCode = null;
        string? phone = null;
        string? tenantName = null;
        string? tenantAddress = null;
        string? tenantCskcbCode = null;
        string? cashierName = null;
        LetterheadDto? letterhead = null;
        var lines = new List<ReceiptLineDto>();

        using (var conn = _dapper.CreateConnection())
        {
            var billing = await conn.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT b.bill_no, b.payer FROM diab_his_bil_billing b WHERE b.id = @id AND b.tenant_id = @tenantId AND b.deleted_at IS NULL",
                new { id = payment.BillingId.ToString(), tenantId });

            if (billing != null)
            {
                // Luu y: bang diab_his_bil_billing_items KHONG co cot deleted_at (xac nhan qua
                // db/migrations/0041_billing_extensions.sql va 9008_seed_demo.sql) — dong hang duoc
                // xoa cung hoa don qua FK ON DELETE CASCADE, khong soft-delete rieng le.
                var items = await conn.QueryAsync<dynamic>(
                    "SELECT name, quantity, unit_price, line_total FROM diab_his_bil_billing_items WHERE billing_id = @id",
                    new { id = payment.BillingId.ToString() });

                foreach (var it in items)
                {
                    lines.Add(new ReceiptLineDto(
                        (string)it.name,
                        (decimal)it.quantity,
                        (decimal)it.unit_price,
                        (decimal)it.line_total));
                }
            }

            // Benh nhan
            var patRow = await conn.QueryFirstOrDefaultAsync<dynamic>(
                @"SELECT p.full_name, p.phone, p.code
                  FROM diab_his_pat_patients p
                  JOIN diab_his_bil_billing b ON b.patient_id = p.id
                  WHERE b.id = @billingId AND b.tenant_id = @tenantId AND p.deleted_at IS NULL LIMIT 1",
                new { billingId = payment.BillingId.ToString(), tenantId });

            if (patRow != null)
            {
                patientName = (string?)patRow.full_name ?? "-";
                patientCode = (string?)patRow.code;
                phone = (string?)patRow.phone;
            }

            // Tenant info
            var tenantRow = await conn.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT name, address, cskcb_code FROM diab_his_sys_tenants WHERE id = @tenantId AND deleted_at IS NULL",
                new { tenantId });

            if (tenantRow != null)
            {
                tenantName = (string?)tenantRow.name;
                tenantAddress = (string?)tenantRow.address;
                tenantCskcbCode = (string?)tenantRow.cskcb_code;
            }

            letterhead = await conn.QueryFirstOrDefaultAsync<LetterheadDto>(
                @"SELECT name AS ClinicName, cskcb_code AS CskcbCode, company_name AS CompanyName, address AS Address,
                         phone AS Phone, email AS Email, email_support AS EmailSupport, logo_url AS LogoUrl,
                         slogan AS Slogan, website AS Website
                  FROM diab_his_sys_tenants WHERE id = @tenantId", new { tenantId });

            // Cashier
            if (payment.PaidBy.HasValue)
            {
                var cashierRow = await conn.QueryFirstOrDefaultAsync<dynamic>(
                    "SELECT full_name FROM diab_his_sec_users WHERE id = @id AND deleted_at IS NULL",
                    new { id = payment.PaidBy.Value.ToString() });
                cashierName = (string?)cashierRow?.full_name;
            }
        }

        var data = new ReceiptPrintData(
            receiptNo,
            tenantName,
            tenantAddress,
            tenantCskcbCode,
            patientCode,
            patientName,
            phone,
            payment.PaidAt ?? DateTime.UtcNow,
            payment.Method,
            payment.Amount,
            payment.Reference,
            cashierName,
            lines,
            letterhead);

        var pdfBytes = await _pdfGen.GenerateAsync(data, cmd.Request.Reprint, ct);

        await _audit.LogAsync(
            "cashier.receipt.print",
            "Payment",
            cmd.PaymentId.ToString(),
            details: new { reprint = cmd.Request.Reprint },
            cancellationToken: ct);

        return Result<PrintReceiptResult>.Success(new PrintReceiptResult(pdfBytes, receiptNo));
    }
}
