using Dapper;
using FluentValidation;
using MediatR;
using ProDiabHis.Application.Common;
using ProDiabHis.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using ProDiabHis.Application.Auth;

namespace ProDiabHis.Application.Billing;

// ---- Queries ----

public record ListServicesQuery(
    string? Q, string? Category, bool? IsActive,
    int Page, int PageSize)
    : IRequest<Result<PagedResult<ServiceResponse>>>;

public record GetServiceQuery(Guid Id) : IRequest<Result<ServiceResponse>>;

public record SearchServicesQuery(string Q) : IRequest<Result<List<ServiceResponse>>>;

public record ListServicePackagesQuery(string? Q, bool? IsActive)
    : IRequest<Result<PagedResult<ServicePackageResponse>>>;

public record GetServicePackageQuery(Guid Id) : IRequest<Result<ServicePackageResponse>>;

// ---- Commands ----

public record CreateServiceCommand(ServiceUpsertRequest Request)
    : IRequest<Result<ServiceResponse>>;

public record UpdateServiceCommand(Guid Id, ServiceUpsertRequest Request)
    : IRequest<Result<ServiceResponse>>;

public record DeleteServiceCommand(Guid Id) : IRequest<Result>;

public record CreateServicePackageCommand(ServicePackageUpsertRequest Request)
    : IRequest<Result<ServicePackageResponse>>;

public record UpdateServicePackageCommand(Guid Id, ServicePackageUpsertRequest Request)
    : IRequest<Result<ServicePackageResponse>>;

public record DeleteServicePackageCommand(Guid Id) : IRequest<Result>;

public record ImportServicesFromExcelCommand(Stream FileStream)
    : IRequest<Result<ImportResultResponse>>;

// ---- Validators ----

public class ServiceUpsertRequestValidator : AbstractValidator<ServiceUpsertRequest>
{
    private static readonly string[] ValidCategories = ["CONSULTATION", "PROCEDURE", "LAB", "RAD", "PHARMACY", "OTHER"];
    private static readonly int[] ValidVatRates = [0, 5, 8, 10];

    public ServiceUpsertRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Category).Must(c => ValidCategories.Contains(c))
            .WithMessage("Category phai la: " + string.Join(", ", ValidCategories));
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        RuleFor(x => x.VatRate).Must(r => ValidVatRates.Contains(r))
            .WithMessage("VatRate phai la 0, 5, 8 hoac 10");
        RuleFor(x => x.BhytMaxAmount).GreaterThanOrEqualTo(0)
            .When(x => x.BhytMaxAmount.HasValue)
            .WithMessage("Muc BHYT toi da khong duoc am");
    }
}

// Command-level validators: noi ServiceUpsertRequestValidator vao Command de
// ValidationBehavior (chay tren TRequest = Command) thuc su goi rule cua DTO long.
public class CreateServiceCommandValidator : AbstractValidator<CreateServiceCommand>
{
    public CreateServiceCommandValidator()
        => RuleFor(x => x.Request).NotNull().SetValidator(new ServiceUpsertRequestValidator());
}

public class UpdateServiceCommandValidator : AbstractValidator<UpdateServiceCommand>
{
    public UpdateServiceCommandValidator()
        => RuleFor(x => x.Request).NotNull().SetValidator(new ServiceUpsertRequestValidator());
}

// ---- Handlers ----

public class ListServicesHandler : IRequestHandler<ListServicesQuery, Result<PagedResult<ServiceResponse>>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;

    public ListServicesHandler(IDapperConnectionFactory db, ITenantProvider tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<Result<PagedResult<ServiceResponse>>> Handle(
        ListServicesQuery query, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var tenantId = _tenant.TenantId;

        var where = "WHERE tenant_id = @tenantId AND deleted_at IS NULL";
        var p = new DynamicParameters();
        p.Add("tenantId", tenantId);

        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            where += " AND (code LIKE @q OR name LIKE @q)";
            p.Add("q", $"%{query.Q}%");
        }
        if (!string.IsNullOrWhiteSpace(query.Category))
        {
            where += " AND category = @category";
            p.Add("category", query.Category);
        }
        if (query.IsActive.HasValue)
        {
            where += " AND is_active = @isActive";
            p.Add("isActive", query.IsActive.Value ? 1 : 0);
        }

        var countSql = $"SELECT COUNT(*) FROM diab_his_bil_services {where}";
        var total = await conn.ExecuteScalarAsync<int>(countSql, p);

        var offset = (query.Page - 1) * query.PageSize;
        var dataSql = $@"SELECT id, tenant_id, code, name, category, price, vat_rate, bhyt_code,
            bhyt_max_amount, is_active, created_at, updated_at
            FROM diab_his_bil_services {where}
            ORDER BY name ASC LIMIT @limit OFFSET @offset";
        p.Add("limit", query.PageSize);
        p.Add("offset", offset);

        var rows = await conn.QueryAsync<dynamic>(dataSql, p);
        var items = rows.Select(MapService).ToList();
        return Result<PagedResult<ServiceResponse>>.Success(
            new PagedResult<ServiceResponse>(items, query.Page, query.PageSize, total));
    }

    internal static ServiceResponse MapService(dynamic r) => new(
        Guid.Parse((string)r.id),
        (int)r.tenant_id,
        (string)r.code,
        (string)r.name,
        (string)r.category,
        (decimal)r.price,
        (int)r.vat_rate,
        (string?)r.bhyt_code,
        (decimal?)r.bhyt_max_amount,
        Convert.ToBoolean(r.is_active),
        (DateTime)r.created_at,
        (DateTime)r.updated_at);
}

