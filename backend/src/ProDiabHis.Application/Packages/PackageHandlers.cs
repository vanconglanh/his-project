using System.Data;
using Dapper;
using FluentValidation;
using MediatR;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.Common.Interfaces;

namespace ProDiabHis.Application.Packages;

// ═══════════════════════════════════════════════════════════════
// Queries / Commands
// ═══════════════════════════════════════════════════════════════

public record ListPackagesQuery(string? Q, bool? IsActive, int Page, int PageSize)
    : IRequest<Result<PagedResult<PackageResponse>>>;

public record GetPackageQuery(Guid Id) : IRequest<Result<PackageResponse>>;

public record CreatePackageCommand(PackageUpsertRequest Request) : IRequest<Result<PackageResponse>>;

public record UpdatePackageCommand(Guid Id, PackageUpsertRequest Request) : IRequest<Result<PackageResponse>>;

public record DeletePackageCommand(Guid Id) : IRequest<Result>;

public record ListSubscriptionsQuery(
    Guid? PatientId, string? Status, string? PaymentStatus, bool? HasDebt,
    int? ExpiringWithinDays, int? BranchId, int Page, int PageSize)
    : IRequest<Result<PagedResult<SubscriptionResponse>>>;

public record GetSubscriptionQuery(Guid Id) : IRequest<Result<SubscriptionResponse>>;

public record GetPatientPackageSummaryQuery(Guid PatientId) : IRequest<Result<PackagePatientSummary>>;

public record CreateSubscriptionCommand(CreateSubscriptionRequest Request) : IRequest<Result<SubscriptionResponse>>;

public record AddSubscriptionPaymentCommand(Guid SubscriptionId, AddPaymentRequest Request) : IRequest<Result<SubscriptionResponse>>;

public record CancelSubscriptionCommand(Guid SubscriptionId, CancelSubscriptionRequest Request) : IRequest<Result<SubscriptionResponse>>;
public record ExtendSubscriptionCommand(Guid SubscriptionId, ExtendSubscriptionRequest Request) : IRequest<Result<SubscriptionResponse>>;

// ═══════════════════════════════════════════════════════════════
// Validators
// ═══════════════════════════════════════════════════════════════

public class PackageUpsertRequestValidator : AbstractValidator<PackageUpsertRequest>
{
    private static readonly string[] ValidTypes = ["VISIT", "SERVICE", "DRUG"];

    public PackageUpsertRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(255);
        RuleFor(x => x.DurationDays).GreaterThan(0).WithMessage("Thời hạn gói phải lớn hơn 0 ngày");
        RuleFor(x => x.ListPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MinDepositPercent).InclusiveBetween(0, 100).When(x => x.MinDepositPercent.HasValue);
        RuleFor(x => x.Entitlements).NotEmpty().When(x => x.IsActive)
            .WithMessage("Gói đang hoạt động phải có ít nhất 1 định mức");
        RuleForEach(x => x.Entitlements).ChildRules(e =>
        {
            e.RuleFor(i => i.ItemType).Must(t => ValidTypes.Contains(t))
                .WithMessage("Loại định mức phải là VISIT, SERVICE hoặc DRUG");
            e.RuleFor(i => i.Quantity).GreaterThan(0);
        });
    }
}

public class CreatePackageCommandValidator : AbstractValidator<CreatePackageCommand>
{
    public CreatePackageCommandValidator() => RuleFor(x => x.Request).SetValidator(new PackageUpsertRequestValidator());
}

public class UpdatePackageCommandValidator : AbstractValidator<UpdatePackageCommand>
{
    public UpdatePackageCommandValidator() => RuleFor(x => x.Request).SetValidator(new PackageUpsertRequestValidator());
}

public class CreateSubscriptionCommandValidator : AbstractValidator<CreateSubscriptionCommand>
{
    public CreateSubscriptionCommandValidator()
    {
        RuleFor(x => x.Request.PatientId).NotEmpty();
        RuleFor(x => x.Request.PackageId).NotEmpty();
        RuleFor(x => x.Request.TotalPrice).GreaterThan(0);
        RuleFor(x => x.Request.InitialPayment.Amount).GreaterThan(0)
            .WithMessage("Số tiền thu lần đầu phải lớn hơn 0");
    }
}

