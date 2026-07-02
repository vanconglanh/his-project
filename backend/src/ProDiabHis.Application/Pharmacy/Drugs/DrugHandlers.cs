using System.Data;
using Dapper;
using MediatR;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.Pharmacy.Drugs;

// --- Commands & Queries -------------------------------------------------
public record ListDrugsQuery(string? Q, string? Status, bool? RequiresPrescription, string? AtcCode, int? CategoryId, int Page, int PageSize)
    : IRequest<Result<PagedResult<DrugMasterResponse>>>;
public record GetDrugQuery(string Id) : IRequest<Result<DrugMasterResponse>>;
public record CreateDrugCommand(DrugMasterRequest Request) : IRequest<Result<DrugMasterResponse>>;
public record UpdateDrugCommand(string Id, DrugMasterRequest Request) : IRequest<Result<DrugMasterResponse>>;
public record DeleteDrugCommand(string Id) : IRequest<Result<bool>>;
public record ImportDrugsCommand(System.IO.Stream ExcelStream, string Mode) : IRequest<Result<DrugImportSummary>>;
public record SearchDrugsQuery(string Q, int Limit) : IRequest<Result<IReadOnlyList<DrugMasterResponse>>>;
public record GetEquivalentDrugsQuery(string Id) : IRequest<Result<IReadOnlyList<DrugMasterResponse>>>;
public record GetDrugInteractionsQuery(string Id) : IRequest<Result<IReadOnlyList<DdiRule>>>;
public record ListDrugCategoriesQuery : IRequest<Result<IReadOnlyList<DrugCategory>>>;
public record CreateDrugCategoryCommand(DrugCategoryCreateRequest Request) : IRequest<Result<DrugCategory>>;
public record SyncCucQldCommand(string Mode, DateTime? Since) : IRequest<Result<SyncJobResponse>>;

// --- Handlers -------------------------------------------------------------
public class ListDrugsHandler : IRequestHandler<ListDrugsQuery, Result<PagedResult<DrugMasterResponse>>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;

    public ListDrugsHandler(IDapperConnectionFactory db, ICurrentUser currentUser) { _db = db; _currentUser = currentUser; }

    public async Task<Result<PagedResult<DrugMasterResponse>>> Handle(ListDrugsQuery q, CancellationToken ct)
    {
        using var conn = ((IDbConnection)_db.CreateConnection());
        var tenantId = _currentUser.TenantId!.Value;
        var offset = (q.Page - 1) * q.PageSize;

        var where = new List<string> { "d.tenant_id = @tenantId", "d.deleted_at IS NULL" };
        var prm = new DynamicParameters();
        prm.Add("tenantId", tenantId); prm.Add("offset", offset); prm.Add("limit", q.PageSize);

        if (!string.IsNullOrWhiteSpace(q.Q))
        {
            where.Add("(d.name_vi LIKE @q OR d.code LIKE @q OR d.generic_name LIKE @q)");
            prm.Add("q", $"%{q.Q}%");
        }
        if (!string.IsNullOrWhiteSpace(q.Status)) { where.Add("d.status = @status"); prm.Add("status", q.Status); }
        if (q.RequiresPrescription.HasValue) { where.Add("d.requires_prescription = @rx"); prm.Add("rx", q.RequiresPrescription.Value ? 1 : 0); }
        if (!string.IsNullOrWhiteSpace(q.AtcCode)) { where.Add("d.atc_code = @atcCode"); prm.Add("atcCode", q.AtcCode); }

        var wc = string.Join(" AND ", where);
        var total = await conn.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM diab_his_pha_drugs d WHERE {wc}", prm);

        var rows = await conn.QueryAsync<DrugRow>(
            $@"SELECT d.ID as Id, d.tenant_id as TenantId, d.code as Code,
                      d.name_vi as NameVi, d.name_en as NameEn, d.generic_name as GenericName,
                      d.atc_code as AtcCode, d.strength as Strength, d.unit as Unit,
                      d.form as Form, d.manufacturer as Manufacturer, d.country as Country,
                      d.price as Price, d.category_id as CategoryId,
                      d.requires_prescription as RequiresPrescription,
                      d.is_psychotropic as IsPsychotropic, d.is_narcotic as IsNarcotic,
                      d.dtqg_drug_code as DtqgDrugCode, d.status as Status,
                      d.created_at as CreatedAt, d.updated_at as UpdatedAt,
                      (SELECT COUNT(*) FROM diab_his_pha_ddi_rules r WHERE (r.drug1_id = d.ID OR r.drug2_id = d.ID) AND r.deleted_at IS NULL) as InteractionsCount
               FROM diab_his_pha_drugs d WHERE {wc}
               ORDER BY d.name_vi ASC LIMIT @limit OFFSET @offset", prm);

        var items = rows.Select(MapDrug).ToList();
        return Result<PagedResult<DrugMasterResponse>>.Success(new PagedResult<DrugMasterResponse>(items, q.Page, q.PageSize, total));
    }

    private static DrugMasterResponse MapDrug(DrugRow r) =>
        new(r.Id?.ToString() ?? "", r.TenantId, r.Code ?? "", r.NameVi ?? "", r.NameEn, r.GenericName,
            r.AtcCode, r.Strength, r.Unit ?? "", r.Form, r.Manufacturer, r.Country,
            r.Price, r.CategoryId, r.RequiresPrescription == 1, r.IsPsychotropic == 1, r.IsNarcotic == 1,
            r.DtqgDrugCode, r.Status ?? "ACTIVE", r.InteractionsCount, r.CreatedAt, r.UpdatedAt);
}

