using Dapper;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProDiabHis.Application.Auth;
using ProDiabHis.Application.Common;
using ProDiabHis.Domain.Entities;

namespace ProDiabHis.Application.Billing;

// ---- Commands + Queries ----

public record CreatePaymentCommand(CreatePaymentRequest Request) : IRequest<Result<PaymentResponse>>;
public record GetPaymentQuery(Guid Id) : IRequest<Result<PaymentResponse>>;
public record ListPaymentsQuery(
    Guid? BillingId, string? Method, string? Status,
    DateOnly? FromDate, DateOnly? ToDate, int Page, int PageSize)
    : IRequest<Result<PagedResult<PaymentResponse>>>;
public record RefundPaymentCommand(Guid PaymentId, RefundPaymentRequest Request) : IRequest<Result<PaymentResponse>>;
public record VoidPaymentCommand(Guid PaymentId, VoidPaymentRequest Request) : IRequest<Result<PaymentResponse>>;
public record ListPaymentMethodsQuery : IRequest<Result<List<PaymentMethodDto>>>;
public record GenerateQrCommand(QrGenerateApiRequest Request) : IRequest<Result<QrCodeResponseDto>>;
public record GetQrStatusQuery(Guid QrId) : IRequest<Result<QrCodeResponseDto>>;
public record ProcessQrWebhookCommand(string Provider, string Payload, string Signature)
    : IRequest<Result>;
public record CardChargeCommand(CardChargeApiRequest Request) : IRequest<Result<PaymentResponse>>;

// ---- Validators ----

public class CreatePaymentValidator : AbstractValidator<CreatePaymentRequest>
{
    public CreatePaymentValidator()
    {
        RuleFor(x => x.BillingId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Method).NotEmpty();
    }
}

// ---- Mapping ----

internal static class PaymentMapper
{
    public static PaymentResponse ToDto(Payment p) => new(
        p.Id, p.TenantId, p.BillingId, p.Amount, p.Method,
        p.Reference, p.Status, p.Provider, p.ProviderTxnId,
        p.PaidAt, p.PaidBy, p.CashierShiftId, p.Note,
        p.RefundedAmount, p.CreatedAt);

    public static QrCodeResponseDto ToQrDto(QrCode q) => new(
        q.Id, q.BillingId, q.Provider, q.QrPayload, q.QrUrl,
        q.Amount, q.ExpiresAt, q.PaidAt, q.Status, q.TransactionRef);
}

// ---- Handlers ----

public class CreatePaymentHandler : IRequestHandler<CreatePaymentCommand, Result<PaymentResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _user;
    private readonly ICashierShiftService _shiftSvc;

    public CreatePaymentHandler(
        IApplicationDbContext db, ITenantProvider tenant,
        ICurrentUser user, ICashierShiftService shiftSvc)
    {
        _db = db; _tenant = tenant; _user = user; _shiftSvc = shiftSvc;
    }

    public async Task<Result<PaymentResponse>> Handle(CreatePaymentCommand cmd, CancellationToken ct)
    {
        var req = cmd.Request;
        var billing = await _db.Billings
            .FirstOrDefaultAsync(b => b.Id == req.BillingId && b.TenantId == _tenant.TenantId, ct);
        if (billing == null)
            return Result<PaymentResponse>.Failure("BILLING_NOT_FOUND", "Khong tim thay hoa don");
        if (billing.Status == BillingStatus.Void)
            return Result<PaymentResponse>.Failure("PAYMENT_AMOUNT_INVALID", "Hoa don da huy");

        // Auto-attach cashier shift
        Guid? shiftId = null;
        if (_user.UserId.HasValue)
        {
            var shift = await _shiftSvc.GetOpenShiftAsync(_user.UserId.Value, _tenant.TenantId, ct);
            shiftId = shift?.Id;
        }

        var payment = new Payment
        {
            TenantId = _tenant.TenantId,
            BillingId = req.BillingId,
            CashierShiftId = shiftId,
            Amount = req.Amount,
            Method = req.Method,
            Reference = req.Reference,
            Provider = req.Provider,
            ProviderTxnId = req.ProviderTxnId,
            Note = req.Note,
            Status = PaymentStatus.Completed,
            PaidAt = DateTime.UtcNow,
            PaidBy = _user.UserId,
            CreatedBy = _user.UserId
        };

        _db.Payments.Add(payment);

        // Update billing
        billing.PaidAmount += req.Amount;
        billing.Balance = billing.PatientPayable - billing.PaidAmount;
        billing.Status = billing.Balance <= 0 ? BillingStatus.Paid : BillingStatus.PartialPaid;

        await _db.SaveChangesAsync(ct);
        return Result<PaymentResponse>.Success(PaymentMapper.ToDto(payment));
    }
}