public class AddSubscriptionPaymentCommandValidator : AbstractValidator<AddSubscriptionPaymentCommand>
{
    public AddSubscriptionPaymentCommandValidator()
    {
        RuleFor(x => x.Request.Amount).GreaterThan(0)
            .WithErrorCode("PACKAGE_PAYMENT_INVALID_AMOUNT")
            .WithMessage("Số tiền thu phải lớn hơn 0");
    }
}

public class CancelSubscriptionCommandValidator : AbstractValidator<CancelSubscriptionCommand>
{
    public CancelSubscriptionCommandValidator()
        => RuleFor(x => x.Request.Reason).NotEmpty().WithMessage("Lý do hủy gói là bắt buộc");
}

// ═══════════════════════════════════════════════════════════════
// Handlers - Package template (Admin, FR-1201)
// ═══════════════════════════════════════════════════════════════

internal static class PackageMapper
{
    public static async Task<PackageResponse> LoadAsync(IDbConnection conn, string id, int tenantId, IDbTransaction? tx = null)
    {
        var pkg = await conn.QueryFirstAsync<dynamic>(
            @"SELECT * FROM diab_his_pkg_service_packages WHERE id=@id AND tenant_id=@tenantId AND deleted_at IS NULL",
            new { id, tenantId }, tx);

        var defs = (await conn.QueryAsync<dynamic>(
            @"SELECT * FROM diab_his_pkg_entitlement_definitions WHERE package_id=@id AND deleted_at IS NULL ORDER BY sort_order",
            new { id }, tx)).ToList();

        var entitlements = defs.Select(d => new EntitlementDefinitionResponse(
            Guid.Parse((string)d.id), (string)d.item_type, Guid.Parse((string)d.item_ref_id),
            (string)d.item_code, (string)d.item_name, (string)d.unit, (decimal)d.quantity, (int)d.sort_order)).ToList();

        // Uoc tinh gia tri thi truong cua goi = sum(quantity * don gia hien tai) - phuc vu hien thi tham khao (khong dung de tinh hoan tien tai thoi diem tao)
        decimal estimatedValue = 0;
        foreach (var d in defs)
        {
            var itemType = (string)d.item_type;
            var refId = (string)d.item_ref_id;
            var qty = (decimal)d.quantity;
            var price = itemType == "DRUG"
                ? await conn.ExecuteScalarAsync<decimal?>("SELECT price FROM diab_his_pha_drugs WHERE id=@refId", new { refId }, tx)
                : await conn.ExecuteScalarAsync<decimal?>("SELECT price FROM diab_his_bil_services WHERE id=@refId", new { refId }, tx);
            estimatedValue += (price ?? 0) * qty;
        }

        return new PackageResponse(
            Guid.Parse((string)pkg.id), (int)pkg.tenant_id, (string)pkg.code, (string)pkg.name, (string?)pkg.description,
            (int)pkg.duration_days, (decimal)pkg.list_price, (int)pkg.vat_rate,
            (decimal?)pkg.min_deposit_percent, Convert.ToBoolean(pkg.is_active),
            pkg.valid_from == null ? (DateOnly?)null : DateOnly.FromDateTime((DateTime)pkg.valid_from),
            pkg.valid_to == null ? (DateOnly?)null : DateOnly.FromDateTime((DateTime)pkg.valid_to),
            entitlements, estimatedValue, (DateTime)pkg.created_at, (DateTime)pkg.updated_at);
    }

    /// <summary>Lay ten/ma item de snapshot - tra null neu khong ton tai (BR: PACKAGE_ITEM_NOT_FOUND)</summary>
    public static async Task<(string code, string name)?> ResolveItemAsync(IDbConnection conn, string itemType, string refId, IDbTransaction? tx = null)
    {
        if (itemType == "DRUG")
        {
            var row = await conn.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT code, name FROM diab_his_pha_drugs WHERE id=@refId AND deleted_at IS NULL", new { refId }, tx);
            return row == null ? null : ((string)row.code, (string)row.name);
        }
        else
        {
            var row = await conn.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT code, name FROM diab_his_bil_services WHERE id=@refId AND deleted_at IS NULL", new { refId }, tx);
            return row == null ? null : ((string)row.code, (string)row.name);
        }
    }