public class GetDrugHandler : IRequestHandler<GetDrugQuery, Result<DrugMasterResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;

    public GetDrugHandler(IDapperConnectionFactory db, ICurrentUser currentUser) { _db = db; _currentUser = currentUser; }

    public async Task<Result<DrugMasterResponse>> Handle(GetDrugQuery q, CancellationToken ct)
    {
        using var conn = ((IDbConnection)_db.CreateConnection());
        var tenantId = _currentUser.TenantId!.Value;

        var row = await conn.QueryFirstOrDefaultAsync<DrugRow>(
            @"SELECT d.ID as Id, d.tenant_id as TenantId, d.code as Code,
                     d.name_vi as NameVi, d.name_en as NameEn, d.generic_name as GenericName,
                     d.atc_code as AtcCode, d.strength as Strength, d.unit as Unit,
                     d.form as Form, d.manufacturer as Manufacturer, d.country as Country,
                     d.price as Price, d.category_id as CategoryId,
                     d.requires_prescription as RequiresPrescription,
                     d.is_psychotropic as IsPsychotropic, d.is_narcotic as IsNarcotic,
                     d.dtqg_drug_code as DtqgDrugCode, d.status as Status,
                     d.created_at as CreatedAt, d.updated_at as UpdatedAt,
                     (SELECT COUNT(*) FROM diab_his_pha_ddi_rules r WHERE (r.drug1_id = d.ID OR r.drug2_id = d.ID) AND r.deleted_at IS NULL) as InteractionsCount
              FROM diab_his_pha_drugs d
              WHERE d.id = @id AND d.tenant_id = @tenantId AND d.deleted_at IS NULL",
            new { id = q.Id, tenantId });

        if (row == null)
            return Result<DrugMasterResponse>.Failure("DRUG_NOT_FOUND", "Khong tim thay thuoc.");

        return Result<DrugMasterResponse>.Success(new DrugMasterResponse(
            row.Id?.ToString() ?? "", row.TenantId, row.Code ?? "", row.NameVi ?? "", row.NameEn, row.GenericName,
            row.AtcCode, row.Strength, row.Unit ?? "", row.Form, row.Manufacturer, row.Country,
            row.Price, row.CategoryId, row.RequiresPrescription == 1, row.IsPsychotropic == 1, row.IsNarcotic == 1,
            row.DtqgDrugCode, row.Status ?? "ACTIVE", row.InteractionsCount, row.CreatedAt, row.UpdatedAt));
    }
}

