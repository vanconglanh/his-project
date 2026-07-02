using System.Data;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Dapper;
using MediatR;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.Bhyt;

// ── List Exports ──────────────────────────────────────────────────────────────

public record ListBhytExportsQuery(
    string? PeriodMonth,
    string? Status,
    int Page,
    int PageSize) : IRequest<Result<PagedResult<BhytExportResponse>>>;

public class ListBhytExportsHandler : IRequestHandler<ListBhytExportsQuery, Result<PagedResult<BhytExportResponse>>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;

    public ListBhytExportsHandler(IDapperConnectionFactory db, ITenantProvider tenant)
    {
        _db = db; _tenant = tenant;
    }

    public async Task<Result<PagedResult<BhytExportResponse>>> Handle(ListBhytExportsQuery q, CancellationToken ct)
    {
        using var conn = (IDbConnection)_db.CreateConnection();

        var where = new StringBuilder("WHERE tenant_id=@t AND deleted_at IS NULL");
        var p = new DynamicParameters();
        p.Add("t", _tenant.TenantId);

        if (!string.IsNullOrEmpty(q.PeriodMonth)) { where.Append(" AND period_month=@pm"); p.Add("pm", q.PeriodMonth); }
        if (!string.IsNullOrEmpty(q.Status))      { where.Append(" AND status=@st");       p.Add("st", q.Status); }

        var total = await conn.ExecuteScalarAsync<int>(
            $"SELECT COUNT(*) FROM diab_his_int_bhyt_exports {where}", p);

        p.Add("offset", (q.Page - 1) * q.PageSize);
        p.Add("limit", q.PageSize);

        var rows = await conn.QueryAsync<dynamic>(
            $"SELECT * FROM diab_his_int_bhyt_exports {where} ORDER BY id DESC LIMIT @limit OFFSET @offset", p);

        var items = rows.Select(CreateBhytExportHandler.MapToResponse).ToList();

        return Result<PagedResult<BhytExportResponse>>.Success(
            new PagedResult<BhytExportResponse>(items, q.Page, q.PageSize, total));
    }
}

// ── Get Export ────────────────────────────────────────────────────────────────

public record GetBhytExportQuery(int Id) : IRequest<Result<BhytExportResponse>>;

public class GetBhytExportHandler : IRequestHandler<GetBhytExportQuery, Result<BhytExportResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;

    public GetBhytExportHandler(IDapperConnectionFactory db, ITenantProvider tenant)
    {
        _db = db; _tenant = tenant;
    }

    public async Task<Result<BhytExportResponse>> Handle(GetBhytExportQuery q, CancellationToken ct)
    {
        using var conn = (IDbConnection)_db.CreateConnection();
        var row = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT * FROM diab_his_int_bhyt_exports WHERE id=@id AND tenant_id=@t AND deleted_at IS NULL",
            new { id = q.Id, t = _tenant.TenantId });

        if (row == null)
            return Result<BhytExportResponse>.Failure("BHYT_EXPORT_NOT_FOUND", "Khong tim thay ky export BHYT");

        return Result<BhytExportResponse>.Success(CreateBhytExportHandler.MapToResponse(row));
    }
}

// ── Download XML by Table ─────────────────────────────────────────────────────

public record DownloadBhytTableXmlQuery(int ExportId, int TableNo) : IRequest<Result<byte[]>>;

public class DownloadBhytTableXmlHandler : IRequestHandler<DownloadBhytTableXmlQuery, Result<byte[]>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;

    public DownloadBhytTableXmlHandler(IDapperConnectionFactory db, ITenantProvider tenant)
    {
        _db = db; _tenant = tenant;
    }

    public async Task<Result<byte[]>> Handle(DownloadBhytTableXmlQuery q, CancellationToken ct)
    {
        using var conn = (IDbConnection)_db.CreateConnection();

        // Xac nhan export ton tai
        var export = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT id, status FROM diab_his_int_bhyt_exports WHERE id=@id AND tenant_id=@t AND deleted_at IS NULL",
            new { id = q.ExportId, t = _tenant.TenantId });

        if (export == null)
            return Result<byte[]>.Failure("BHYT_EXPORT_NOT_FOUND", "Khong tim thay ky export BHYT");

        var items = await conn.QueryAsync<dynamic>(
            "SELECT row_data_json FROM diab_his_int_bhyt_export_items WHERE export_id=@eid AND table_no=@tn ORDER BY record_index",
            new { eid = q.ExportId, tn = q.TableNo });

        // Build XML don gian tu row_data_json
        var xml = BuildTableXml(q.TableNo, items.ToList());
        return Result<byte[]>.Success(Encoding.UTF8.GetBytes(xml));
    }

    private static string BuildTableXml(int tableNo, List<dynamic> items)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.AppendLine($"<Bang{tableNo}>");
        int idx = 0;
        foreach (var item in items)
        {
            sb.AppendLine($"  <Row index=\"{idx++}\">");
            if (item.row_data_json is string json)
            {
                var doc = JsonDocument.Parse(json);
                foreach (var prop in doc.RootElement.EnumerateObject())
                    sb.AppendLine($"    <{prop.Name}>{System.Security.SecurityElement.Escape(prop.Value.ToString())}</{prop.Name}>");
            }
            sb.AppendLine("  </Row>");
        }
        sb.AppendLine($"</Bang{tableNo}>");
        return sb.ToString();
    }
}

