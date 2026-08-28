using System.Data;
using Dapper;
using MediatR;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.Common.Interfaces;

namespace ProDiabHis.Application.Packages;

internal static class SubscriptionMapper
{
    public static async Task<SubscriptionResponse> LoadAsync(IDbConnection conn, string id, int tenantId, IDbTransaction? tx = null)
    {
        var s = await conn.QueryFirstAsync<dynamic>(
            "SELECT * FROM diab_his_pkg_subscriptions WHERE id=@id AND tenant_id=@tenantId", new { id, tenantId }, tx);

        var balances = (await conn.QueryAsync<dynamic>(
            @"SELECT id, item_type, item_code, item_name, unit, total_quantity, used_quantity, remaining_quantity
              FROM diab_his_pkg_entitlement_balances WHERE subscription_id=@id ORDER BY item_type, item_name",
            new { id }, tx)).Select(b => new BalanceResponse(
                Guid.Parse((string)b.id), (string)b.item_type, (string)b.item_code, (string)b.item_name, (string)b.unit,
                (decimal)b.total_quantity, (decimal)b.used_quantity, (decimal)b.remaining_quantity)).ToList();

        decimal totalPrice = (decimal)s.total_price;
        decimal amountPaid = (decimal)s.amount_paid;
        decimal? depositPercentPaid = totalPrice > 0 ? Math.Round(amountPaid / totalPrice * 100, 2) : null;

        return new SubscriptionResponse(
            Guid.Parse((string)s.id), (string)s.subscription_no, Guid.Parse((string)s.patient_id), Guid.Parse((string)s.package_id),
            (string)s.package_name_snapshot,
            DateOnly.FromDateTime((DateTime)s.purchase_date), DateOnly.FromDateTime((DateTime)s.effective_date),
            DateOnly.FromDateTime((DateTime)s.expiry_date),
            totalPrice, amountPaid, (decimal)s.amount_due, depositPercentPaid,
            (string)s.payment_status, (string)s.status, (DateTime?)s.activated_at,
            (decimal)s.refunded_amount, (DateTime?)s.cancelled_at, (string?)s.cancel_reason, balances);
    }
}

public class ListSubscriptionsHandler : IRequestHandler<ListSubscriptionsQuery, Result<PagedResult<SubscriptionResponse>>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    public ListSubscriptionsHandler(IDapperConnectionFactory db, ITenantProvider tenant) { _db = db; _tenant = tenant; }

    public async Task<Result<PagedResult<SubscriptionResponse>>> Handle(ListSubscriptionsQuery q, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var tenantId = _tenant.TenantId;
        var where = "WHERE tenant_id=@tenantId AND deleted_at IS NULL";
        var p = new DynamicParameters();
        p.Add("tenantId", tenantId);
        if (q.PatientId.HasValue) { where += " AND patient_id=@patientId"; p.Add("patientId", q.PatientId.Value.ToString()); }
        if (!string.IsNullOrWhiteSpace(q.Status)) { where += " AND status=@status"; p.Add("status", q.Status); }
        if (!string.IsNullOrWhiteSpace(q.PaymentStatus)) { where += " AND payment_status=@paymentStatus"; p.Add("paymentStatus", q.PaymentStatus); }
        if (q.HasDebt == true) { where += " AND amount_due > 0"; }
        if (q.BranchId.HasValue) { where += " AND branch_id=@branchId"; p.Add("branchId", q.BranchId.Value); }
        if (q.ExpiringWithinDays.HasValue) { where += " AND expiry_date <= DATE_ADD(CURDATE(), INTERVAL @days DAY)"; p.Add("days", q.ExpiringWithinDays.Value); }

        var total = await conn.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM diab_his_pkg_subscriptions {where}", p);
        var offset = (q.Page - 1) * q.PageSize;
        p.Add("offset", offset); p.Add("limit", q.PageSize);
        var ids = await conn.QueryAsync<string>(
            $"SELECT id FROM diab_his_pkg_subscriptions {where} ORDER BY created_at DESC LIMIT @limit OFFSET @offset", p);

        var items = new List<SubscriptionResponse>();
        foreach (var id in ids) items.Add(await SubscriptionMapper.LoadAsync(conn, id, tenantId));
        return Result<PagedResult<SubscriptionResponse>>.Success(new PagedResult<SubscriptionResponse>(items, q.Page, q.PageSize, total));
    }
}