public class CreateDrugHandler : IRequestHandler<CreateDrugCommand, Result<DrugMasterResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;

    public CreateDrugHandler(IDapperConnectionFactory db, ICurrentUser currentUser) { _db = db; _currentUser = currentUser; }

    public async Task<Result<DrugMasterResponse>> Handle(CreateDrugCommand cmd, CancellationToken ct)
    {
        using var conn = ((IDbConnection)_db.CreateConnection());
        var tenantId = _currentUser.TenantId!.Value;
        var r = cmd.Request;

        // Check duplicate
        var exists = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM diab_his_pha_drugs WHERE tenant_id = @tenantId AND code = @code AND deleted_at IS NULL",
            new { tenantId, code = r.Code });
        if (exists > 0)
            return Result<DrugMasterResponse>.Failure("DRUG_CODE_EXISTS", "Ma thuoc da ton tai.");

        var newId = Guid.NewGuid().ToString();
        await conn.ExecuteAsync(
            @"INSERT INTO diab_his_pha_drugs (id, tenant_id, code, name_vi, name_en, generic_name, atc_code, strength, unit, form,
              manufacturer, country, price, category_id, requires_prescription, is_psychotropic, is_narcotic, dtqg_drug_code, status, created_at, updated_at)
              VALUES (@newId, @tenantId, @code, @nameVi, @nameEn, @genericName, @atcCode, @strength, @unit, @form,
              @manufacturer, @country, @price, @categoryId, @rx, @psycho, @narcotic, @dtqgCode, @status, NOW(), NOW())",
            new { newId, tenantId, code = r.Code, nameVi = r.NameVi, nameEn = r.NameEn, genericName = r.GenericName,
                  atcCode = r.AtcCode, strength = r.Strength, unit = r.Unit, form = r.Form,
                  manufacturer = r.Manufacturer, country = r.Country, price = r.Price, categoryId = r.CategoryId,
                  rx = r.RequiresPrescription ? 1 : 0, psycho = r.IsPsychotropic ? 1 : 0,
                  narcotic = r.IsNarcotic ? 1 : 0, dtqgCode = r.DtqgDrugCode, status = r.Status });

        return await new GetDrugHandler(_db, _currentUser).Handle(new GetDrugQuery(newId), ct);
    }
}

public class UpdateDrugHandler : IRequestHandler<UpdateDrugCommand, Result<DrugMasterResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;

    public UpdateDrugHandler(IDapperConnectionFactory db, ICurrentUser currentUser) { _db = db; _currentUser = currentUser; }

    public async Task<Result<DrugMasterResponse>> Handle(UpdateDrugCommand cmd, CancellationToken ct)
    {
        using var conn = ((IDbConnection)_db.CreateConnection());
        var tenantId = _currentUser.TenantId!.Value;
        var r = cmd.Request;

        var exists = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM diab_his_pha_drugs WHERE id = @id AND tenant_id = @tenantId AND deleted_at IS NULL",
            new { id = cmd.Id, tenantId });
        if (exists == 0)
            return Result<DrugMasterResponse>.Failure("DRUG_NOT_FOUND", "Khong tim thay thuoc.");

        await conn.ExecuteAsync(
            @"UPDATE diab_his_pha_drugs SET name_vi=@nameVi, name_en=@nameEn, generic_name=@genericName,
              atc_code=@atcCode, strength=@strength, unit=@unit, form=@form, manufacturer=@manufacturer,
              country=@country, price=@price, category_id=@categoryId, requires_prescription=@rx,
              is_psychotropic=@psycho, is_narcotic=@narcotic, dtqg_drug_code=@dtqgCode, status=@status, updated_at=NOW()
              WHERE id=@id AND tenant_id=@tenantId",
            new { nameVi = r.NameVi, nameEn = r.NameEn, genericName = r.GenericName, atcCode = r.AtcCode,
                  strength = r.Strength, unit = r.Unit, form = r.Form, manufacturer = r.Manufacturer,
                  country = r.Country, price = r.Price, categoryId = r.CategoryId,
                  rx = r.RequiresPrescription ? 1 : 0, psycho = r.IsPsychotropic ? 1 : 0,
                  narcotic = r.IsNarcotic ? 1 : 0, dtqgCode = r.DtqgDrugCode, status = r.Status,
                  id = cmd.Id, tenantId });

        return await new GetDrugHandler(_db, _currentUser).Handle(new GetDrugQuery(cmd.Id), ct);
    }
}