public class GetServiceHandler : IRequestHandler<GetServiceQuery, Result<ServiceResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;

    public GetServiceHandler(IDapperConnectionFactory db, ITenantProvider tenant)
    {
        _db = db; _tenant = tenant;
    }

    public async Task<Result<ServiceResponse>> Handle(GetServiceQuery query, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var row = await conn.QueryFirstOrDefaultAsync<dynamic>(
            @"SELECT id, tenant_id, code, name, category, price, vat_rate, bhyt_code,
              bhyt_max_amount, is_active, created_at, updated_at
              FROM diab_his_bil_services
              WHERE id = @id AND tenant_id = @tenantId AND deleted_at IS NULL",
            new { id = query.Id.ToString(), tenantId = _tenant.TenantId });

        if (row == null) return Result<ServiceResponse>.Failure("SERVICE_NOT_FOUND", "Khong tim thay dich vu");
        return Result<ServiceResponse>.Success(ListServicesHandler.MapService(row));
    }
}

public class SearchServicesHandler : IRequestHandler<SearchServicesQuery, Result<List<ServiceResponse>>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;

    public SearchServicesHandler(IDapperConnectionFactory db, ITenantProvider tenant)
    {
        _db = db; _tenant = tenant;
    }

    public async Task<Result<List<ServiceResponse>>> Handle(SearchServicesQuery query, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<dynamic>(
            @"SELECT id, tenant_id, code, name, category, price, vat_rate, bhyt_code,
              bhyt_max_amount, is_active, created_at, updated_at
              FROM diab_his_bil_services
              WHERE tenant_id = @tenantId AND deleted_at IS NULL AND is_active = 1
                AND (code LIKE @q OR name LIKE @q)
              ORDER BY name ASC LIMIT 20",
            new { tenantId = _tenant.TenantId, q = $"%{query.Q}%" });

        return Result<List<ServiceResponse>>.Success(rows.Select(ListServicesHandler.MapService).ToList());
    }
}

public class CreateServiceHandler : IRequestHandler<CreateServiceCommand, Result<ServiceResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _user;

    public CreateServiceHandler(IApplicationDbContext db, ITenantProvider tenant, ICurrentUser user)
    {
        _db = db; _tenant = tenant; _user = user;
    }

    public async Task<Result<ServiceResponse>> Handle(CreateServiceCommand cmd, CancellationToken ct)
    {
        var exists = await _db.BillingServices
            .AnyAsync(s => s.TenantId == _tenant.TenantId && s.Code == cmd.Request.Code && s.DeletedAt == null, ct);
        if (exists) return Result<ServiceResponse>.Failure("SERVICE_CODE_EXISTS", "Ma dich vu da ton tai");

        var svc = new BillingService
        {
            TenantId = _tenant.TenantId,
            Code = cmd.Request.Code,
            Name = cmd.Request.Name,
            Category = cmd.Request.Category,
            Price = cmd.Request.Price,
            VatRate = cmd.Request.VatRate,
            BhytCode = cmd.Request.BhytCode,
            BhytMaxAmount = cmd.Request.BhytMaxAmount,
            IsActive = cmd.Request.IsActive,
            CreatedBy = _user.UserId
        };
        _db.BillingServices.Add(svc);
        await _db.SaveChangesAsync(ct);

        return Result<ServiceResponse>.Success(ToDto(svc));
    }

    internal static ServiceResponse ToDto(BillingService s) => new(
        s.Id, s.TenantId, s.Code, s.Name, s.Category,
        s.Price, s.VatRate, s.BhytCode, s.BhytMaxAmount, s.IsActive, s.CreatedAt, s.UpdatedAt);
}