    public static async Task<decimal> ResolveCurrentPriceAsync(IDbConnection conn, string itemType, string refId, IDbTransaction? tx = null)
    {
        decimal? price = itemType == "DRUG"
            ? await conn.ExecuteScalarAsync<decimal?>("SELECT price FROM diab_his_pha_drugs WHERE id=@refId", new { refId }, tx)
            : await conn.ExecuteScalarAsync<decimal?>("SELECT price FROM diab_his_bil_services WHERE id=@refId", new { refId }, tx);
        return price ?? 0;
    }
}

public class ListPackagesHandler : IRequestHandler<ListPackagesQuery, Result<PagedResult<PackageResponse>>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    public ListPackagesHandler(IDapperConnectionFactory db, ITenantProvider tenant) { _db = db; _tenant = tenant; }

    public async Task<Result<PagedResult<PackageResponse>>> Handle(ListPackagesQuery q, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var tenantId = _tenant.TenantId;
        var where = "WHERE tenant_id=@tenantId AND deleted_at IS NULL";
        var p = new DynamicParameters();
        p.Add("tenantId", tenantId);
        if (!string.IsNullOrWhiteSpace(q.Q)) { where += " AND (code LIKE @q OR name LIKE @q)"; p.Add("q", $"%{q.Q}%"); }
        if (q.IsActive.HasValue) { where += " AND is_active=@isActive"; p.Add("isActive", q.IsActive.Value ? 1 : 0); }

        var total = await conn.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM diab_his_pkg_service_packages {where}", p);
        var offset = (q.Page - 1) * q.PageSize;
        p.Add("offset", offset); p.Add("limit", q.PageSize);
        var ids = await conn.QueryAsync<string>(
            $"SELECT id FROM diab_his_pkg_service_packages {where} ORDER BY name ASC LIMIT @limit OFFSET @offset", p);

        var items = new List<PackageResponse>();
        foreach (var id in ids) items.Add(await PackageMapper.LoadAsync(conn, id, tenantId));
        return Result<PagedResult<PackageResponse>>.Success(new PagedResult<PackageResponse>(items, q.Page, q.PageSize, total));
    }
}

public class GetPackageHandler : IRequestHandler<GetPackageQuery, Result<PackageResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    public GetPackageHandler(IDapperConnectionFactory db, ITenantProvider tenant) { _db = db; _tenant = tenant; }

    public async Task<Result<PackageResponse>> Handle(GetPackageQuery q, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var exists = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM diab_his_pkg_service_packages WHERE id=@id AND tenant_id=@tenantId AND deleted_at IS NULL",
            new { id = q.Id.ToString(), tenantId = _tenant.TenantId });
        if (exists == 0) return Result<PackageResponse>.Failure("PACKAGE_NOT_FOUND", "Khong tim thay goi dinh muc");
        return Result<PackageResponse>.Success(await PackageMapper.LoadAsync(conn, q.Id.ToString(), _tenant.TenantId));
    }
}