// ── Download All ZIP ──────────────────────────────────────────────────────────

public record DownloadBhytAllXmlQuery(int ExportId) : IRequest<Result<byte[]>>;

public class DownloadBhytAllXmlHandler : IRequestHandler<DownloadBhytAllXmlQuery, Result<byte[]>>
{
    private readonly IMediator _mediator;

    public DownloadBhytAllXmlHandler(IMediator mediator) => _mediator = mediator;

    public async Task<Result<byte[]>> Handle(DownloadBhytAllXmlQuery q, CancellationToken ct)
    {
        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            for (int tableNo = 1; tableNo <= 5; tableNo++)
            {
                var tableResult = await _mediator.Send(new DownloadBhytTableXmlQuery(q.ExportId, tableNo), ct);
                if (!tableResult.IsSuccess)
                    return Result<byte[]>.Failure(tableResult.ErrorCode!, tableResult.ErrorMessage!);

                var entry = zip.CreateEntry($"bang{tableNo}.xml", CompressionLevel.Optimal);
                await using var entryStream = entry.Open();
                await entryStream.WriteAsync(tableResult.Value!, ct);
            }
        }
        return Result<byte[]>.Success(ms.ToArray());
    }
}

// ── List Items by Table ───────────────────────────────────────────────────────

public record ListBhytExportItemsQuery(int ExportId, int TableNo, int Page, int PageSize)
    : IRequest<Result<PagedResult<BhytExportItemResponse>>>;

public class ListBhytExportItemsHandler : IRequestHandler<ListBhytExportItemsQuery, Result<PagedResult<BhytExportItemResponse>>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;

    public ListBhytExportItemsHandler(IDapperConnectionFactory db, ITenantProvider tenant)
    {
        _db = db; _tenant = tenant;
    }

    public async Task<Result<PagedResult<BhytExportItemResponse>>> Handle(ListBhytExportItemsQuery q, CancellationToken ct)
    {
        using var conn = (IDbConnection)_db.CreateConnection();

        var total = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM diab_his_int_bhyt_export_items WHERE export_id=@eid AND table_no=@tn AND tenant_id=@t",
            new { eid = q.ExportId, tn = q.TableNo, t = _tenant.TenantId });

        var rows = await conn.QueryAsync<dynamic>(
            @"SELECT * FROM diab_his_int_bhyt_export_items
              WHERE export_id=@eid AND table_no=@tn AND tenant_id=@t
              ORDER BY record_index
              LIMIT @limit OFFSET @offset",
            new { eid = q.ExportId, tn = q.TableNo, t = _tenant.TenantId,
                  limit = q.PageSize, offset = (q.Page - 1) * q.PageSize });

        var items = rows.Select(MapItem).ToList();
        return Result<PagedResult<BhytExportItemResponse>>.Success(
            new PagedResult<BhytExportItemResponse>(items, q.Page, q.PageSize, total));
    }

    internal static BhytExportItemResponse MapItem(dynamic row) => new(
        Id: (int)row.id,
        ExportId: (int)row.export_id,
        TableNo: (int)row.table_no,
        RecordIndex: (int)(row.record_index ?? 0),
        RowDataJson: row.row_data_json != null
            ? JsonSerializer.Deserialize<object>((string)row.row_data_json) : null,
        SourceEncounterId: (string?)row.source_encounter_id,
        SourceBillingId: (string?)row.source_billing_id,
        MaLienKet: (string?)row.ma_lien_ket,
        RequestAmount: (decimal)(row.request_amount ?? 0m),
        ApprovedAmount: (decimal?)row.approved_amount,
        RejectionCode: (string?)row.rejection_code,
        RejectionReason: (string?)row.rejection_reason);
}

// ── Get Item Detail ───────────────────────────────────────────────────────────

public record GetBhytExportItemQuery(int ExportId, int TableNo, Guid RowId)
    : IRequest<Result<BhytExportItemResponse>>;

public class GetBhytExportItemHandler : IRequestHandler<GetBhytExportItemQuery, Result<BhytExportItemResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;

    public GetBhytExportItemHandler(IDapperConnectionFactory db, ITenantProvider tenant)
    {
        _db = db; _tenant = tenant;
    }

    public async Task<Result<BhytExportItemResponse>> Handle(GetBhytExportItemQuery q, CancellationToken ct)
    {
        using var conn = (IDbConnection)_db.CreateConnection();
        var row = await conn.QueryFirstOrDefaultAsync<dynamic>(
            @"SELECT * FROM diab_his_int_bhyt_export_items
              WHERE export_id=@eid AND table_no=@tn AND tenant_id=@t AND id=@rid",
            new { eid = q.ExportId, tn = q.TableNo, t = _tenant.TenantId, rid = q.RowId.ToString() });

        if (row == null)
            return Result<BhytExportItemResponse>.Failure("BHYT_EXPORT_NOT_FOUND", "Khong tim thay dong du lieu");

        return Result<BhytExportItemResponse>.Success(ListBhytExportItemsHandler.MapItem(row));
    }
}
