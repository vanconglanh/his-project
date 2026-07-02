using Dapper;
using MediatR;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.Pharmacy.Warehouse;

// ─── DTOs ─────────────────────────────────────────────────────────────────────
public record SupplierRequest(
    string Code,
    string Name,
    string? TaxCode,
    string? Address,
    string? Phone,
    string? Email,
    string? ContactPerson,
    string? Status = "ACTIVE"
);

public record SupplierResponse(
    string Id,
    int TenantId,
    string Code,
    string Name,
    string? TaxCode,
    string? Phone,
    string? Email,
    string? Address,
    string? ContactPerson,
    string Status,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

// ─── Queries & Commands ────────────────────────────────────────────────────────
public record ListSuppliersQuery(int TenantId, string? Q, string? Status, int Page, int PageSize)
    : IRequest<PagedResult<SupplierResponse>>;

public record GetSupplierQuery(string Id, int TenantId)
    : IRequest<SupplierResponse?>;

public record CreateSupplierCommand(int TenantId, SupplierRequest Request)
    : IRequest<SupplierResponse>;

public record UpdateSupplierCommand(string Id, int TenantId, SupplierRequest Request)
    : IRequest<SupplierResponse?>;

public record DeleteSupplierCommand(string Id, int TenantId)
    : IRequest<bool>;

// ─── Handlers ─────────────────────────────────────────────────────────────────
public class ListSuppliersHandler : IRequestHandler<ListSuppliersQuery, PagedResult<SupplierResponse>>
{
    private readonly IDapperConnectionFactory _db;
    public ListSuppliersHandler(IDapperConnectionFactory db) => _db = db;

    public async Task<PagedResult<SupplierResponse>> Handle(ListSuppliersQuery q, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var where = new List<string> { "tenant_id = @TenantId", "deleted_at IS NULL" };
        var prm = new DynamicParameters();
        prm.Add("TenantId", q.TenantId);

        if (!string.IsNullOrWhiteSpace(q.Q))
        {
            where.Add("(name LIKE @q OR code LIKE @q OR phone LIKE @q)");
            prm.Add("q", $"%{q.Q}%");
        }
        if (!string.IsNullOrWhiteSpace(q.Status))
        {
            where.Add("is_active = @IsActive");
            prm.Add("IsActive", q.Status.Equals("ACTIVE", StringComparison.OrdinalIgnoreCase) ? 1 : 0);
        }

        var wc = string.Join(" AND ", where);
        int offset = (q.Page - 1) * q.PageSize;
        prm.Add("Limit", q.PageSize);
        prm.Add("Offset", offset);

        var total = await conn.ExecuteScalarAsync<int>(
            $"SELECT COUNT(*) FROM diab_his_pha_suppliers WHERE {wc}", prm);

        var rows = await conn.QueryAsync<dynamic>(
            $@"SELECT id, tenant_id, code, name, tax_code, phone, email, address,
                      contact_name, is_active, created_at, updated_at
               FROM diab_his_pha_suppliers
               WHERE {wc}
               ORDER BY name ASC LIMIT @Limit OFFSET @Offset", prm);

        var items = rows.Select(MapRow).ToList();
        return new PagedResult<SupplierResponse>(items, q.Page, q.PageSize, total);
    }

    private static SupplierResponse MapRow(dynamic r) => new(
        r.id?.ToString() ?? "",
        (int)(r.tenant_id ?? 0),
        (string)(r.code ?? ""),
        (string)(r.name ?? ""),
        (string?)r.tax_code,
        (string?)r.phone,
        (string?)r.email,
        (string?)r.address,
        (string?)r.contact_name,
        (bool)(r.is_active ?? true) ? "ACTIVE" : "INACTIVE",
        (DateTime)(r.created_at ?? DateTime.UtcNow),
        (DateTime)(r.updated_at ?? DateTime.UtcNow)
    );
}

public class GetSupplierHandler : IRequestHandler<GetSupplierQuery, SupplierResponse?>
{
    private readonly IDapperConnectionFactory _db;
    public GetSupplierHandler(IDapperConnectionFactory db) => _db = db;

    public async Task<SupplierResponse?> Handle(GetSupplierQuery q, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var row = await conn.QueryFirstOrDefaultAsync<dynamic>(
            @"SELECT id, tenant_id, code, name, tax_code, phone, email, address,
                     contact_name, is_active, created_at, updated_at
              FROM diab_his_pha_suppliers
              WHERE id = @Id AND tenant_id = @TenantId AND deleted_at IS NULL",
            new { Id = q.Id, q.TenantId });

        if (row == null) return null;

        return new SupplierResponse(
            row.id?.ToString() ?? "",
            (int)(row.tenant_id ?? 0),
            (string)(row.code ?? ""),
            (string)(row.name ?? ""),
            (string?)row.tax_code,
            (string?)row.phone,
            (string?)row.email,
            (string?)row.address,
            (string?)row.contact_name,
            (bool)(row.is_active ?? true) ? "ACTIVE" : "INACTIVE",
            (DateTime)(row.created_at ?? DateTime.UtcNow),
            (DateTime)(row.updated_at ?? DateTime.UtcNow)
        );
    }
}

public class CreateSupplierHandler : IRequestHandler<CreateSupplierCommand, SupplierResponse>
{
    private readonly IDapperConnectionFactory _db;
    public CreateSupplierHandler(IDapperConnectionFactory db) => _db = db;

    public async Task<SupplierResponse> Handle(CreateSupplierCommand cmd, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var id = Guid.NewGuid().ToString();
        var isActive = !string.Equals(cmd.Request.Status, "INACTIVE", StringComparison.OrdinalIgnoreCase);

        await conn.ExecuteAsync(
            @"INSERT INTO diab_his_pha_suppliers
                (id, tenant_id, code, name, tax_code, phone, email, address, contact_name, is_active, created_at, updated_at)
              VALUES (@Id, @TenantId, @Code, @Name, @TaxCode, @Phone, @Email, @Address, @ContactName, @IsActive, UTC_TIMESTAMP(), UTC_TIMESTAMP())",
            new
            {
                Id = id, cmd.TenantId, cmd.Request.Code, cmd.Request.Name,
                TaxCode = cmd.Request.TaxCode, Phone = cmd.Request.Phone, Email = cmd.Request.Email,
                Address = cmd.Request.Address, ContactName = cmd.Request.ContactPerson,
                IsActive = isActive
            });

        return new SupplierResponse(
            id, cmd.TenantId, cmd.Request.Code, cmd.Request.Name,
            cmd.Request.TaxCode, cmd.Request.Phone, cmd.Request.Email, cmd.Request.Address,
            cmd.Request.ContactPerson, isActive ? "ACTIVE" : "INACTIVE",
            DateTime.UtcNow, DateTime.UtcNow);
    }
}

public class UpdateSupplierHandler : IRequestHandler<UpdateSupplierCommand, SupplierResponse?>
{
    private readonly IDapperConnectionFactory _db;
    public UpdateSupplierHandler(IDapperConnectionFactory db) => _db = db;

    public async Task<SupplierResponse?> Handle(UpdateSupplierCommand cmd, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var isActive = !string.Equals(cmd.Request.Status, "INACTIVE", StringComparison.OrdinalIgnoreCase);

        var affected = await conn.ExecuteAsync(
            @"UPDATE diab_his_pha_suppliers
              SET code = @Code, name = @Name, tax_code = @TaxCode, phone = @Phone,
                  email = @Email, address = @Address, contact_name = @ContactName,
                  is_active = @IsActive, updated_at = UTC_TIMESTAMP()
              WHERE id = @Id AND tenant_id = @TenantId AND deleted_at IS NULL",
            new
            {
                cmd.Id, cmd.TenantId, cmd.Request.Code, cmd.Request.Name,
                TaxCode = cmd.Request.TaxCode, Phone = cmd.Request.Phone, Email = cmd.Request.Email,
                Address = cmd.Request.Address, ContactName = cmd.Request.ContactPerson,
                IsActive = isActive
            });

        if (affected == 0) return null;

        return new SupplierResponse(
            cmd.Id, cmd.TenantId, cmd.Request.Code, cmd.Request.Name,
            cmd.Request.TaxCode, cmd.Request.Phone, cmd.Request.Email, cmd.Request.Address,
            cmd.Request.ContactPerson, isActive ? "ACTIVE" : "INACTIVE",
            DateTime.UtcNow, DateTime.UtcNow);
    }
}

public class DeleteSupplierHandler : IRequestHandler<DeleteSupplierCommand, bool>
{
    private readonly IDapperConnectionFactory _db;
    public DeleteSupplierHandler(IDapperConnectionFactory db) => _db = db;

    public async Task<bool> Handle(DeleteSupplierCommand cmd, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var affected = await conn.ExecuteAsync(
            "UPDATE diab_his_pha_suppliers SET deleted_at = UTC_TIMESTAMP() WHERE id = @Id AND tenant_id = @TenantId AND deleted_at IS NULL",
            new { cmd.Id, cmd.TenantId });
        return affected > 0;
    }
}
