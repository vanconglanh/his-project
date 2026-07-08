using Dapper;
using MediatR;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.ICD10;

// DTOs
public record Icd10Response(
    string Code,
    string NameVi,
    string? NameEn,
    string? Category,
    string? ParentCode,
    bool IsBillable);

public record Icd10CategoryDto(
    string CodeRange,
    string? Chapter,
    string? NameVi,
    string? NameEn,
    int Count);

// Queries
public record SearchIcd10Query(
    string Q,
    string? Type,
    string? Category,
    bool BillableOnly,
    int Limit)
    : IRequest<Result<IReadOnlyList<Icd10Response>>>;

public record GetIcd10ByCodeQuery(string Code) : IRequest<Result<Icd10Response>>;

public record GetIcd10CategoriesQuery() : IRequest<Result<IReadOnlyList<Icd10CategoryDto>>>;

// ────────────────────────────────────────────────
// Search
// ────────────────────────────────────────────────
public class SearchIcd10QueryHandler : IRequestHandler<SearchIcd10Query, Result<IReadOnlyList<Icd10Response>>>
{
    private readonly IDapperConnectionFactory _db;

    public SearchIcd10QueryHandler(IDapperConnectionFactory db) => _db = db;

    public async Task<Result<IReadOnlyList<Icd10Response>>> Handle(SearchIcd10Query q, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var limit = Math.Min(q.Limit, 100);

        var where = "WHERE 1=1";
        var p = new DynamicParameters();

        if (q.BillableOnly) { where += " AND is_billable=1"; }
        if (!string.IsNullOrEmpty(q.Category)) { where += " AND category=@Cat"; p.Add("Cat", q.Category); }

        // Full-text search nếu có FULLTEXT index, fallback về LIKE
        string orderClause;
        if (!string.IsNullOrWhiteSpace(q.Q))
        {
            var type = q.Type ?? "all";
            if (type == "code")
            {
                where += " AND code LIKE @Prefix";
                p.Add("Prefix", q.Q + "%");
                orderClause = "ORDER BY code";
            }
            else
            {
                // Yeu cau TAT CA cac tu khoa (AND) voi prefix match -> tranh khop mot manh chu
                var words = q.Q.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                var boolTerm = string.Join(" ", words.Select(w => "+" + w + "*"));

                where += " AND (MATCH(name_vi, name_en) AGAINST (@Term IN BOOLEAN MODE) OR code LIKE @Prefix OR name_vi LIKE @LikeTerm)";
                p.Add("Term", boolTerm);
                p.Add("Prefix", q.Q + "%");
                p.Add("LikeTerm", "%" + q.Q + "%");
                p.Add("Exact", q.Q);
                // Relevance: khop ma chinh xac -> ma prefix -> ten chua nguyen cum -> ma ngan (3 ky tu) truoc
                orderClause = @"ORDER BY (code = @Exact) DESC, (code LIKE @Prefix) DESC,
                                (name_vi LIKE @LikeTerm) DESC, is_billable DESC, CHAR_LENGTH(code), code";
            }
        }
        else
        {
            orderClause = "ORDER BY code";
        }

        p.Add("Limit", limit);

        IEnumerable<dynamic> rows;
        try
        {
            rows = await conn.QueryAsync<dynamic>(
                $"SELECT code, name_vi, name_en, category, parent_code, is_billable FROM diab_his_dict_icd10 {where} {orderClause} LIMIT @Limit", p);
        }
        catch
        {
            // Fallback nếu FULLTEXT chưa sẵn sàng
            var p2 = new DynamicParameters();
            var where2 = "WHERE 1=1";
            if (q.BillableOnly) where2 += " AND is_billable=1";
            if (!string.IsNullOrEmpty(q.Category)) { where2 += " AND category=@Cat"; p2.Add("Cat", q.Category); }
            if (!string.IsNullOrWhiteSpace(q.Q))
            {
                where2 += " AND (code LIKE @Term OR name_vi LIKE @Term OR name_en LIKE @Term)";
                p2.Add("Term", "%" + q.Q + "%");
            }
            p2.Add("Limit", limit);
            rows = await conn.QueryAsync<dynamic>(
                $"SELECT code, name_vi, name_en, category, parent_code, is_billable FROM diab_his_dict_icd10 {where2} ORDER BY code LIMIT @Limit", p2);
        }

        var result = rows.Select(r => new Icd10Response(
            (string)r.code, (string)r.name_vi, (string?)r.name_en,
            (string?)r.category, (string?)r.parent_code,
            r.is_billable is bool b ? b : (sbyte)r.is_billable == 1)).ToList();

        return Result<IReadOnlyList<Icd10Response>>.Success(result.AsReadOnly());
    }
}

// ────────────────────────────────────────────────
// Get by code
// ────────────────────────────────────────────────
public class GetIcd10ByCodeQueryHandler : IRequestHandler<GetIcd10ByCodeQuery, Result<Icd10Response>>
{
    private readonly IDapperConnectionFactory _db;

    public GetIcd10ByCodeQueryHandler(IDapperConnectionFactory db) => _db = db;

    public async Task<Result<Icd10Response>> Handle(GetIcd10ByCodeQuery q, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var row = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT code, name_vi, name_en, category, parent_code, is_billable FROM diab_his_dict_icd10 WHERE code=@Code",
            new { Code = q.Code });

        if (row is null) return Result<Icd10Response>.Failure("ICD10_NOT_FOUND", $"Không tìm thấy mã ICD-10: {q.Code}");

        return Result<Icd10Response>.Success(new Icd10Response(
            (string)row.code, (string)row.name_vi, (string?)row.name_en,
            (string?)row.category, (string?)row.parent_code,
            row.is_billable is bool b ? b : (sbyte)row.is_billable == 1));
    }
}

// ────────────────────────────────────────────────
// Categories
// ────────────────────────────────────────────────
public class GetIcd10CategoriesQueryHandler : IRequestHandler<GetIcd10CategoriesQuery, Result<IReadOnlyList<Icd10CategoryDto>>>
{
    private readonly IDapperConnectionFactory _db;

    public GetIcd10CategoriesQueryHandler(IDapperConnectionFactory db) => _db = db;

    public async Task<Result<IReadOnlyList<Icd10CategoryDto>>> Handle(GetIcd10CategoriesQuery q, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<dynamic>(@"
            SELECT category AS code_range, NULL AS chapter, MIN(name_vi) AS name_vi, MIN(name_en) AS name_en, COUNT(*) AS cnt
            FROM diab_his_dict_icd10
            WHERE category IS NOT NULL
            GROUP BY category
            ORDER BY category");

        var result = rows.Select(r => new Icd10CategoryDto(
            (string)r.code_range, (string?)r.chapter, (string?)r.name_vi, (string?)r.name_en, (int)r.cnt)).ToList();

        return Result<IReadOnlyList<Icd10CategoryDto>>.Success(result.AsReadOnly());
    }
}