public class GetPaymentHandler : IRequestHandler<GetPaymentQuery, Result<PaymentResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantProvider _tenant;

    public GetPaymentHandler(IApplicationDbContext db, ITenantProvider tenant)
    {
        _db = db; _tenant = tenant;
    }

    public async Task<Result<PaymentResponse>> Handle(GetPaymentQuery query, CancellationToken ct)
    {
        var p = await _db.Payments
            .FirstOrDefaultAsync(x => x.Id == query.Id && x.TenantId == _tenant.TenantId, ct);
        if (p == null) return Result<PaymentResponse>.Failure("PAYMENT_NOT_FOUND", "Khong tim thay thanh toan");
        return Result<PaymentResponse>.Success(PaymentMapper.ToDto(p));
    }
}

public class ListPaymentsHandler : IRequestHandler<ListPaymentsQuery, Result<PagedResult<PaymentResponse>>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;

    public ListPaymentsHandler(IDapperConnectionFactory db, ITenantProvider tenant)
    {
        _db = db; _tenant = tenant;
    }

    public async Task<Result<PagedResult<PaymentResponse>>> Handle(ListPaymentsQuery query, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var where = "WHERE tenant_id = @tenantId";
        var p = new DynamicParameters();
        p.Add("tenantId", _tenant.TenantId);

        if (query.BillingId.HasValue) { where += " AND billing_id = @bid"; p.Add("bid", query.BillingId.ToString()); }
        if (!string.IsNullOrEmpty(query.Method)) { where += " AND method = @method"; p.Add("method", query.Method); }
        if (!string.IsNullOrEmpty(query.Status)) { where += " AND status = @status"; p.Add("status", query.Status); }
        if (query.FromDate.HasValue) { where += " AND DATE(created_at) >= @from"; p.Add("from", query.FromDate.Value.ToString("yyyy-MM-dd")); }
        if (query.ToDate.HasValue) { where += " AND DATE(created_at) <= @to"; p.Add("to", query.ToDate.Value.ToString("yyyy-MM-dd")); }

        var total = await conn.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM diab_his_bil_payments {where}", p);
        var offset = (query.Page - 1) * query.PageSize;
        p.Add("limit", query.PageSize); p.Add("offset", offset);

        var rows = await conn.QueryAsync<dynamic>(
            $"SELECT * FROM diab_his_bil_payments {where} ORDER BY created_at DESC LIMIT @limit OFFSET @offset", p);

        var items = rows.Select(r => new PaymentResponse(
            Guid.Parse((string)r.id), (int)r.tenant_id,
            Guid.Parse((string)r.billing_id), (decimal)r.amount,
            (string)r.method, (string?)r.reference, (string)r.status,
            (string?)r.provider, (string?)r.provider_txn_id,
            r.paid_at == null ? null : (DateTime?)r.paid_at,
            r.paid_by == null ? null : Guid.Parse((string)r.paid_by),
            r.cashier_shift_id == null ? null : Guid.Parse((string)r.cashier_shift_id),
            (string?)r.note, (decimal)r.refunded_amount, (DateTime)r.created_at)).ToList();

        return Result<PagedResult<PaymentResponse>>.Success(
            new PagedResult<PaymentResponse>(items, query.Page, query.PageSize, total));
    }
}

