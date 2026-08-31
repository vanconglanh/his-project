using Dapper;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProDiabHis.Application.Auth;
using ProDiabHis.Application.Common;
using ProDiabHis.Domain.Entities;

namespace ProDiabHis.Application.Billing;

// ---- DTOs ----

public record ServicePriceOverrideResponse(
    Guid Id,
    int TenantId,
    Guid ServiceId,
    string? ServiceName,
    string Scope,
    int? BranchId,
    int? GroupId,
    decimal Price,
    bool IsActive,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    string? Note,
    DateTime CreatedAt,
    Guid? CreatedBy);

public record CreateServicePriceOverrideRequest(
    Guid ServiceId,
    string Scope,
    int? BranchId,
    int? GroupId,
    decimal Price,
    bool IsActive,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    string? Note);

public record UpdateServicePriceOverrideRequest(
    decimal Price,
    bool IsActive,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    string? Note);

// ---- Commands / Queries ----

public record CreateServicePriceOverrideCommand(CreateServicePriceOverrideRequest Request)
    : IRequest<Result<ServicePriceOverrideResponse>>;
public record UpdateServicePriceOverrideCommand(Guid Id, UpdateServicePriceOverrideRequest Request)
    : IRequest<Result<ServicePriceOverrideResponse>>;
public record DeleteServicePriceOverrideCommand(Guid Id) : IRequest<Result>;
public record ListServicePriceOverridesQuery(Guid? ServiceId, int? BranchId, int? GroupId, string? Scope, int Page, int PageSize)
    : IRequest<Result<PagedResult<ServicePriceOverrideResponse>>>;
public record GetServicePriceOverrideQuery(Guid Id) : IRequest<Result<ServicePriceOverrideResponse>>;

// ---- Validators ----

public class CreateServicePriceOverrideValidator : AbstractValidator<CreateServicePriceOverrideRequest>
{
    public CreateServicePriceOverrideValidator()
    {
        RuleFor(x => x.ServiceId).NotEmpty();
        RuleFor(x => x.Scope).Must(s => s == PriceOverrideScope.Branch || s == PriceOverrideScope.Group)
            .WithMessage("Pham vi (scope) phai la BRANCH hoac GROUP");
        RuleFor(x => x.BranchId).NotNull().When(x => x.Scope == PriceOverrideScope.Branch)
            .WithMessage("Phai chon chi nhanh khi pham vi la BRANCH");
        RuleFor(x => x.GroupId).NotNull().When(x => x.Scope == PriceOverrideScope.Group)
            .WithMessage("Phai chon nhom chi nhanh khi pham vi la GROUP");
        RuleFor(x => x.Price).GreaterThan(0).WithMessage("Gia phai lon hon 0");
        RuleFor(x => x.EffectiveTo).GreaterThanOrEqualTo(x => x.EffectiveFrom)
            .When(x => x.EffectiveTo.HasValue)
            .WithMessage("Ngay ket thuc phai sau hoac bang ngay bat dau");
    }
}

public class UpdateServicePriceOverrideValidator : AbstractValidator<UpdateServicePriceOverrideRequest>
{
    public UpdateServicePriceOverrideValidator()
    {
        RuleFor(x => x.Price).GreaterThan(0).WithMessage("Gia phai lon hon 0");
        RuleFor(x => x.EffectiveTo).GreaterThanOrEqualTo(x => x.EffectiveFrom)
            .When(x => x.EffectiveTo.HasValue)
            .WithMessage("Ngay ket thuc phai sau hoac bang ngay bat dau");
    }
}

// BUG-04: lop boc cap Command — thieu 2 lop nay thi 2 validator tren KHONG BAO GIO chay
// (da chung minh: tao duoc override gia -999.999d, 201). Chan gia <= 0 lot qua.
public class CreateServicePriceOverrideCommandValidator : AbstractValidator<CreateServicePriceOverrideCommand>
{
    public CreateServicePriceOverrideCommandValidator()
    {
        RuleFor(x => x.Request).NotNull().WithMessage("Thiếu dữ liệu giá override")
            .SetValidator(new CreateServicePriceOverrideValidator());
    }
}