public class GetSubscriptionHandler : IRequestHandler<GetSubscriptionQuery, Result<SubscriptionResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    public GetSubscriptionHandler(IDapperConnectionFactory db, ITenantProvider tenant) { _db = db; _tenant = tenant; }

    public async Task<Result<SubscriptionResponse>> Handle(GetSubscriptionQuery q, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var exists = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM diab_his_pkg_subscriptions WHERE id=@id AND tenant_id=@tenantId",
            new { id = q.Id.ToString(), tenantId = _tenant.TenantId });
        if (exists == 0) return Result<SubscriptionResponse>.Failure("PACKAGE_SUBSCRIPTION_NOT_FOUND", "Khong tim thay goi dinh muc da mua");
        return Result<SubscriptionResponse>.Success(await SubscriptionMapper.LoadAsync(conn, q.Id.ToString(), _tenant.TenantId));
    }
}

public class GetPatientPackageSummaryHandler : IRequestHandler<GetPatientPackageSummaryQuery, Result<PackagePatientSummary>>
{
    private readonly IPackageEntitlementService _svc;
    public GetPatientPackageSummaryHandler(IPackageEntitlementService svc) { _svc = svc; }

    public async Task<Result<PackagePatientSummary>> Handle(GetPatientPackageSummaryQuery q, CancellationToken ct)
        => Result<PackagePatientSummary>.Success(await _svc.GetPatientSummaryAsync(q.PatientId, ct));
}

/// <summary>
/// FR-1202 - Ban goi dinh muc + thu tien lan dau. Policy coc toi thieu (D8): mac dinh 50%
/// (khoa cau hinh <c>pkg.min_deposit_percent</c> trong <c>diab_his_sys_settings</c>, doc qua
/// <see cref="ISettingsProvider"/> - migration 9095), co the override rieng theo tung goi o
/// pkg_service_packages.min_deposit_percent (uu tien cao hon setting he thong).
/// </summary>
public class CreateSubscriptionHandler : IRequestHandler<CreateSubscriptionCommand, Result<SubscriptionResponse>>
{
    /// <summary>Fallback cuoi cung neu ca setting he thong lan override goi deu khong co (khong xay ra sau seed 9095).</summary>
    public const decimal DefaultMinDepositPercent = 50m;

    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    private readonly IBranchProvider _branch;
    private readonly ICurrentUser _user;
    private readonly IAuditService _audit;
    private readonly ISettingsProvider _settings;

    public CreateSubscriptionHandler(IDapperConnectionFactory db, ITenantProvider tenant, IBranchProvider branch,
        ICurrentUser user, IAuditService audit, ISettingsProvider settings)
    { _db = db; _tenant = tenant; _branch = branch; _user = user; _audit = audit; _settings = settings; }

    public async Task<Result<SubscriptionResponse>> Handle(CreateSubscriptionCommand cmd, CancellationToken ct)
    {
        var req = cmd.Request;
        using var conn = _db.CreateConnection();
        conn.Open();
        var tenantId = _tenant.TenantId;
        var branchId = _branch.BranchId > 0 ? _branch.BranchId : (int?)null;

        var pkg = await conn.QueryFirstOrDefaultAsync<dynamic>(
            @"SELECT * FROM diab_his_pkg_service_packages WHERE id=@id AND tenant_id=@tenantId AND deleted_at IS NULL",
            new { id = req.PackageId.ToString(), tenantId });
        if (pkg == null) return Result<SubscriptionResponse>.Failure("PACKAGE_NOT_FOUND", "Khong tim thay goi dinh muc");
        if (!Convert.ToBoolean(pkg.is_active)) return Result<SubscriptionResponse>.Failure("PACKAGE_NOT_SELLABLE", "Goi hien khong con ban");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (pkg.valid_from != null && today < DateOnly.FromDateTime((DateTime)pkg.valid_from))
            return Result<SubscriptionResponse>.Failure("PACKAGE_NOT_SELLABLE", "Chua den thoi gian mo ban goi");
        if (pkg.valid_to != null && today > DateOnly.FromDateTime((DateTime)pkg.valid_to))
            return Result<SubscriptionResponse>.Failure("PACKAGE_NOT_SELLABLE", "Goi da het thoi gian ban");

        var patientExists = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM diab_his_pat_patients WHERE id=@id AND tenant_id=@tenantId AND deleted_at IS NULL",
            new { id = req.PatientId.ToString(), tenantId });
        if (patientExists == 0) return Result<SubscriptionResponse>.Failure("PATIENT_NOT_FOUND", "Khong tim thay benh nhan");