public class UpdateServiceHandler : IRequestHandler<UpdateServiceCommand, Result<ServiceResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _user;

    public UpdateServiceHandler(IApplicationDbContext db, ITenantProvider tenant, ICurrentUser user)
    {
        _db = db; _tenant = tenant; _user = user;
    }

    public async Task<Result<ServiceResponse>> Handle(UpdateServiceCommand cmd, CancellationToken ct)
    {
        var svc = await _db.BillingServices
            .FirstOrDefaultAsync(s => s.Id == cmd.Id && s.TenantId == _tenant.TenantId && s.DeletedAt == null, ct);
        if (svc == null) return Result<ServiceResponse>.Failure("SERVICE_NOT_FOUND", "Khong tim thay dich vu");

        svc.Code = cmd.Request.Code;
        svc.Name = cmd.Request.Name;
        svc.Category = cmd.Request.Category;
        svc.Price = cmd.Request.Price;
        svc.VatRate = cmd.Request.VatRate;
        svc.BhytCode = cmd.Request.BhytCode;
        svc.BhytMaxAmount = cmd.Request.BhytMaxAmount;
        svc.IsActive = cmd.Request.IsActive;
        svc.UpdatedBy = _user.UserId;
        await _db.SaveChangesAsync(ct);

        return Result<ServiceResponse>.Success(CreateServiceHandler.ToDto(svc));
    }
}

public class DeleteServiceHandler : IRequestHandler<DeleteServiceCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantProvider _tenant;

    public DeleteServiceHandler(IApplicationDbContext db, ITenantProvider tenant)
    {
        _db = db; _tenant = tenant;
    }

    public async Task<Result> Handle(DeleteServiceCommand cmd, CancellationToken ct)
    {
        var svc = await _db.BillingServices
            .FirstOrDefaultAsync(s => s.Id == cmd.Id && s.TenantId == _tenant.TenantId && s.DeletedAt == null, ct);
        if (svc == null) return Result.Failure("SERVICE_NOT_FOUND", "Khong tim thay dich vu");
        svc.DeletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public class ListServicePackagesHandler : IRequestHandler<ListServicePackagesQuery, Result<PagedResult<ServicePackageResponse>>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;

    public ListServicePackagesHandler(IDapperConnectionFactory db, ITenantProvider tenant)
    {
        _db = db; _tenant = tenant;
    }

    public async Task<Result<PagedResult<ServicePackageResponse>>> Handle(
        ListServicePackagesQuery query, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var tenantId = _tenant.TenantId;

        var where = "WHERE p.tenant_id = @tenantId AND p.deleted_at IS NULL";
        var p = new DynamicParameters();
        p.Add("tenantId", tenantId);

        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            where += " AND (p.code LIKE @q OR p.name LIKE @q)";
            p.Add("q", $"%{query.Q}%");
        }
        if (query.IsActive.HasValue)
        {
            where += " AND p.is_active = @isActive";
            p.Add("isActive", query.IsActive.Value ? 1 : 0);
        }

        var total = await conn.ExecuteScalarAsync<int>(
            $"SELECT COUNT(*) FROM diab_his_bil_service_packages p {where}", p);

        var pkgs = await conn.QueryAsync<dynamic>(
            $"SELECT p.* FROM diab_his_bil_service_packages p {where} ORDER BY p.name ASC", p);

        var result = new List<ServicePackageResponse>();
        foreach (var pkg in pkgs)
        {
            var items = await conn.QueryAsync<dynamic>(
                @"SELECT i.service_id, s.name as service_name, s.price as unit_price, i.quantity
                  FROM diab_his_bil_service_package_items i
                  JOIN diab_his_bil_services s ON s.id = i.service_id
                  WHERE i.package_id = @pkgId",
                new { pkgId = (string)pkg.id });

            var itemList = items.Select(i => new ServicePackageItemDto(
                Guid.Parse((string)i.service_id),
                (string?)i.service_name,
                (decimal?)i.unit_price,
                (int)i.quantity)).ToList();

            var totalPrice = itemList.Sum(i => (i.UnitPrice ?? 0) * i.Quantity);
            var discount = (decimal)pkg.discount_percent;
            var finalPrice = totalPrice * (1 - discount / 100);

            result.Add(new ServicePackageResponse(
                Guid.Parse((string)pkg.id),
                (int)pkg.tenant_id,
                (string)pkg.code,
                (string)pkg.name,
                itemList,
                totalPrice,
                discount,
                finalPrice,
                pkg.valid_from == null ? (DateOnly?)null : DateOnly.FromDateTime((DateTime)pkg.valid_from),
                pkg.valid_to == null ? (DateOnly?)null : DateOnly.FromDateTime((DateTime)pkg.valid_to),
                Convert.ToBoolean(pkg.is_active)));
        }

        return Result<PagedResult<ServicePackageResponse>>.Success(
            new PagedResult<ServicePackageResponse>(result, 1, result.Count, total));
    }
}