public class UpdateServicePriceOverrideCommandValidator : AbstractValidator<UpdateServicePriceOverrideCommand>
{
    public UpdateServicePriceOverrideCommandValidator()
    {
        RuleFor(x => x.Request).NotNull().WithMessage("Thiếu dữ liệu giá override")
            .SetValidator(new UpdateServicePriceOverrideValidator());
    }
}

// ---- Mapper ----

internal static class ServicePriceOverrideMapper
{
    public static ServicePriceOverrideResponse ToDto(ServiceBranchPrice p, string? serviceName = null) => new(
        p.Id, p.TenantId, p.ServiceId, serviceName, p.Scope, p.BranchId, p.GroupId,
        p.Price, p.IsActive, p.EffectiveFrom, p.EffectiveTo, p.Note, p.CreatedAt, p.CreatedBy);
}

// ---- Handlers ----
// Ghi chu: quyen service.price_override (BR-74: chi admin/quan_ly_vung) duoc kiem tra truc tiep
// trong tung handler qua IPermissionChecker (defense-in-depth, bo sung cho [RequirePermission] o controller).

public class CreateServicePriceOverrideHandler
    : IRequestHandler<CreateServicePriceOverrideCommand, Result<ServicePriceOverrideResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _user;
    private readonly IPermissionChecker _permChecker;

    public CreateServicePriceOverrideHandler(
        IApplicationDbContext db, ITenantProvider tenant, ICurrentUser user, IPermissionChecker permChecker)
    {
        _db = db; _tenant = tenant; _user = user; _permChecker = permChecker;
    }

    public async Task<Result<ServicePriceOverrideResponse>> Handle(CreateServicePriceOverrideCommand cmd, CancellationToken ct)
    {
        if (!_permChecker.HasPermission("service.price_override"))
            return Result<ServicePriceOverrideResponse>.Failure("FORBIDDEN", "Ban khong co quyen thao tac gia override dich vu");

        var req = cmd.Request;
        var tenantId = _tenant.TenantId;

        var serviceExists = await _db.BillingServices
            .AnyAsync(s => s.Id == req.ServiceId && s.TenantId == tenantId && s.DeletedAt == null, ct);
        if (!serviceExists)
            return Result<ServicePriceOverrideResponse>.Failure("SERVICE_NOT_FOUND", "Khong tim thay dich vu");

        // BR-72: khong cho phep 2 override cung scope + cung service co khoang hieu luc GIAO NHAU
        var conflict = await FindOverlapAsync(_db, tenantId, req.ServiceId, req.Scope, req.BranchId, req.GroupId,
            req.EffectiveFrom, req.EffectiveTo, excludeId: null, ct);
        if (conflict != null)
            return Result<ServicePriceOverrideResponse>.Failure("PRICE_OVERLAP",
                $"Da co gia override khac cho dich vu nay trong khoang thoi gian giao nhau (ma ban ghi xung dot: {conflict.Id})");

        var entity = new ServiceBranchPrice
        {
            TenantId = tenantId,
            ServiceId = req.ServiceId,
            Scope = req.Scope,
            BranchId = req.Scope == PriceOverrideScope.Branch ? req.BranchId : null,
            GroupId = req.Scope == PriceOverrideScope.Group ? req.GroupId : null,
            Price = req.Price,
            IsActive = req.IsActive,
            EffectiveFrom = req.EffectiveFrom,
            EffectiveTo = req.EffectiveTo,
            Note = req.Note,
            CreatedBy = _user.UserId,
            UpdatedBy = _user.UserId
        };
        _db.ServiceBranchPrices.Add(entity);
        await _db.SaveChangesAsync(ct);

        return Result<ServicePriceOverrideResponse>.Success(ServicePriceOverrideMapper.ToDto(entity));
    }

    internal static async Task<ServiceBranchPrice?> FindOverlapAsync(
        IApplicationDbContext db, int tenantId, Guid serviceId, string scope, int? branchId, int? groupId,
        DateOnly from, DateOnly? to, Guid? excludeId, CancellationToken ct)
    {
        var query = db.ServiceBranchPrices.Where(p =>
            p.TenantId == tenantId && p.ServiceId == serviceId && p.Scope == scope && p.DeletedAt == null
            && (scope == PriceOverrideScope.Branch ? p.BranchId == branchId : p.GroupId == groupId));

        if (excludeId.HasValue) query = query.Where(p => p.Id != excludeId.Value);

        var candidates = await query.ToListAsync(ct);
        return candidates.FirstOrDefault(p =>
            p.EffectiveFrom <= (to ?? DateOnly.MaxValue) && (p.EffectiveTo ?? DateOnly.MaxValue) >= from);
    }
}

