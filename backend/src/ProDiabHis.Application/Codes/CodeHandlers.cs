using Dapper;
using MediatR;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.Codes;

// ────────────────────────────────────────────────
// DTOs
// ────────────────────────────────────────────────
public record CodeGroupDto(string Id, string Name);

public record CodeItemDto(string Code, string Name);

// ────────────────────────────────────────────────
// Queries
// ────────────────────────────────────────────────
public record GetCodeGroupsQuery() : IRequest<Result<IReadOnlyList<CodeGroupDto>>>;

public record GetCodeItemsQuery(string GroupId) : IRequest<Result<IReadOnlyList<CodeItemDto>>>;

public record GetCodeBatchQuery(IReadOnlyList<string> GroupIds)
    : IRequest<Result<IReadOnlyDictionary<string, IReadOnlyList<CodeItemDto>>>>;

// ────────────────────────────────────────────────
// Danh sach nhom ma (code_master)
// ────────────────────────────────────────────────
public class GetCodeGroupsQueryHandler
    : IRequestHandler<GetCodeGroupsQuery, Result<IReadOnlyList<CodeGroupDto>>>
{
    private readonly IDapperConnectionFactory _db;

    public GetCodeGroupsQueryHandler(IDapperConnectionFactory db) => _db = db;

    public async Task<Result<IReadOnlyList<CodeGroupDto>>> Handle(GetCodeGroupsQuery q, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<dynamic>(@"
            SELECT id, name
            FROM diab_his_sys_code_master
            WHERE is_active = 1
            ORDER BY sort_order, id");

        var result = rows.Select(r => new CodeGroupDto((string)r.id, (string)r.name)).ToList();
        return Result<IReadOnlyList<CodeGroupDto>>.Success(result.AsReadOnly());
    }
}

// ────────────────────────────────────────────────
// Danh sach ma trong 1 nhom (code_detail)
// ────────────────────────────────────────────────
public class GetCodeItemsQueryHandler
    : IRequestHandler<GetCodeItemsQuery, Result<IReadOnlyList<CodeItemDto>>>
{
    private readonly IDapperConnectionFactory _db;

    public GetCodeItemsQueryHandler(IDapperConnectionFactory db) => _db = db;

    public async Task<Result<IReadOnlyList<CodeItemDto>>> Handle(GetCodeItemsQuery q, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<dynamic>(@"
            SELECT code, name
            FROM diab_his_sys_code_detail
            WHERE code_master_id = @GroupId AND is_active = 1
            ORDER BY sort_order, code",
            new { GroupId = q.GroupId });

        var result = rows.Select(r => new CodeItemDto((string)r.code, (string)r.name)).ToList();
        return Result<IReadOnlyList<CodeItemDto>>.Success(result.AsReadOnly());
    }
}

// ────────────────────────────────────────────────
// Nap nhieu nhom 1 lan (batch)
// ────────────────────────────────────────────────
public class GetCodeBatchQueryHandler
    : IRequestHandler<GetCodeBatchQuery, Result<IReadOnlyDictionary<string, IReadOnlyList<CodeItemDto>>>>
{
    private readonly IDapperConnectionFactory _db;

    public GetCodeBatchQueryHandler(IDapperConnectionFactory db) => _db = db;

    public async Task<Result<IReadOnlyDictionary<string, IReadOnlyList<CodeItemDto>>>> Handle(
        GetCodeBatchQuery q, CancellationToken ct)
    {
        var map = new Dictionary<string, IReadOnlyList<CodeItemDto>>();
        var ids = q.GroupIds.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
        if (ids.Count == 0)
            return Result<IReadOnlyDictionary<string, IReadOnlyList<CodeItemDto>>>.Success(map);

        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<dynamic>(@"
            SELECT code_master_id, code, name
            FROM diab_his_sys_code_detail
            WHERE code_master_id IN @Ids AND is_active = 1
            ORDER BY code_master_id, sort_order, code",
            new { Ids = ids });

        foreach (var id in ids)
            map[id] = new List<CodeItemDto>();

        foreach (var r in rows)
        {
            var gid = (string)r.code_master_id;
            ((List<CodeItemDto>)map[gid]).Add(new CodeItemDto((string)r.code, (string)r.name));
        }

        return Result<IReadOnlyDictionary<string, IReadOnlyList<CodeItemDto>>>.Success(map);
    }
}