public class RefundPaymentHandler : IRequestHandler<RefundPaymentCommand, Result<PaymentResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _user;

    public RefundPaymentHandler(IApplicationDbContext db, ITenantProvider tenant, ICurrentUser user)
    {
        _db = db; _tenant = tenant; _user = user;
    }

    public async Task<Result<PaymentResponse>> Handle(RefundPaymentCommand cmd, CancellationToken ct)
    {
        var original = await _db.Payments
            .FirstOrDefaultAsync(p => p.Id == cmd.PaymentId && p.TenantId == _tenant.TenantId, ct);
        if (original == null) return Result<PaymentResponse>.Failure("PAYMENT_NOT_FOUND", "Khong tim thay thanh toan");

        var refund = new Payment
        {
            TenantId = _tenant.TenantId,
            BillingId = original.BillingId,
            Amount = -cmd.Request.Amount,
            Method = original.Method,
            Status = PaymentStatus.Refunded,
            Note = cmd.Request.Reason,
            PaidAt = DateTime.UtcNow,
            PaidBy = _user.UserId,
            CreatedBy = _user.UserId
        };
        original.RefundedAmount += cmd.Request.Amount;
        original.Status = PaymentStatus.Refunded;

        _db.Payments.Add(refund);

        // Revert billing
        var billing = await _db.Billings.FirstOrDefaultAsync(b => b.Id == original.BillingId, ct);
        if (billing != null)
        {
            billing.PaidAmount -= cmd.Request.Amount;
            billing.Balance = billing.PatientPayable - billing.PaidAmount;
            billing.Status = billing.Balance <= 0 ? BillingStatus.Paid : BillingStatus.PartialPaid;
        }

        await _db.SaveChangesAsync(ct);
        return Result<PaymentResponse>.Success(PaymentMapper.ToDto(refund));
    }
}

public class VoidPaymentHandler : IRequestHandler<VoidPaymentCommand, Result<PaymentResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantProvider _tenant;

    public VoidPaymentHandler(IApplicationDbContext db, ITenantProvider tenant)
    {
        _db = db; _tenant = tenant;
    }

    public async Task<Result<PaymentResponse>> Handle(VoidPaymentCommand cmd, CancellationToken ct)
    {
        var p = await _db.Payments
            .FirstOrDefaultAsync(x => x.Id == cmd.PaymentId && x.TenantId == _tenant.TenantId, ct);
        if (p == null) return Result<PaymentResponse>.Failure("PAYMENT_NOT_FOUND", "Khong tim thay thanh toan");

        p.Status = PaymentStatus.Void;
        p.Note = cmd.Request.Reason ?? p.Note;
        await _db.SaveChangesAsync(ct);
        return Result<PaymentResponse>.Success(PaymentMapper.ToDto(p));
    }
}

public class ListPaymentMethodsHandler : IRequestHandler<ListPaymentMethodsQuery, Result<List<PaymentMethodDto>>>
{
    public Task<Result<List<PaymentMethodDto>>> Handle(ListPaymentMethodsQuery query, CancellationToken ct)
    {
        var methods = new List<PaymentMethodDto>
        {
            new("CASH", true, null),
            new("BANK_TRANSFER", true, null),
            new("VISA", true, "VISA"),
            new("MASTER", true, "MASTER"),
            new("QR_VIETQR", true, "VIETQR"),
            new("QR_MOMO", true, "MOMO"),
            new("QR_VNPAY", true, "VNPAY"),
            new("OTHER", true, null)
        };
        return Task.FromResult(Result<List<PaymentMethodDto>>.Success(methods));
    }
}