        var systemMinPercent = await _settings.GetDecimalAsync("pkg.min_deposit_percent", DefaultMinDepositPercent, ct);
        var minPercent = (decimal?)pkg.min_deposit_percent ?? systemMinPercent;
        var requiredMin = Math.Round(req.TotalPrice * minPercent / 100, 2);
        if (req.InitialPayment.Amount < requiredMin)
            return Result<SubscriptionResponse>.Failure("PACKAGE_DEPOSIT_BELOW_MINIMUM",
                $"So tien coc toi thieu la {requiredMin:N0} ({minPercent}% gia tri goi)",
                new { required_min = requiredMin, provided = req.InitialPayment.Amount, min_percent = minPercent });
        if (req.InitialPayment.Amount > req.TotalPrice)
            return Result<SubscriptionResponse>.Failure("PACKAGE_PAYMENT_EXCEEDS_TOTAL", "So tien thu vuot gia tri goi");

        var defs = (await conn.QueryAsync<dynamic>(
            "SELECT * FROM diab_his_pkg_entitlement_definitions WHERE package_id=@pkgId AND deleted_at IS NULL",
            new { pkgId = (string)pkg.id })).ToList();

        using var tx = conn.BeginTransaction();
        try
        {
            var subId = Guid.NewGuid().ToString();
            var effectiveDate = req.EffectiveDate ?? today;
            var durationDays = (int)pkg.duration_days;
            var expiryDate = effectiveDate.AddDays(durationDays);
            var subscriptionNo = await GenerateSubscriptionNoAsync(conn, tx, tenantId);

            var isFull = req.InitialPayment.Amount >= req.TotalPrice;
            var paymentStatus = isFull ? "paid_full" : "deposit_paid";

            await conn.ExecuteAsync(
                @"INSERT INTO diab_his_pkg_subscriptions
                  (id, tenant_id, branch_id, patient_id, package_id, subscription_no, package_code_snapshot,
                   package_name_snapshot, purchase_date, effective_date, expiry_date, duration_days_snapshot,
                   total_price, amount_paid, payment_status, status, activated_at, note, created_at, updated_at, created_by)
                  VALUES (@subId, @tenantId, @branchId, @patientId, @packageId, @subscriptionNo, @codeSnap, @nameSnap,
                   @purchaseDate, @effectiveDate, @expiryDate, @durationDays, @totalPrice, @amountPaid, @paymentStatus,
                   'active', UTC_TIMESTAMP(3), @note, UTC_TIMESTAMP(), UTC_TIMESTAMP(), @createdBy)",
                new
                {
                    subId, tenantId, branchId, patientId = req.PatientId.ToString(), packageId = req.PackageId.ToString(),
                    subscriptionNo, codeSnap = (string)pkg.code, nameSnap = (string)pkg.name,
                    purchaseDate = today, effectiveDate, expiryDate, durationDays,
                    totalPrice = req.TotalPrice, amountPaid = req.InitialPayment.Amount, paymentStatus,
                    note = req.Note, createdBy = _user.UserId?.ToString()
                }, tx);

            // D5: snapshot dinh muc vao balances - kich hoat ngay (RULE-S1)
            foreach (var d in defs)
            {
                var unitPrice = await PackageMapper.ResolveCurrentPriceAsync(conn, (string)d.item_type, (string)d.item_ref_id, tx);
                await conn.ExecuteAsync(
                    @"INSERT INTO diab_his_pkg_entitlement_balances
                      (id, tenant_id, subscription_id, definition_id, item_type, item_ref_id, item_code, item_name,
                       unit, total_quantity, used_quantity, unit_price_snapshot, version, created_at, updated_at, created_by)
                      VALUES (UUID(), @tenantId, @subId, @definitionId, @itemType, @itemRefId, @itemCode, @itemName,
                       @unit, @totalQty, 0, @unitPrice, 0, UTC_TIMESTAMP(), UTC_TIMESTAMP(), @createdBy)",
                    new
                    {
                        tenantId, subId, definitionId = (string)d.id, itemType = (string)d.item_type,
                        itemRefId = (string)d.item_ref_id, itemCode = (string)d.item_code, itemName = (string)d.item_name,
                        unit = (string)d.unit, totalQty = (decimal)d.quantity, unitPrice, createdBy = _user.UserId?.ToString()
                    }, tx);
            }

            // Hoa don ban goi (D11 - tach bach doanh thu tra truoc)
            var billingId = Guid.NewGuid().ToString();
            await conn.ExecuteAsync(
                @"INSERT INTO diab_his_bil_billing
                  (id, tenant_id, patient_id, payer, subtotal, vat_total, patient_payable, paid_amount, balance,
                   status, package_subscription_id, note, created_at, updated_at, created_by)
                  VALUES (@billingId, @tenantId, @patientId, 'SELF', @totalPrice, 0, @totalPrice, @amountPaid,
                   @balance, @status, @subId, @note, UTC_TIMESTAMP(3), UTC_TIMESTAMP(3), @createdBy)",
                new
                {
                    billingId, tenantId, patientId = req.PatientId.ToString(), totalPrice = req.TotalPrice,
                    amountPaid = req.InitialPayment.Amount, balance = req.TotalPrice - req.InitialPayment.Amount,
                    status = isFull ? "PAID" : "PARTIAL_PAID", subId, note = $"Ban goi dinh muc {pkg.name}",
                    createdBy = _user.UserId?.ToString()
                }, tx);

            var paymentId = Guid.NewGuid().ToString();
            await conn.ExecuteAsync(
                @"INSERT INTO diab_his_bil_payments
                  (id, tenant_id, billing_id, amount, method, status, paid_at, paid_by, note, created_at, updated_at, created_by)
                  VALUES (@paymentId, @tenantId, @billingId, @amount, @method, 'COMPLETED', UTC_TIMESTAMP(3), @paidBy, @note, UTC_TIMESTAMP(3), UTC_TIMESTAMP(3), @createdBy)",
                new
                {
                    paymentId, tenantId, billingId, amount = req.InitialPayment.Amount, method = req.InitialPayment.Method,
                    paidBy = _user.UserId?.ToString(), note = "Thu coc mua goi dinh muc", createdBy = _user.UserId?.ToString()
                }, tx);

            var einvoiceId = req.InitialPayment.IssueEinvoice ? Guid.NewGuid().ToString() : null; // TODO: goi IEInvoiceService that khi tich hop HDDT

            await conn.ExecuteAsync(
                @"INSERT INTO diab_his_pkg_payment_records
                  (id, tenant_id, branch_id, subscription_id, billing_id, payment_id, payment_kind, amount, method,
                   paid_at, cashier_user_id, einvoice_id, note, created_at, created_by)
                  VALUES (UUID(), @tenantId, @branchId, @subId, @billingId, @paymentId, 'DEPOSIT', @amount, @method,
                   UTC_TIMESTAMP(3), @cashierUserId, @einvoiceId, @note, UTC_TIMESTAMP(), @createdBy)",
                new
                {
                    tenantId, branchId, subId, billingId, paymentId, amount = req.InitialPayment.Amount,
                    method = req.InitialPayment.Method, cashierUserId = _user.UserId?.ToString(), einvoiceId,
                    note = "Thu lan dau", createdBy = _user.UserId?.ToString()
                }, tx);

            tx.Commit();
            await _audit.LogAsync("CREATE", "diab_his_pkg_subscriptions", subId,
                new { patientId = req.PatientId, packageId = req.PackageId, amount = req.InitialPayment.Amount }, ct);

            return Result<SubscriptionResponse>.Success(await SubscriptionMapper.LoadAsync(conn, subId, tenantId));
        }
        catch { tx.Rollback(); throw; }
    }

    /// <summary>Sinh so hop dong dang GOI-{yyyy}-{seq}. Don gian hoa so voi doc (bil_counters) do gioi
    /// han thoi gian trien khai - an toan vi unique constraint (tenant_id, subscription_no) o DB chan trung.</summary>
    private static async Task<string> GenerateSubscriptionNoAsync(IDbConnection conn, IDbTransaction tx, int tenantId)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"GOI-{year}-";
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var count = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM diab_his_pkg_subscriptions WHERE tenant_id=@tenantId AND subscription_no LIKE @prefix",
                new { tenantId, prefix = $"{prefix}%" }, tx);
            var candidate = $"{prefix}{(count + 1 + attempt):000000}";
            var exists = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM diab_his_pkg_subscriptions WHERE tenant_id=@tenantId AND subscription_no=@candidate",
                new { tenantId, candidate }, tx);
            if (exists == 0) return candidate;
        }
        return $"{prefix}{Guid.NewGuid().ToString()[..8]}";
    }
}