public class GetServicePackageHandler : IRequestHandler<GetServicePackageQuery, Result<ServicePackageResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;

    public GetServicePackageHandler(IDapperConnectionFactory db, ITenantProvider tenant)
    {
        _db = db; _tenant = tenant;
    }

    public async Task<Result<ServicePackageResponse>> Handle(GetServicePackageQuery query, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var pkg = await conn.QueryFirstOrDefaultAsync<dynamic>(
            @"SELECT * FROM diab_his_bil_service_packages
              WHERE id = @id AND tenant_id = @tenantId AND deleted_at IS NULL",
            new { id = query.Id.ToString(), tenantId = _tenant.TenantId });

        if (pkg == null) return Result<ServicePackageResponse>.Failure("PACKAGE_NOT_FOUND", "Khong tim thay goi kham");

        var items = await conn.QueryAsync<dynamic>(
            @"SELECT i.service_id, s.name as service_name, s.price as unit_price, i.quantity
              FROM diab_his_bil_service_package_items i
              JOIN diab_his_bil_services s ON s.id = i.service_id
              WHERE i.package_id = @pkgId",
            new { pkgId = (string)pkg.id });

        var itemList = items.Select(i => new ServicePackageItemDto(
            Guid.Parse((string)i.service_id),
            (string?)i.service_name,
            (decimal?)i.unit_price,
            (int)i.quantity)).ToList();

        var totalPrice = itemList.Sum(i => (i.UnitPrice ?? 0) * i.Quantity);
        var discount = (decimal)pkg.discount_percent;
        var finalPrice = totalPrice * (1 - discount / 100);

        return Result<ServicePackageResponse>.Success(new ServicePackageResponse(
            Guid.Parse((string)pkg.id),
            (int)pkg.tenant_id,
            (string)pkg.code,
            (string)pkg.name,
            itemList,
            totalPrice, discount, finalPrice,
            pkg.valid_from == null ? (DateOnly?)null : DateOnly.FromDateTime((DateTime)pkg.valid_from),
            pkg.valid_to == null ? (DateOnly?)null : DateOnly.FromDateTime((DateTime)pkg.valid_to),
            Convert.ToBoolean(pkg.is_active)));
    }
}

public class CreateServicePackageHandler : IRequestHandler<CreateServicePackageCommand, Result<ServicePackageResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _user;

    public CreateServicePackageHandler(IApplicationDbContext db, ITenantProvider tenant, ICurrentUser user)
    {
        _db = db; _tenant = tenant; _user = user;
    }

    public async Task<Result<ServicePackageResponse>> Handle(CreateServicePackageCommand cmd, CancellationToken ct)
    {
        var req = cmd.Request;
        var pkg = new ServicePackage
        {
            TenantId = _tenant.TenantId,
            Code = req.Code,
            Name = req.Name,
            DiscountPercent = req.DiscountPercent,
            ValidFrom = req.ValidFrom,
            ValidTo = req.ValidTo,
            IsActive = req.IsActive,
            CreatedBy = _user.UserId
        };

        foreach (var item in req.Services)
        {
            pkg.Items.Add(new ServicePackageItem
            {
                ServiceId = item.ServiceId,
                Quantity = item.Quantity
            });
        }

        _db.ServicePackages.Add(pkg);
        await _db.SaveChangesAsync(ct);

        return Result<ServicePackageResponse>.Success(ToDto(pkg, []));
    }

    internal static ServicePackageResponse ToDto(ServicePackage pkg, List<ServicePackageItemDto> items)
    {
        var total = items.Sum(i => (i.UnitPrice ?? 0) * i.Quantity);
        var final = total * (1 - pkg.DiscountPercent / 100);
        return new ServicePackageResponse(
            pkg.Id, pkg.TenantId, pkg.Code, pkg.Name, items,
            total, pkg.DiscountPercent, final, pkg.ValidFrom, pkg.ValidTo, pkg.IsActive);
    }
}