public class GenerateQrHandler : IRequestHandler<GenerateQrCommand, Result<QrCodeResponseDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantProvider _tenant;
    private readonly IEnumerable<IPaymentGateway> _gateways;

    public GenerateQrHandler(IApplicationDbContext db, ITenantProvider tenant, IEnumerable<IPaymentGateway> gateways)
    {
        _db = db; _tenant = tenant; _gateways = gateways;
    }

    public async Task<Result<QrCodeResponseDto>> Handle(GenerateQrCommand cmd, CancellationToken ct)
    {
        var req = cmd.Request;
        var gateway = _gateways.FirstOrDefault(g => g.Provider.Equals(req.Provider, StringComparison.OrdinalIgnoreCase));
        if (gateway == null)
            return Result<QrCodeResponseDto>.Failure("QR_PROVIDER_NOT_SUPPORTED", "Provider khong duoc ho tro");

        var txnRef = $"PD{DateTime.Now:yyyyMMddHHmmss}{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
        var qrResult = await gateway.GenerateQrAsync(new QrGenerateRequest(
            req.BillingId, req.Provider, req.Amount, txnRef, req.ExpiresInSeconds), ct);

        var qr = new QrCode
        {
            TenantId = _tenant.TenantId,
            BillingId = req.BillingId,
            Provider = req.Provider,
            QrPayload = qrResult.QrPayloadBase64,
            QrUrl = qrResult.QrUrl,
            Amount = req.Amount,
            TransactionRef = txnRef,
            ExpiresAt = DateTime.UtcNow.AddSeconds(req.ExpiresInSeconds),
            Status = "PENDING"
        };
        _db.QrCodes.Add(qr);
        await _db.SaveChangesAsync(ct);
        return Result<QrCodeResponseDto>.Success(PaymentMapper.ToQrDto(qr));
    }
}

public class GetQrStatusHandler : IRequestHandler<GetQrStatusQuery, Result<QrCodeResponseDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantProvider _tenant;

    public GetQrStatusHandler(IApplicationDbContext db, ITenantProvider tenant)
    {
        _db = db; _tenant = tenant;
    }

    public async Task<Result<QrCodeResponseDto>> Handle(GetQrStatusQuery query, CancellationToken ct)
    {
        var qr = await _db.QrCodes.FirstOrDefaultAsync(q => q.Id == query.QrId && q.TenantId == _tenant.TenantId, ct);
        if (qr == null) return Result<QrCodeResponseDto>.Failure("QR_NOT_FOUND", "Khong tim thay QR");
        if (qr.Status == "PENDING" && qr.ExpiresAt < DateTime.UtcNow)
        {
            qr.Status = "EXPIRED";
            await _db.SaveChangesAsync(ct);
        }
        return Result<QrCodeResponseDto>.Success(PaymentMapper.ToQrDto(qr));
    }
}

public class ProcessQrWebhookHandler : IRequestHandler<ProcessQrWebhookCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly IEnumerable<IPaymentGateway> _gateways;

    public ProcessQrWebhookHandler(IApplicationDbContext db, IEnumerable<IPaymentGateway> gateways)
    {
        _db = db; _gateways = gateways;
    }

    public async Task<Result> Handle(ProcessQrWebhookCommand cmd, CancellationToken ct)
    {
        var gateway = _gateways.FirstOrDefault(g =>
            g.Provider.Equals(cmd.Provider, StringComparison.OrdinalIgnoreCase));
        if (gateway == null) return Result.Failure("PROVIDER_NOT_FOUND", "Provider khong tim thay");

        // Parse provider_txn_id from payload (simplified)
        string? providerTxnId = null;
        string? txnRef = null;
        try
        {
            var doc = System.Text.Json.JsonDocument.Parse(cmd.Payload);
            providerTxnId = doc.RootElement.TryGetProperty("transId", out var tid) ? tid.GetString() :
                doc.RootElement.TryGetProperty("vnp_TransactionNo", out var vid) ? vid.GetString() : null;
            txnRef = doc.RootElement.TryGetProperty("orderInfo", out var oi) ? oi.GetString() :
                doc.RootElement.TryGetProperty("vnp_TxnRef", out var vr) ? vr.GetString() : null;
        }
        catch { return Result.Failure("WEBHOOK_PARSE_ERROR", "Khong parse duoc payload"); }

        // Idempotency check
        var existing = await _db.Payments
            .AnyAsync(p => p.Provider == cmd.Provider && p.ProviderTxnId == providerTxnId, ct);
        if (existing) return Result.Success(); // Already processed

        // Find QR by txnRef
        if (txnRef != null)
        {
            var qr = await _db.QrCodes
                .FirstOrDefaultAsync(q => q.TransactionRef == txnRef && q.Status == "PENDING", ct);
            if (qr != null)
            {
                qr.Status = "PAID";
                qr.PaidAt = DateTime.UtcNow;

                // Create payment record
                var payment = new Payment
                {
                    TenantId = qr.TenantId,
                    BillingId = qr.BillingId,
                    Amount = qr.Amount,
                    Method = $"QR_{cmd.Provider.ToUpper()}",
                    Provider = cmd.Provider,
                    ProviderTxnId = providerTxnId,
                    Status = PaymentStatus.Completed,
                    PaidAt = DateTime.UtcNow
                };
                _db.Payments.Add(payment);

                var billing = await _db.Billings.FirstOrDefaultAsync(b => b.Id == qr.BillingId, ct);
                if (billing != null)
                {
                    billing.PaidAmount += qr.Amount;
                    billing.Balance = billing.PatientPayable - billing.PaidAmount;
                    billing.Status = billing.Balance <= 0 ? BillingStatus.Paid : BillingStatus.PartialPaid;
                }

                await _db.SaveChangesAsync(ct);
            }
        }
        return Result.Success();
    }
}