public class DeleteDrugHandler : IRequestHandler<DeleteDrugCommand, Result<bool>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;

    public DeleteDrugHandler(IDapperConnectionFactory db, ICurrentUser currentUser) { _db = db; _currentUser = currentUser; }

    public async Task<Result<bool>> Handle(DeleteDrugCommand cmd, CancellationToken ct)
    {
        using var conn = ((IDbConnection)_db.CreateConnection());
        var tenantId = _currentUser.TenantId!.Value;

        var rows = await conn.ExecuteAsync(
            "UPDATE diab_his_pha_drugs SET deleted_at = NOW(), updated_at = NOW() WHERE id = @id AND tenant_id = @tenantId AND deleted_at IS NULL",
            new { id = cmd.Id, tenantId });

        return rows == 0
            ? Result<bool>.Failure("DRUG_NOT_FOUND", "Khong tim thay thuoc.")
            : Result<bool>.Success(true);
    }
}

public class ImportDrugsHandler : IRequestHandler<ImportDrugsCommand, Result<DrugImportSummary>>
{
    private readonly IExcelImporter _importer;
    private readonly ICurrentUser _currentUser;

    public ImportDrugsHandler(IExcelImporter importer, ICurrentUser currentUser) { _importer = importer; _currentUser = currentUser; }

    public async Task<Result<DrugImportSummary>> Handle(ImportDrugsCommand cmd, CancellationToken ct)
    {
        var tenantId = _currentUser.TenantId!.Value;
        var importResult = await _importer.ImportDrugsAsync(cmd.ExcelStream, cmd.Mode, tenantId, 0, ct);
        var summary = new DrugImportSummary(
            importResult.TotalRows, importResult.Inserted, importResult.Updated, importResult.Failed,
            importResult.Errors.Select(e => new DrugImportRowError(e.Row, e.Message)).ToList());
        return Result<DrugImportSummary>.Success(summary);
    }
}

public class SearchDrugsHandler : IRequestHandler<SearchDrugsQuery, Result<IReadOnlyList<DrugMasterResponse>>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;

    public SearchDrugsHandler(IDapperConnectionFactory db, ICurrentUser currentUser) { _db = db; _currentUser = currentUser; }

    public async Task<Result<IReadOnlyList<DrugMasterResponse>>> Handle(SearchDrugsQuery q, CancellationToken ct)
    {
        using var conn = ((IDbConnection)_db.CreateConnection());
        var tenantId = _currentUser.TenantId!.Value;

        var rows = await conn.QueryAsync<DrugRow>(
            @"SELECT d.ID as Id, d.tenant_id as TenantId, d.code as Code,
                     d.name_vi as NameVi, d.name_en as NameEn, d.generic_name as GenericName,
                     d.atc_code as AtcCode, d.strength as Strength, d.unit as Unit,
                     d.form as Form, d.manufacturer as Manufacturer, d.country as Country,
                     d.price as Price, d.category_id as CategoryId,
                     d.requires_prescription as RequiresPrescription,
                     d.is_psychotropic as IsPsychotropic, d.is_narcotic as IsNarcotic,
                     d.dtqg_drug_code as DtqgDrugCode, d.status as Status,
                     d.created_at as CreatedAt, d.updated_at as UpdatedAt, 0 as InteractionsCount
              FROM diab_his_pha_drugs d
              WHERE d.tenant_id = @tenantId AND d.deleted_at IS NULL AND d.status = 'ACTIVE'
                AND (d.name_vi LIKE @q OR d.code LIKE @q OR d.generic_name LIKE @q)
              ORDER BY d.name_vi LIMIT @limit",
            new { tenantId, q = $"%{q.Q}%", limit = Math.Min(q.Limit, 50) });

        var items = rows.Select(r => new DrugMasterResponse(r.Id?.ToString() ?? "", r.TenantId, r.Code ?? "", r.NameVi ?? "", r.NameEn, r.GenericName,
            r.AtcCode, r.Strength, r.Unit ?? "", r.Form, r.Manufacturer, r.Country, r.Price, r.CategoryId,
            r.RequiresPrescription == 1, r.IsPsychotropic == 1, r.IsNarcotic == 1, r.DtqgDrugCode,
            r.Status ?? "ACTIVE", 0, r.CreatedAt, r.UpdatedAt)).ToList();

        return Result<IReadOnlyList<DrugMasterResponse>>.Success(items);
    }
}