public class CreatePackageHandler : IRequestHandler<CreatePackageCommand, Result<PackageResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _user;
    private readonly IAuditService _audit;

    public CreatePackageHandler(IDapperConnectionFactory db, ITenantProvider tenant, ICurrentUser user, IAuditService audit)
    { _db = db; _tenant = tenant; _user = user; _audit = audit; }

    public async Task<Result<PackageResponse>> Handle(CreatePackageCommand cmd, CancellationToken ct)
    {
        var req = cmd.Request;
        using var conn = _db.CreateConnection();
        conn.Open();
        var tenantId = _tenant.TenantId;

        var dup = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM diab_his_pkg_service_packages WHERE tenant_id=@tenantId AND code=@code AND deleted_at IS NULL",
            new { tenantId, code = req.Code });
        if (dup > 0) return Result<PackageResponse>.Failure("PACKAGE_CODE_DUPLICATE", "Ma goi da ton tai");

        var typeSet = req.Entitlements.Select(e => (e.ItemType, e.ItemRefId)).ToList();
        if (typeSet.Count != typeSet.Distinct().Count())
            return Result<PackageResponse>.Failure("PACKAGE_ENTITLEMENT_DUPLICATE_ITEM", "Co dong dinh muc trung item");

        using var tx = conn.BeginTransaction();
        try
        {
            var pkgId = Guid.NewGuid().ToString();
            await conn.ExecuteAsync(
                @"INSERT INTO diab_his_pkg_service_packages
                  (id, tenant_id, code, name, description, duration_days, list_price, vat_rate,
                   min_deposit_percent, is_active, valid_from, valid_to, created_at, updated_at, created_by)
                  VALUES (@pkgId, @tenantId, @code, @name, @description, @durationDays, @listPrice, @vatRate,
                   @minDepositPercent, @isActive, @validFrom, @validTo, UTC_TIMESTAMP(), UTC_TIMESTAMP(), @createdBy)",
                new
                {
                    pkgId, tenantId, code = req.Code, name = req.Name, description = req.Description,
                    durationDays = req.DurationDays, listPrice = req.ListPrice, vatRate = req.VatRate,
                    minDepositPercent = req.MinDepositPercent, isActive = req.IsActive ? 1 : 0,
                    validFrom = req.ValidFrom, validTo = req.ValidTo, createdBy = _user.UserId?.ToString()
                }, tx);

            var sortOrder = 0;
            foreach (var e in req.Entitlements)
            {
                var refId = e.ItemRefId.ToString();
                var resolved = await PackageMapper.ResolveItemAsync(conn, e.ItemType, refId, tx);
                if (resolved == null)
                {
                    tx.Rollback();
                    return Result<PackageResponse>.Failure("PACKAGE_ITEM_NOT_FOUND", $"Khong tim thay hang muc {refId}");
                }
                await conn.ExecuteAsync(
                    @"INSERT INTO diab_his_pkg_entitlement_definitions
                      (id, tenant_id, package_id, item_type, item_ref_id, item_code, item_name, unit, quantity, sort_order,
                       created_at, updated_at, created_by)
                      VALUES (UUID(), @tenantId, @pkgId, @itemType, @refId, @code, @name, @unit, @qty, @sortOrder,
                       UTC_TIMESTAMP(), UTC_TIMESTAMP(), @createdBy)",
                    new
                    {
                        tenantId, pkgId, itemType = e.ItemType, refId, code = resolved.Value.code, name = resolved.Value.name,
                        unit = e.Unit ?? (e.ItemType == "DRUG" ? "viên" : "lần"), qty = e.Quantity, sortOrder = sortOrder++,
                        createdBy = _user.UserId?.ToString()
                    }, tx);
            }

            tx.Commit();
            await _audit.LogAsync("CREATE", "diab_his_pkg_service_packages", pkgId, new { code = req.Code }, ct);
            return Result<PackageResponse>.Success(await PackageMapper.LoadAsync(conn, pkgId, tenantId));
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }
}