public class UpdateServicePackageHandler : IRequestHandler<UpdateServicePackageCommand, Result<ServicePackageResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantProvider _tenant;

    public UpdateServicePackageHandler(IApplicationDbContext db, ITenantProvider tenant)
    {
        _db = db; _tenant = tenant;
    }

    public async Task<Result<ServicePackageResponse>> Handle(UpdateServicePackageCommand cmd, CancellationToken ct)
    {
        var pkg = await _db.ServicePackages
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == cmd.Id && p.TenantId == _tenant.TenantId && p.DeletedAt == null, ct);
        if (pkg == null) return Result<ServicePackageResponse>.Failure("PACKAGE_NOT_FOUND", "Khong tim thay goi kham");

        pkg.Code = cmd.Request.Code;
        pkg.Name = cmd.Request.Name;
        pkg.DiscountPercent = cmd.Request.DiscountPercent;
        pkg.ValidFrom = cmd.Request.ValidFrom;
        pkg.ValidTo = cmd.Request.ValidTo;
        pkg.IsActive = cmd.Request.IsActive;

        pkg.Items.Clear();
        foreach (var item in cmd.Request.Services)
            pkg.Items.Add(new ServicePackageItem { ServiceId = item.ServiceId, Quantity = item.Quantity });

        await _db.SaveChangesAsync(ct);
        return Result<ServicePackageResponse>.Success(CreateServicePackageHandler.ToDto(pkg, []));
    }
}

public class DeleteServicePackageHandler : IRequestHandler<DeleteServicePackageCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantProvider _tenant;

    public DeleteServicePackageHandler(IApplicationDbContext db, ITenantProvider tenant)
    {
        _db = db; _tenant = tenant;
    }

    public async Task<Result> Handle(DeleteServicePackageCommand cmd, CancellationToken ct)
    {
        var pkg = await _db.ServicePackages
            .FirstOrDefaultAsync(p => p.Id == cmd.Id && p.TenantId == _tenant.TenantId && p.DeletedAt == null, ct);
        if (pkg == null) return Result.Failure("PACKAGE_NOT_FOUND", "Khong tim thay goi kham");
        pkg.DeletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public record ServiceExcelRow(string Code, string Name, string Category, decimal Price, int VatRate);

/// <summary>Interface de tach biet Application khoi ClosedXML (trong Infrastructure)</summary>
public interface IServiceExcelParser
{
    List<ServiceExcelRow> Parse(Stream fileStream);
}

public class ImportServicesHandler : IRequestHandler<ImportServicesFromExcelCommand, Result<ImportResultResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _user;
    private readonly IServiceExcelParser _parser;

    public ImportServicesHandler(IApplicationDbContext db, ITenantProvider tenant, ICurrentUser user, IServiceExcelParser parser)
    {
        _db = db; _tenant = tenant; _user = user; _parser = parser;
    }

    public async Task<Result<ImportResultResponse>> Handle(ImportServicesFromExcelCommand cmd, CancellationToken ct)
    {
        List<ServiceExcelRow> rows;
        try { rows = _parser.Parse(cmd.FileStream); }
        catch (Exception ex) { return Result<ImportResultResponse>.Failure("EXCEL_PARSE_ERROR", ex.Message); }

        int inserted = 0, updated = 0;
        var errors = new List<object>();
        int rowNum = 2;

        foreach (var row in rows)
        {
            try
            {
                if (string.IsNullOrEmpty(row.Code) || string.IsNullOrEmpty(row.Name)) { rowNum++; continue; }

                var existing = await _db.BillingServices
                    .FirstOrDefaultAsync(s => s.TenantId == _tenant.TenantId && s.Code == row.Code && s.DeletedAt == null, ct);

                if (existing == null)
                {
                    _db.BillingServices.Add(new BillingService
                    {
                        TenantId = _tenant.TenantId,
                        Code = row.Code, Name = row.Name, Category = row.Category,
                        Price = row.Price, VatRate = row.VatRate, CreatedBy = _user.UserId
                    });
                    inserted++;
                }
                else
                {
                    existing.Name = row.Name;
                    existing.Category = row.Category;
                    existing.Price = row.Price;
                    existing.VatRate = row.VatRate;
                    updated++;
                }
            }
            catch (Exception ex)
            {
                errors.Add(new { row = rowNum, error = ex.Message });
            }
            rowNum++;
        }

        await _db.SaveChangesAsync(ct);
        return Result<ImportResultResponse>.Success(
            new ImportResultResponse(rows.Count, inserted, updated, errors));
    }
}