/// <summary>FR-1203 - thu not, khong ap lai nguong 50% (RULE-S2).</summary>
public class AddSubscriptionPaymentHandler : IRequestHandler<AddSubscriptionPaymentCommand, Result<SubscriptionResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    private readonly IBranchProvider _branch;
    private readonly ICurrentUser _user;
    private readonly IAuditService _audit;

    public AddSubscriptionPaymentHandler(IDapperConnectionFactory db, ITenantProvider tenant, IBranchProvider branch, ICurrentUser user, IAuditService audit)
    { _db = db; _tenant = tenant; _branch = branch; _user = user; _audit = audit; }

    public async Task<Result<SubscriptionResponse>> Handle(AddSubscriptionPaymentCommand cmd, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        conn.Open();
        var tenantId = _tenant.TenantId;
        var subId = cmd.SubscriptionId.ToString();

        var sub = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT * FROM diab_his_pkg_subscriptions WHERE id=@subId AND tenant_id=@tenantId AND deleted_at IS NULL", new { subId, tenantId });
        if (sub == null) return Result<SubscriptionResponse>.Failure("PACKAGE_SUBSCRIPTION_NOT_FOUND", "Khong tim thay goi dinh muc da mua");

        var amountDue = (decimal)sub.amount_due;
        if (cmd.Request.Amount > amountDue)
            return Result<SubscriptionResponse>.Failure("PACKAGE_PAYMENT_EXCEEDS_DUE", "So tien thu vuot cong no con lai");

        using var tx = conn.BeginTransaction();
        try
        {
            var newPaid = (decimal)sub.amount_paid + cmd.Request.Amount;
            var newStatus = newPaid >= (decimal)sub.total_price ? "paid_full" : "deposit_paid";
            await conn.ExecuteAsync(
                @"UPDATE diab_his_pkg_subscriptions SET amount_paid=@newPaid, payment_status=@newStatus, updated_at=UTC_TIMESTAMP()
                  WHERE id=@subId", new { newPaid, newStatus, subId }, tx);

            var billingId = (string?)sub.id; // fallback: dung chinh subscription lam moc, billing rieng khong bat buoc trong pham vi nay
            var paymentId = Guid.NewGuid().ToString();
            await conn.ExecuteAsync(
                @"INSERT INTO diab_his_bil_payments
                  (id, tenant_id, billing_id, amount, method, status, paid_at, paid_by, note, created_at, updated_at, created_by)
                  SELECT @paymentId, @tenantId, b.id, @amount, @method, 'COMPLETED', UTC_TIMESTAMP(3), @paidBy, 'Thu not goi dinh muc', UTC_TIMESTAMP(3), UTC_TIMESTAMP(3), @createdBy
                  FROM diab_his_bil_billing b WHERE b.package_subscription_id=@subId LIMIT 1",
                new { paymentId, tenantId, amount = cmd.Request.Amount, method = cmd.Request.Method, paidBy = _user.UserId?.ToString(), createdBy = _user.UserId?.ToString(), subId }, tx);

            await conn.ExecuteAsync(
                @"UPDATE diab_his_bil_billing SET paid_amount = paid_amount + @amount, balance = balance - @amount,
                  status = CASE WHEN balance - @amount <= 0 THEN 'PAID' ELSE 'PARTIAL_PAID' END, updated_at=UTC_TIMESTAMP(3)
                  WHERE package_subscription_id=@subId", new { amount = cmd.Request.Amount, subId }, tx);

            var einvoiceId = cmd.Request.IssueEinvoice ? Guid.NewGuid().ToString() : null;
            await conn.ExecuteAsync(
                @"INSERT INTO diab_his_pkg_payment_records
                  (id, tenant_id, branch_id, subscription_id, payment_id, payment_kind, amount, method, paid_at,
                   cashier_user_id, einvoice_id, note, created_at, created_by)
                  VALUES (UUID(), @tenantId, @branchId, @subId, @paymentId, 'SETTLEMENT', @amount, @method, UTC_TIMESTAMP(3),
                   @cashierUserId, @einvoiceId, @note, UTC_TIMESTAMP(), @createdBy)",
                new
                {
                    tenantId, branchId = _branch.BranchId > 0 ? _branch.BranchId : (int?)null, subId, paymentId,
                    amount = cmd.Request.Amount, method = cmd.Request.Method, cashierUserId = _user.UserId?.ToString(),
                    einvoiceId, note = cmd.Request.Note, createdBy = _user.UserId?.ToString()
                }, tx);

            tx.Commit();
            await _audit.LogAsync("UPDATE", "diab_his_pkg_subscriptions", subId, new { paymentAdded = cmd.Request.Amount }, ct);
            return Result<SubscriptionResponse>.Success(await SubscriptionMapper.LoadAsync(conn, subId, tenantId));
        }
        catch { tx.Rollback(); throw; }
    }
}

/// <summary>
/// FR-huy goi + Quyet dinh nghiep vu #3 (chot voi PO, KHAC voi de xuat "khong hoan tien" cua doc Q5):
/// Hoan tien theo TY LE dinh muc CHUA DUNG, tinh theo DON GIA THI TRUONG HIEN TAI (khong phai
/// gia luc ban), vi khach hang "tra lai" phan chua tieu thu chu khong phai "huy hop dong theo gia goc".
///
/// Cong thuc (ghi ro vi day la nghiep vu co the can dieu chinh sau):
///   unused_value   = SUM_over_balances( remaining_quantity_i * gia_le_hien_tai(item_i) )
///                    (gia_le_hien_tai = bil_services.price hoac pha_drugs.sale_price TAI THOI DIEM HUY,
///                     KHONG dung unit_price_snapshot luc ban)
///   total_value    = SUM_over_balances( total_quantity_i * gia_le_hien_tai(item_i) )
///                    (tong gia tri dinh muc GOC quy doi theo gia hien tai - dung lam mau so ty le)
///   refund_ratio   = unused_value / total_value   (neu total_value = 0 -> ratio = 0, khong hoan)
///   refund_amount  = amount_paid * refund_ratio, lam tron 2 chu so, toi thieu 0, toi da = amount_paid
///
/// Ly do dung gia HIEN TAI thay vi gia luc ban: neu dung gia luc ban se trung khop het voi
/// amount_paid * (SUM(remaining)/SUM(total)) tinh theo SO LUONG - nhung PO yeu cau ro rang
/// "gia tri chua dung tinh theo don gia hien tai" (vi du thuoc tang gia thi hoan nhieu hon,
/// dich vu giam gia thi hoan it hon) -> phan anh dung "gia tri thuc" tai thoi diem huy.
/// </summary>
public class CancelSubscriptionHandler : IRequestHandler<CancelSubscriptionCommand, Result<SubscriptionResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _user;
    private readonly IAuditService _audit;

    public CancelSubscriptionHandler(IDapperConnectionFactory db, ITenantProvider tenant, ICurrentUser user, IAuditService audit)
    { _db = db; _tenant = tenant; _user = user; _audit = audit; }

    public async Task<Result<SubscriptionResponse>> Handle(CancelSubscriptionCommand cmd, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        conn.Open();
        var tenantId = _tenant.TenantId;
        var subId = cmd.SubscriptionId.ToString();

        var sub = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT * FROM diab_his_pkg_subscriptions WHERE id=@subId AND tenant_id=@tenantId AND deleted_at IS NULL", new { subId, tenantId });
        if (sub == null) return Result<SubscriptionResponse>.Failure("PACKAGE_SUBSCRIPTION_NOT_FOUND", "Khong tim thay goi dinh muc da mua");
        var status = (string)sub.status;
        if (status is "cancelled" or "expired")
            return Result<SubscriptionResponse>.Failure("PACKAGE_SUBSCRIPTION_ALREADY_CLOSED", "Goi da o trang thai cuoi, khong the huy");

        var balances = (await conn.QueryAsync<dynamic>(
            "SELECT item_type, item_ref_id, total_quantity, remaining_quantity FROM diab_his_pkg_entitlement_balances WHERE subscription_id=@subId",
            new { subId })).ToList();

        decimal unusedValue = 0, totalValue = 0;
        foreach (var b in balances)
        {
            var currentPrice = await PackageMapper.ResolveCurrentPriceAsync(conn, (string)b.item_type, (string)b.item_ref_id);
            unusedValue += (decimal)b.remaining_quantity * currentPrice;
            totalValue += (decimal)b.total_quantity * currentPrice;
        }

        var refundRatio = totalValue > 0 ? unusedValue / totalValue : 0;
        var amountPaid = (decimal)sub.amount_paid;
        var refundAmount = Math.Max(0, Math.Round(amountPaid * refundRatio, 2));
        refundAmount = Math.Min(refundAmount, amountPaid);

        using var tx = conn.BeginTransaction();
        try
        {
            await conn.ExecuteAsync(
                @"UPDATE diab_his_pkg_subscriptions
                  SET status='cancelled', cancelled_at=UTC_TIMESTAMP(3), cancel_reason=@reason,
                      refunded_amount=@refundAmount,
                      payment_status = CASE WHEN @refundAmount >= amount_paid THEN 'refunded' ELSE payment_status END,
                      updated_at=UTC_TIMESTAMP(), updated_by=@updatedBy
                  WHERE id=@subId", new { reason = cmd.Request.Reason, refundAmount, subId, updatedBy = _user.UserId?.ToString() }, tx);

            if (refundAmount > 0)
            {
                await conn.ExecuteAsync(
                    @"INSERT INTO diab_his_pkg_payment_records
                      (id, tenant_id, subscription_id, payment_kind, amount, method, paid_at, cashier_user_id, note, created_at, created_by)
                      VALUES (UUID(), @tenantId, @subId, 'REFUND', @amount, 'CASH', UTC_TIMESTAMP(3), @cashierUserId, @note, UTC_TIMESTAMP(), @createdBy)",
                    new
                    {
                        tenantId, subId, amount = -refundAmount, cashierUserId = _user.UserId?.ToString(),
                        note = $"Hoan tien huy goi - ty le chua dung {refundRatio:P2} (cong thuc theo don gia hien tai)",
                        createdBy = _user.UserId?.ToString()
                    }, tx);
            }

            tx.Commit();
            await _audit.LogAsync("UPDATE", "diab_his_pkg_subscriptions", subId,
                new { action = "cancel", reason = cmd.Request.Reason, refundAmount, refundRatio }, ct);

            return Result<SubscriptionResponse>.Success(await SubscriptionMapper.LoadAsync(conn, subId, tenantId));
        }
        catch { tx.Rollback(); throw; }
    }
}