public class CardChargeHandler : IRequestHandler<CardChargeCommand, Result<PaymentResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _user;
    private readonly IEnumerable<IPaymentGateway> _gateways;
    private readonly ICashierShiftService _shiftSvc;

    public CardChargeHandler(
        IApplicationDbContext db, ITenantProvider tenant, ICurrentUser user,
        IEnumerable<IPaymentGateway> gateways, ICashierShiftService shiftSvc)
    {
        _db = db; _tenant = tenant; _user = user;
        _gateways = gateways; _shiftSvc = shiftSvc;
    }

    public async Task<Result<PaymentResponse>> Handle(CardChargeCommand cmd, CancellationToken ct)
    {
        var req = cmd.Request;
        var gateway = _gateways.FirstOrDefault(g =>
            g.Provider.Equals(req.Provider, StringComparison.OrdinalIgnoreCase));
        if (gateway == null)
            return Result<PaymentResponse>.Failure("GATEWAY_NOT_FOUND", "Gateway khong tim thay");

        var billing = await _db.Billings
            .FirstOrDefaultAsync(b => b.Id == req.BillingId && b.TenantId == _tenant.TenantId, ct);
        if (billing == null)
            return Result<PaymentResponse>.Failure("BILLING_NOT_FOUND", "Khong tim thay hoa don");

        var chargeResult = await gateway.ChargeCardAsync(
            new CardChargeRequest(req.BillingId, req.Amount, req.CardToken, req.Provider, req.ThreeDsNonce), ct);
        if (!chargeResult.Success)
            return Result<PaymentResponse>.Failure("PAYMENT_GATEWAY_ERROR",
                chargeResult.ErrorMessage ?? "Loi gateway");

        Guid? shiftId = null;
        if (_user.UserId.HasValue)
        {
            var shift = await _shiftSvc.GetOpenShiftAsync(_user.UserId.Value, _tenant.TenantId, ct);
            shiftId = shift?.Id;
        }

        var payment = new Payment
        {
            TenantId = _tenant.TenantId,
            BillingId = req.BillingId,
            CashierShiftId = shiftId,
            Amount = req.Amount,
            Method = req.Provider == "MASTER" ? "MASTER" : "VISA",
            Provider = req.Provider,
            ProviderTxnId = chargeResult.ProviderTxnId,
            Status = PaymentStatus.Completed,
            PaidAt = DateTime.UtcNow,
            PaidBy = _user.UserId,
            CreatedBy = _user.UserId
        };
        _db.Payments.Add(payment);

        billing.PaidAmount += req.Amount;
        billing.Balance = billing.PatientPayable - billing.PaidAmount;
        billing.Status = billing.Balance <= 0 ? BillingStatus.Paid : BillingStatus.PartialPaid;
        await _db.SaveChangesAsync(ct);

        return Result<PaymentResponse>.Success(PaymentMapper.ToDto(payment));
    }
}
