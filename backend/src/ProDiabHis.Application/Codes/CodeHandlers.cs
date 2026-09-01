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
    private readonly ICodeResolver _resolver;

    public GetCodeItemsQueryHandler(ICodeResolver resolver) => _resolver = resolver;

    public async Task<Result<IReadOnlyList<CodeItemDto>>> Handle(GetCodeItemsQuery q, CancellationToken ct)
    {
        var items = await _resolver.GetAsync(q.GroupId, ct);
        var result = items.Select(i => new CodeItemDto(i.Code, i.Name)).ToList();
        return Result<IReadOnlyList<CodeItemDto>>.Success(result.AsReadOnly());
    }
}

// ────────────────────────────────────────────────
// Nap nhieu nhom 1 lan (batch)
// ────────────────────────────────────────────────
public class GetCodeBatchQueryHandler
    : IRequestHandler<GetCodeBatchQuery, Result<IReadOnlyDictionary<string, IReadOnlyList<CodeItemDto>>>>
{
    private readonly ICodeResolver _resolver;

    public GetCodeBatchQueryHandler(ICodeResolver resolver) => _resolver = resolver;

    public async Task<Result<IReadOnlyDictionary<string, IReadOnlyList<CodeItemDto>>>> Handle(
        GetCodeBatchQuery q, CancellationToken ct)
    {
        var map = new Dictionary<string, IReadOnlyList<CodeItemDto>>();
        var ids = q.GroupIds.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
        if (ids.Count == 0)
            return Result<IReadOnlyDictionary<string, IReadOnlyList<CodeItemDto>>>.Success(map);

        foreach (var id in ids)
        {
            var items = await _resolver.GetAsync(id, ct);
            map[id] = items.Select(i => new CodeItemDto(i.Code, i.Name)).ToList();
        }

        return Result<IReadOnlyDictionary<string, IReadOnlyList<CodeItemDto>>>.Success(map);
    }
}