public class UpdateServicePriceOverrideHandler
    : IRequestHandler<UpdateServicePriceOverrideCommand, Result<ServicePriceOverrideResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _user;
    private readonly IPermissionChecker _permChecker;

    public UpdateServicePriceOverrideHandler(
        IApplicationDbContext db, ITenantProvider tenant, ICurrentUser user, IPermissionChecker permChecker)
    {
        _db = db; _tenant = tenant; _user = user; _permChecker = permChecker;
    }

    public async Task<Result<ServicePriceOverrideResponse>> Handle(UpdateServicePriceOverrideCommand cmd, CancellationToken ct)
    {
        if (!_permChecker.HasPermission("service.price_override"))
            return Result<ServicePriceOverrideResponse>.Failure("FORBIDDEN", "Ban khong co quyen thao tac gia override dich vu");

        var entity = await _db.ServiceBranchPrices
            .FirstOrDefaultAsync(p => p.Id == cmd.Id && p.TenantId == _tenant.TenantId && p.DeletedAt == null, ct);
        if (entity == null)
            return Result<ServicePriceOverrideResponse>.Failure("PRICE_OVERRIDE_NOT_FOUND", "Khong tim thay gia override");

        var req = cmd.Request;
        var conflict = await CreateServicePriceOverrideHandler.FindOverlapAsync(
            _db, _tenant.TenantId, entity.ServiceId, entity.Scope, entity.BranchId, entity.GroupId,
            req.EffectiveFrom, req.EffectiveTo, excludeId: entity.Id, ct);
        if (conflict != null)
            return Result<ServicePriceOverrideResponse>.Failure("PRICE_OVERLAP",
                $"Da co gia override khac cho dich vu nay trong khoang thoi gian giao nhau (ma ban ghi xung dot: {conflict.Id})");

        entity.Price = req.Price;
        entity.IsActive = req.IsActive;
        entity.EffectiveFrom = req.EffectiveFrom;
        entity.EffectiveTo = req.EffectiveTo;
        entity.Note = req.Note;
        entity.UpdatedBy = _user.UserId;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return Result<ServicePriceOverrideResponse>.Success(ServicePriceOverrideMapper.ToDto(entity));
    }
}