public class UpdatePackageHandler : IRequestHandler<UpdatePackageCommand, Result<PackageResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _user;

    public UpdatePackageHandler(IDapperConnectionFactory db, ITenantProvider tenant, ICurrentUser user)
    { _db = db; _tenant = tenant; _user = user; }

    public async Task<Result<PackageResponse>> Handle(UpdatePackageCommand cmd, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        conn.Open();
        var tenantId = _tenant.TenantId;
        var id = cmd.Id.ToString();

        var exists = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM diab_his_pkg_service_packages WHERE id=@id AND tenant_id=@tenantId AND deleted_at IS NULL",
            new { id, tenantId });
        if (exists == 0) return Result<PackageResponse>.Failure("PACKAGE_NOT_FOUND", "Khong tim thay goi dinh muc");

        // BR-1201-3: da co subscription thi khong duoc sua duration_days / entitlements
        var hasSubscription = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM diab_his_pkg_subscriptions WHERE package_id=@id AND deleted_at IS NULL", new { id });

        var current = await PackageMapper.LoadAsync(conn, id, tenantId);
        if (hasSubscription > 0)
        {
            var entitlementsChanged = current.Entitlements.Count != cmd.Request.Entitlements.Count ||
                current.DurationDays != cmd.Request.DurationDays;
            if (entitlementsChanged)
                return Result<PackageResponse>.Failure("PACKAGE_IN_USE", "Goi da co nguoi mua - khong the sua thoi han/dinh muc, chi duoc vo hieu hoa");
        }

        using var tx = conn.BeginTransaction();
        try
        {
            await conn.ExecuteAsync(
                @"UPDATE diab_his_pkg_service_packages SET name=@name, description=@description,
                  list_price=@listPrice, vat_rate=@vatRate, min_deposit_percent=@minDepositPercent,
                  is_active=@isActive, valid_from=@validFrom, valid_to=@validTo,
                  updated_at=UTC_TIMESTAMP(), updated_by=@updatedBy
                  WHERE id=@id AND tenant_id=@tenantId",
                new
                {
                    id, tenantId, name = cmd.Request.Name, description = cmd.Request.Description,
                    listPrice = cmd.Request.ListPrice, vatRate = cmd.Request.VatRate,
                    minDepositPercent = cmd.Request.MinDepositPercent, isActive = cmd.Request.IsActive ? 1 : 0,
                    validFrom = cmd.Request.ValidFrom, validTo = cmd.Request.ValidTo, updatedBy = _user.UserId?.ToString()
                }, tx);

            if (hasSubscription == 0)
            {
                await conn.ExecuteAsync("UPDATE diab_his_pkg_entitlement_definitions SET deleted_at=UTC_TIMESTAMP() WHERE package_id=@id", new { id }, tx);
                await conn.ExecuteAsync("UPDATE diab_his_pkg_service_packages SET duration_days=@d WHERE id=@id", new { id, d = cmd.Request.DurationDays }, tx);
                var sortOrder = 0;
                foreach (var e in cmd.Request.Entitlements)
                {
                    var refId = e.ItemRefId.ToString();
                    var resolved = await PackageMapper.ResolveItemAsync(conn, e.ItemType, refId, tx);
                    if (resolved == null) { tx.Rollback(); return Result<PackageResponse>.Failure("PACKAGE_ITEM_NOT_FOUND", $"Khong tim thay hang muc {refId}"); }
                    await conn.ExecuteAsync(
                        @"INSERT INTO diab_his_pkg_entitlement_definitions
                          (id, tenant_id, package_id, item_type, item_ref_id, item_code, item_name, unit, quantity, sort_order, created_at, updated_at, created_by)
                          VALUES (UUID(), @tenantId, @id, @itemType, @refId, @code, @name, @unit, @qty, @sortOrder, UTC_TIMESTAMP(), UTC_TIMESTAMP(), @createdBy)",
                        new { tenantId, id, itemType = e.ItemType, refId, code = resolved.Value.code, name = resolved.Value.name,
                              unit = e.Unit ?? (e.ItemType == "DRUG" ? "viên" : "lần"), qty = e.Quantity, sortOrder = sortOrder++, createdBy = _user.UserId?.ToString() }, tx);
                }
            }

            tx.Commit();
            return Result<PackageResponse>.Success(await PackageMapper.LoadAsync(conn, id, tenantId));
        }
        catch { tx.Rollback(); throw; }
    }
}

public class DeletePackageHandler : IRequestHandler<DeletePackageCommand, Result>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    public DeletePackageHandler(IDapperConnectionFactory db, ITenantProvider tenant) { _db = db; _tenant = tenant; }

    public async Task<Result> Handle(DeletePackageCommand cmd, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var id = cmd.Id.ToString();
        var tenantId = _tenant.TenantId;
        var activeSub = await conn.ExecuteScalarAsync<int>(
            @"SELECT COUNT(*) FROM diab_his_pkg_subscriptions
              WHERE package_id=@id AND status IN ('pending_payment','active','suspended') AND deleted_at IS NULL", new { id });
        if (activeSub > 0) return Result.Failure("PACKAGE_IN_USE", "Goi con subscription dang hoat dong - khong the xoa");

        var affected = await conn.ExecuteAsync(
            "UPDATE diab_his_pkg_service_packages SET deleted_at=UTC_TIMESTAMP(), is_active=0 WHERE id=@id AND tenant_id=@tenantId",
            new { id, tenantId });
        return affected > 0 ? Result.Success() : Result.Failure("PACKAGE_NOT_FOUND", "Khong tim thay goi dinh muc");
    }
}