public class GetEquivalentDrugsHandler : IRequestHandler<GetEquivalentDrugsQuery, Result<IReadOnlyList<DrugMasterResponse>>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;

    public GetEquivalentDrugsHandler(IDapperConnectionFactory db, ICurrentUser currentUser) { _db = db; _currentUser = currentUser; }

    public async Task<Result<IReadOnlyList<DrugMasterResponse>>> Handle(GetEquivalentDrugsQuery q, CancellationToken ct)
    {
        using var conn = ((IDbConnection)_db.CreateConnection());
        var tenantId = _currentUser.TenantId!.Value;

        var atcCode = await conn.ExecuteScalarAsync<string>(
            "SELECT atc_code FROM diab_his_pha_drugs WHERE id = @id AND tenant_id = @tenantId AND deleted_at IS NULL",
            new { id = q.Id, tenantId });

        if (string.IsNullOrWhiteSpace(atcCode))
            return Result<IReadOnlyList<DrugMasterResponse>>.Success([]);

        var rows = await conn.QueryAsync<DrugRow>(
            @"SELECT d.ID as Id, d.tenant_id as TenantId, d.code as Code,
                     d.name_vi as NameVi, d.name_en as NameEn, d.generic_name as GenericName,
                     d.atc_code as AtcCode, d.strength as Strength, d.unit as Unit,
                     d.form as Form, d.manufacturer as Manufacturer, d.country as Country,
                     d.price as Price, d.category_id as CategoryId,
                     d.requires_prescription as RequiresPrescription,
                     d.is_psychotropic as IsPsychotropic, d.is_narcotic as IsNarcotic,
                     d.dtqg_drug_code as DtqgDrugCode, d.status as Status,
                     d.created_at as CreatedAt, d.updated_at as UpdatedAt, 0 as InteractionsCount
              FROM diab_his_pha_drugs d
              WHERE d.tenant_id = @tenantId AND d.atc_code = @atcCode AND d.id != @id AND d.deleted_at IS NULL
              LIMIT 20",
            new { tenantId, atcCode, id = q.Id });

        var items = rows.Select(r => new DrugMasterResponse(r.Id?.ToString() ?? "", r.TenantId, r.Code ?? "", r.NameVi ?? "", r.NameEn, r.GenericName,
            r.AtcCode, r.Strength, r.Unit ?? "", r.Form, r.Manufacturer, r.Country, r.Price, r.CategoryId,
            r.RequiresPrescription == 1, r.IsPsychotropic == 1, r.IsNarcotic == 1, r.DtqgDrugCode,
            r.Status ?? "ACTIVE", 0, r.CreatedAt, r.UpdatedAt)).ToList();

        return Result<IReadOnlyList<DrugMasterResponse>>.Success(items);
    }
}

public class GetDrugInteractionsHandler : IRequestHandler<GetDrugInteractionsQuery, Result<IReadOnlyList<DdiRule>>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;

    public GetDrugInteractionsHandler(IDapperConnectionFactory db, ICurrentUser currentUser) { _db = db; _currentUser = currentUser; }

    public async Task<Result<IReadOnlyList<DdiRule>>> Handle(GetDrugInteractionsQuery q, CancellationToken ct)
    {
        using var conn = ((IDbConnection)_db.CreateConnection());

        var rows = await conn.QueryAsync<dynamic>(
            @"SELECT r.id, r.drug1_id, d1.name_vi as drug1_name, r.drug2_id, d2.name_vi as drug2_name,
                     r.severity, r.description, r.evidence_level
              FROM diab_his_pha_ddi_rules r
              JOIN diab_his_pha_drugs d1 ON d1.id = r.drug1_id
              JOIN diab_his_pha_drugs d2 ON d2.id = r.drug2_id
              WHERE (r.drug1_id = @id OR r.drug2_id = @id) AND r.deleted_at IS NULL",
            new { id = q.Id });

        var rules = rows.Select(r => new DdiRule(
            ((object)r.id).ToString()!, ((object)r.drug1_id).ToString()!, (string)r.drug1_name,
            ((object)r.drug2_id).ToString()!, (string)r.drug2_name,
            (string)r.severity, (string)r.description, (string)r.evidence_level)).ToList();

        return Result<IReadOnlyList<DdiRule>>.Success(rules);
    }
}