public class DeleteServicePriceOverrideHandler : IRequestHandler<DeleteServicePriceOverrideCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _user;
    private readonly IPermissionChecker _permChecker;

    public DeleteServicePriceOverrideHandler(
        IApplicationDbContext db, ITenantProvider tenant, ICurrentUser user, IPermissionChecker permChecker)
    {
        _db = db; _tenant = tenant; _user = user; _permChecker = permChecker;
    }

    public async Task<Result> Handle(DeleteServicePriceOverrideCommand cmd, CancellationToken ct)
    {
        if (!_permChecker.HasPermission("service.price_override"))
            return Result.Failure("FORBIDDEN", "Ban khong co quyen thao tac gia override dich vu");

        var entity = await _db.ServiceBranchPrices
            .FirstOrDefaultAsync(p => p.Id == cmd.Id && p.TenantId == _tenant.TenantId && p.DeletedAt == null, ct);
        if (entity == null)
            return Result.Failure("PRICE_OVERRIDE_NOT_FOUND", "Khong tim thay gia override");

        entity.DeletedAt = DateTime.UtcNow;
        entity.UpdatedBy = _user.UserId;
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public class GetServicePriceOverrideHandler : IRequestHandler<GetServicePriceOverrideQuery, Result<ServicePriceOverrideResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantProvider _tenant;

    public GetServicePriceOverrideHandler(IApplicationDbContext db, ITenantProvider tenant)
    {
        _db = db; _tenant = tenant;
    }

    public async Task<Result<ServicePriceOverrideResponse>> Handle(GetServicePriceOverrideQuery query, CancellationToken ct)
    {
        var entity = await _db.ServiceBranchPrices
            .FirstOrDefaultAsync(p => p.Id == query.Id && p.TenantId == _tenant.TenantId && p.DeletedAt == null, ct);
        if (entity == null)
            return Result<ServicePriceOverrideResponse>.Failure("PRICE_OVERRIDE_NOT_FOUND", "Khong tim thay gia override");
        return Result<ServicePriceOverrideResponse>.Success(ServicePriceOverrideMapper.ToDto(entity));
    }
}

public class ListServicePriceOverridesHandler
    : IRequestHandler<ListServicePriceOverridesQuery, Result<PagedResult<ServicePriceOverrideResponse>>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;

    public ListServicePriceOverridesHandler(IDapperConnectionFactory db, ITenantProvider tenant)
    {
        _db = db; _tenant = tenant;
    }

    public async Task<Result<PagedResult<ServicePriceOverrideResponse>>> Handle(
        ListServicePriceOverridesQuery query, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var where = "WHERE p.tenant_id = @tenantId AND p.deleted_at IS NULL";
        var pr = new DynamicParameters();
        pr.Add("tenantId", _tenant.TenantId);

        if (query.ServiceId.HasValue) { where += " AND p.service_id = @sid"; pr.Add("sid", query.ServiceId.ToString()); }
        if (query.BranchId.HasValue) { where += " AND p.branch_id = @bid"; pr.Add("bid", query.BranchId); }
        if (query.GroupId.HasValue) { where += " AND p.group_id = @gid"; pr.Add("gid", query.GroupId); }
        if (!string.IsNullOrEmpty(query.Scope)) { where += " AND p.scope = @scope"; pr.Add("scope", query.Scope); }

        var total = await conn.ExecuteScalarAsync<int>(
            $"SELECT COUNT(*) FROM diab_his_bil_service_branch_prices p {where}", pr);
        var offset = (query.Page - 1) * query.PageSize;
        pr.Add("limit", query.PageSize); pr.Add("offset", offset);

        var rows = await conn.QueryAsync<dynamic>(
            $@"SELECT p.*, s.name AS service_name FROM diab_his_bil_service_branch_prices p
               LEFT JOIN diab_his_bil_services s ON s.id = p.service_id
               {where} ORDER BY p.created_at DESC LIMIT @limit OFFSET @offset", pr);

        var items = rows.Select(r => new ServicePriceOverrideResponse(
            Guid.Parse((string)r.id.ToString()),
            (int)r.tenant_id,
            Guid.Parse((string)r.service_id.ToString()),
            (string?)r.service_name,
            (string)r.scope,
            r.branch_id == null ? null : (int?)r.branch_id,
            r.group_id == null ? null : (int?)r.group_id,
            (decimal)r.price,
            Convert.ToBoolean(r.is_active),
            DateOnly.FromDateTime((DateTime)r.effective_from),
            r.effective_to == null ? null : (DateOnly?)DateOnly.FromDateTime((DateTime)r.effective_to),
            (string?)r.note,
            (DateTime)r.created_at,
            r.created_by == null ? (Guid?)null : Guid.Parse((string)r.created_by.ToString())
        )).ToList();

        return Result<PagedResult<ServicePriceOverrideResponse>>.Success(
            new PagedResult<ServicePriceOverrideResponse>(items, query.Page, query.PageSize, total));
    }
}
