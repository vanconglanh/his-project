using Dapper;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProDiabHis.Application.Auth;
using ProDiabHis.Application.Common;
using ProDiabHis.Domain.Entities;            // PriceOverrideScope
using ProDiabHis.Domain.Entities.Pharmacy;   // DrugBranchPrice

namespace ProDiabHis.Application.Pharmacy.Drugs;

// ---- DTOs ----
// Mirror ServicePriceOverride cho THUOC (bang diab_his_pha_drug_branch_prices, migration 9185).
// drug_id dang string vi diab_his_pha_drugs.ID co the la INT (legacy) hoac CHAR(36) UUID.

public record DrugPriceOverrideResponse(
    Guid Id,
    int TenantId,
    string DrugId,
    string? DrugName,
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

public record CreateDrugPriceOverrideRequest(
    string DrugId,
    string Scope,
    int? BranchId,
    int? GroupId,
    decimal Price,
    bool IsActive,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    string? Note);

public record UpdateDrugPriceOverrideRequest(
    decimal Price,
    bool IsActive,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    string? Note);

// ---- Commands / Queries ----

public record CreateDrugPriceOverrideCommand(CreateDrugPriceOverrideRequest Request)
    : IRequest<Result<DrugPriceOverrideResponse>>;
public record UpdateDrugPriceOverrideCommand(Guid Id, UpdateDrugPriceOverrideRequest Request)
    : IRequest<Result<DrugPriceOverrideResponse>>;
public record DeleteDrugPriceOverrideCommand(Guid Id) : IRequest<Result>;
public record ListDrugPriceOverridesQuery(string? DrugId, int? BranchId, int? GroupId, string? Scope, int Page, int PageSize)
    : IRequest<Result<PagedResult<DrugPriceOverrideResponse>>>;
public record GetDrugPriceOverrideQuery(Guid Id) : IRequest<Result<DrugPriceOverrideResponse>>;

// ---- Validators ----

public class CreateDrugPriceOverrideValidator : AbstractValidator<CreateDrugPriceOverrideRequest>
{
    public CreateDrugPriceOverrideValidator()
    {
        RuleFor(x => x.DrugId).NotEmpty();
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

public class UpdateDrugPriceOverrideValidator : AbstractValidator<UpdateDrugPriceOverrideRequest>
{
    public UpdateDrugPriceOverrideValidator()
    {
        RuleFor(x => x.Price).GreaterThan(0).WithMessage("Gia phai lon hon 0");
        RuleFor(x => x.EffectiveTo).GreaterThanOrEqualTo(x => x.EffectiveFrom)
            .When(x => x.EffectiveTo.HasValue)
            .WithMessage("Ngay ket thuc phai sau hoac bang ngay bat dau");
    }
}

// ---- Mapper ----

internal static class DrugPriceOverrideMapper
{
    public static DrugPriceOverrideResponse ToDto(DrugBranchPrice p, string? drugName = null) => new(
        p.Id, p.TenantId, p.DrugId, drugName, p.Scope, p.BranchId, p.GroupId,
        p.Price, p.IsActive, p.EffectiveFrom, p.EffectiveTo, p.Note, p.CreatedAt, p.CreatedBy);
}

// ---- Handlers ----
// Quyen drug.price_override (mirror service.price_override) kiem tra trong tung handler (defense-in-depth).

public class CreateDrugPriceOverrideHandler
    : IRequestHandler<CreateDrugPriceOverrideCommand, Result<DrugPriceOverrideResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly IDapperConnectionFactory _dapper;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _user;
    private readonly IPermissionChecker _permChecker;

    public CreateDrugPriceOverrideHandler(
        IApplicationDbContext db, IDapperConnectionFactory dapper, ITenantProvider tenant,
        ICurrentUser user, IPermissionChecker permChecker)
    {
        _db = db; _dapper = dapper; _tenant = tenant; _user = user; _permChecker = permChecker;
    }

    public async Task<Result<DrugPriceOverrideResponse>> Handle(CreateDrugPriceOverrideCommand cmd, CancellationToken ct)
    {
        if (!_permChecker.HasPermission("drug.price_override"))
            return Result<DrugPriceOverrideResponse>.Failure("FORBIDDEN", "Ban khong co quyen thao tac gia override thuoc");

        var req = cmd.Request;
        var tenantId = _tenant.TenantId;

        // Kiem tra thuoc ton tai (bang diab_his_pha_drugs chay bang Dapper - khong dung EF Drug entity)
        using (var conn = _dapper.CreateConnection())
        {
            var drugExists = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM diab_his_pha_drugs WHERE ID=@drugId AND tenant_id=@tenantId AND deleted_at IS NULL",
                new { drugId = req.DrugId, tenantId });
            if (drugExists == 0)
                return Result<DrugPriceOverrideResponse>.Failure("DRUG_NOT_FOUND", "Khong tim thay thuoc");
        }

        var conflict = await FindOverlapAsync(_db, tenantId, req.DrugId, req.Scope, req.BranchId, req.GroupId,
            req.EffectiveFrom, req.EffectiveTo, excludeId: null, ct);
        if (conflict != null)
            return Result<DrugPriceOverrideResponse>.Failure("PRICE_OVERLAP",
                $"Da co gia override khac cho thuoc nay trong khoang thoi gian giao nhau (ma ban ghi xung dot: {conflict.Id})");

        var entity = new DrugBranchPrice
        {
            TenantId = tenantId,
            DrugId = req.DrugId,
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
        _db.DrugBranchPrices.Add(entity);
        await _db.SaveChangesAsync(ct);

        return Result<DrugPriceOverrideResponse>.Success(DrugPriceOverrideMapper.ToDto(entity));
    }

    internal static async Task<DrugBranchPrice?> FindOverlapAsync(
        IApplicationDbContext db, int tenantId, string drugId, string scope, int? branchId, int? groupId,
        DateOnly from, DateOnly? to, Guid? excludeId, CancellationToken ct)
    {
        var query = db.DrugBranchPrices.Where(p =>
            p.TenantId == tenantId && p.DrugId == drugId && p.Scope == scope && p.DeletedAt == null
            && (scope == PriceOverrideScope.Branch ? p.BranchId == branchId : p.GroupId == groupId));

        if (excludeId.HasValue) query = query.Where(p => p.Id != excludeId.Value);

        var candidates = await query.ToListAsync(ct);
        return candidates.FirstOrDefault(p =>
            p.EffectiveFrom <= (to ?? DateOnly.MaxValue) && (p.EffectiveTo ?? DateOnly.MaxValue) >= from);
    }
}

public class UpdateDrugPriceOverrideHandler
    : IRequestHandler<UpdateDrugPriceOverrideCommand, Result<DrugPriceOverrideResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _user;
    private readonly IPermissionChecker _permChecker;

    public UpdateDrugPriceOverrideHandler(
        IApplicationDbContext db, ITenantProvider tenant, ICurrentUser user, IPermissionChecker permChecker)
    {
        _db = db; _tenant = tenant; _user = user; _permChecker = permChecker;
    }

    public async Task<Result<DrugPriceOverrideResponse>> Handle(UpdateDrugPriceOverrideCommand cmd, CancellationToken ct)
    {
        if (!_permChecker.HasPermission("drug.price_override"))
            return Result<DrugPriceOverrideResponse>.Failure("FORBIDDEN", "Ban khong co quyen thao tac gia override thuoc");

        var entity = await _db.DrugBranchPrices
            .FirstOrDefaultAsync(p => p.Id == cmd.Id && p.TenantId == _tenant.TenantId && p.DeletedAt == null, ct);
        if (entity == null)
            return Result<DrugPriceOverrideResponse>.Failure("PRICE_OVERRIDE_NOT_FOUND", "Khong tim thay gia override");

        var req = cmd.Request;
        var conflict = await CreateDrugPriceOverrideHandler.FindOverlapAsync(
            _db, _tenant.TenantId, entity.DrugId, entity.Scope, entity.BranchId, entity.GroupId,
            req.EffectiveFrom, req.EffectiveTo, excludeId: entity.Id, ct);
        if (conflict != null)
            return Result<DrugPriceOverrideResponse>.Failure("PRICE_OVERLAP",
                $"Da co gia override khac cho thuoc nay trong khoang thoi gian giao nhau (ma ban ghi xung dot: {conflict.Id})");

        entity.Price = req.Price;
        entity.IsActive = req.IsActive;
        entity.EffectiveFrom = req.EffectiveFrom;
        entity.EffectiveTo = req.EffectiveTo;
        entity.Note = req.Note;
        entity.UpdatedBy = _user.UserId;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return Result<DrugPriceOverrideResponse>.Success(DrugPriceOverrideMapper.ToDto(entity));
    }
}

public class DeleteDrugPriceOverrideHandler : IRequestHandler<DeleteDrugPriceOverrideCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _user;
    private readonly IPermissionChecker _permChecker;

    public DeleteDrugPriceOverrideHandler(
        IApplicationDbContext db, ITenantProvider tenant, ICurrentUser user, IPermissionChecker permChecker)
    {
        _db = db; _tenant = tenant; _user = user; _permChecker = permChecker;
    }

    public async Task<Result> Handle(DeleteDrugPriceOverrideCommand cmd, CancellationToken ct)
    {
        if (!_permChecker.HasPermission("drug.price_override"))
            return Result.Failure("FORBIDDEN", "Ban khong co quyen thao tac gia override thuoc");

        var entity = await _db.DrugBranchPrices
            .FirstOrDefaultAsync(p => p.Id == cmd.Id && p.TenantId == _tenant.TenantId && p.DeletedAt == null, ct);
        if (entity == null)
            return Result.Failure("PRICE_OVERRIDE_NOT_FOUND", "Khong tim thay gia override");

        entity.DeletedAt = DateTime.UtcNow;
        entity.UpdatedBy = _user.UserId;
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public class GetDrugPriceOverrideHandler : IRequestHandler<GetDrugPriceOverrideQuery, Result<DrugPriceOverrideResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantProvider _tenant;

    public GetDrugPriceOverrideHandler(IApplicationDbContext db, ITenantProvider tenant)
    {
        _db = db; _tenant = tenant;
    }

    public async Task<Result<DrugPriceOverrideResponse>> Handle(GetDrugPriceOverrideQuery query, CancellationToken ct)
    {
        var entity = await _db.DrugBranchPrices
            .FirstOrDefaultAsync(p => p.Id == query.Id && p.TenantId == _tenant.TenantId && p.DeletedAt == null, ct);
        if (entity == null)
            return Result<DrugPriceOverrideResponse>.Failure("PRICE_OVERRIDE_NOT_FOUND", "Khong tim thay gia override");
        return Result<DrugPriceOverrideResponse>.Success(DrugPriceOverrideMapper.ToDto(entity));
    }
}

public class ListDrugPriceOverridesHandler
    : IRequestHandler<ListDrugPriceOverridesQuery, Result<PagedResult<DrugPriceOverrideResponse>>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;

    public ListDrugPriceOverridesHandler(IDapperConnectionFactory db, ITenantProvider tenant)
    {
        _db = db; _tenant = tenant;
    }

    public async Task<Result<PagedResult<DrugPriceOverrideResponse>>> Handle(
        ListDrugPriceOverridesQuery query, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var where = "WHERE p.tenant_id = @tenantId AND p.deleted_at IS NULL";
        var pr = new DynamicParameters();
        pr.Add("tenantId", _tenant.TenantId);

        if (!string.IsNullOrEmpty(query.DrugId)) { where += " AND p.drug_id = @did"; pr.Add("did", query.DrugId); }
        if (query.BranchId.HasValue) { where += " AND p.branch_id = @bid"; pr.Add("bid", query.BranchId); }
        if (query.GroupId.HasValue) { where += " AND p.group_id = @gid"; pr.Add("gid", query.GroupId); }
        if (!string.IsNullOrEmpty(query.Scope)) { where += " AND p.scope = @scope"; pr.Add("scope", query.Scope); }

        var total = await conn.ExecuteScalarAsync<int>(
            $"SELECT COUNT(*) FROM diab_his_pha_drug_branch_prices p {where}", pr);
        var offset = (query.Page - 1) * query.PageSize;
        pr.Add("limit", query.PageSize); pr.Add("offset", offset);

        var rows = await conn.QueryAsync<dynamic>(
            $@"SELECT p.*, d.name_vi AS drug_name FROM diab_his_pha_drug_branch_prices p
               LEFT JOIN diab_his_pha_drugs d ON d.ID = p.drug_id
               {where} ORDER BY p.created_at DESC LIMIT @limit OFFSET @offset", pr);

        var items = rows.Select(r => new DrugPriceOverrideResponse(
            Guid.Parse((string)r.id.ToString()),
            (int)r.tenant_id,
            (string)r.drug_id.ToString(),
            (string?)r.drug_name,
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

        return Result<PagedResult<DrugPriceOverrideResponse>>.Success(
            new PagedResult<DrugPriceOverrideResponse>(items, query.Page, query.PageSize, total));
    }
}