public class ListDrugCategoriesHandler : IRequestHandler<ListDrugCategoriesQuery, Result<IReadOnlyList<DrugCategory>>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;

    public ListDrugCategoriesHandler(IDapperConnectionFactory db, ICurrentUser currentUser) { _db = db; _currentUser = currentUser; }

    public async Task<Result<IReadOnlyList<DrugCategory>>> Handle(ListDrugCategoriesQuery q, CancellationToken ct)
    {
        using var conn = ((IDbConnection)_db.CreateConnection());
        var tenantId = _currentUser.TenantId!.Value;

        var rows = await conn.QueryAsync<dynamic>(
            "SELECT id, code, name, parent_id FROM diab_his_pha_drug_categories WHERE tenant_id = @tenantId AND deleted_at IS NULL ORDER BY name",
            new { tenantId });

        var cats = rows.Select(r => new DrugCategory((string)r.id, (string)r.code, (string)r.name, (string?)r.parent_id)).ToList();
        return Result<IReadOnlyList<DrugCategory>>.Success(cats);
    }
}

public class CreateDrugCategoryHandler : IRequestHandler<CreateDrugCategoryCommand, Result<DrugCategory>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;

    public CreateDrugCategoryHandler(IDapperConnectionFactory db, ICurrentUser currentUser) { _db = db; _currentUser = currentUser; }

    public async Task<Result<DrugCategory>> Handle(CreateDrugCategoryCommand cmd, CancellationToken ct)
    {
        using var conn = ((IDbConnection)_db.CreateConnection());
        var tenantId = _currentUser.TenantId!.Value;
        var id = Guid.NewGuid().ToString();

        await conn.ExecuteAsync(
            "INSERT INTO diab_his_pha_drug_categories (id, tenant_id, code, name, parent_id) VALUES (@id, @tenantId, @code, @name, @parentId)",
            new { id, tenantId, code = cmd.Request.Code, name = cmd.Request.Name, parentId = cmd.Request.ParentId });

        return Result<DrugCategory>.Success(new DrugCategory(id, cmd.Request.Code, cmd.Request.Name, cmd.Request.ParentId));
    }
}

public class SyncCucQldHandler : IRequestHandler<SyncCucQldCommand, Result<SyncJobResponse>>
{
    private readonly IDrugCucQldSync _sync;

    public SyncCucQldHandler(IDrugCucQldSync sync) { _sync = sync; }

    public async Task<Result<SyncJobResponse>> Handle(SyncCucQldCommand cmd, CancellationToken ct)
    {
        var jobId = await _sync.EnqueueSyncJobAsync(cmd.Mode, cmd.Since, ct);
        return Result<SyncJobResponse>.Success(new SyncJobResponse(jobId));
    }
}

// --- Internal row types -----------------------------------------------
internal class DrugRow
{
    // Id co the la INT (diab_his_pha_drugs.ID legacy) hoac CHAR(36) UUID tuy moi truong.
    // Dung object de Dapper khong throw IConvertible exception, sau do .ToString() khi map.
    public object Id { get; set; } = string.Empty;
    public int TenantId { get; set; }
    public string? Code { get; set; }
    public string? NameVi { get; set; }
    public string? NameEn { get; set; }
    public string? GenericName { get; set; }
    public string? AtcCode { get; set; }
    public string? Strength { get; set; }
    public string? Unit { get; set; }
    public string? Form { get; set; }
    public string? Manufacturer { get; set; }
    public string? Country { get; set; }
    public decimal? Price { get; set; }
    public int? CategoryId { get; set; }
    public int RequiresPrescription { get; set; }
    public int IsPsychotropic { get; set; }
    public int IsNarcotic { get; set; }
    public string? DtqgDrugCode { get; set; }
    public string? Status { get; set; }
    public int InteractionsCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
