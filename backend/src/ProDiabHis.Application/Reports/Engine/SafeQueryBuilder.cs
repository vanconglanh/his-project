using Dapper;

namespace ProDiabHis.Application.Reports.Engine;

/// <summary>
/// Toan tu filter duoc phep — TAP CO DINH, khong bao gio nhan chuoi tu do tu client.
/// </summary>
public static class ReportFilterOperators
{
    public const string Eq = "=";
    public const string Ne = "<>";
    public const string In = "in";
    public const string Between = "between";
    public const string Like = "like";
    public const string Gt = ">";
    public const string Lt = "<";
    public const string Gte = ">=";
    public const string Lte = "<=";

    private static readonly HashSet<string> Allowed = new(StringComparer.OrdinalIgnoreCase)
        { Eq, Ne, In, Between, Like, Gt, Lt, Gte, Lte };

    public static bool IsAllowed(string op) => Allowed.Contains(op);
}

/// <summary>
/// Sinh cau SQL tham so hoa (Dapper) tu 1 Dataset whitelist + dinh nghia bao cao do nguoi dung chon
/// (field/agg/filter/groupBy/sort) — KHONG BAO GIO noi suy chuoi nguoi dung vao SQL. Moi field chi duoc
/// resolve tu <see cref="Dataset.FindField"/> sang <see cref="DatasetField.SqlExpr"/> (bieu thuc noi bo,
/// do dev khai bao san, khong the client kiem soat). Gia tri filter luon di qua Dapper DynamicParameters.
/// </summary>
public static class SafeQueryBuilder
{
    public const int MaxColumns = 20;
    public const int MaxFilters = 15;
    public const int MaxGroupByDims = 3;
    public const int MaxDateRangeDays = 366;

    /// <summary>Sinh SQL + tham so cho 1 truy van bao cao dong. <paramref name="limit"/> la LIMIT cuoi cung
    /// (data = 5000, preview = 200 — do caller quyet dinh, KHONG lay tu client).</summary>
    public static (string Sql, DynamicParameters Parameters) Build(
        Dataset dataset,
        ReportDefinitionInput definition,
        ReportQueryContext ctx,
        int limit)
    {
        if (dataset is null)
            throw new ReportValidationException("REPORT_DEFINITION_INVALID", "Dataset không hợp lệ");

        if (definition.Columns is null || definition.Columns.Count == 0)
            throw new ReportValidationException("REPORT_DEFINITION_INVALID", "Báo cáo phải có ít nhất 1 cột");

        if (definition.Columns.Count > MaxColumns)
            throw new ReportValidationException("REPORT_DEFINITION_INVALID", $"Số cột vượt giới hạn cho phép ({MaxColumns})");

        var filters = definition.Filters ?? Array.Empty<ReportDefinitionFilter>();
        if (filters.Count > MaxFilters)
            throw new ReportValidationException("REPORT_DEFINITION_INVALID", $"Số bộ lọc vượt giới hạn cho phép ({MaxFilters})");

        if (ctx.To.DayNumber - ctx.From.DayNumber > MaxDateRangeDays)
            throw new ReportValidationException("REPORT_INVALID_DATE_RANGE", $"Khoảng thời gian báo cáo không được vượt quá {MaxDateRangeDays} ngày");

        if (ctx.From > ctx.To)
            throw new ReportValidationException("REPORT_INVALID_DATE_RANGE", "Ngày bắt đầu phải nhỏ hơn hoặc bằng ngày kết thúc");

        // ---- Resolve cot: dim (Agg == null) / measure (Agg != null) — TU WHITELIST DATASET ---- //
        var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var dimSelects = new List<(string Alias, string Expr)>();
        var measureSelects = new List<(string Alias, string Expr)>();

        foreach (var col in definition.Columns)
        {
            if (string.IsNullOrWhiteSpace(col.Field))
                throw new ReportValidationException("REPORT_DEFINITION_INVALID", "Cột thiếu tên trường");

            var field = dataset.FindField(col.Field)
                ?? throw new ReportValidationException("REPORT_DEFINITION_INVALID", $"Trường '{col.Field}' không thuộc dataset '{dataset.Key}'");

            var alias = SanitizeAlias(col.Field);
            if (!seenKeys.Add(alias))
                throw new ReportValidationException("REPORT_DEFINITION_INVALID", $"Trường '{col.Field}' bị lặp lại");

            if (col.Agg is null)
            {
                if (field.Role != DatasetFieldRole.Dimension)
                    throw new ReportValidationException("REPORT_DEFINITION_INVALID", $"Trường '{col.Field}' là số đo, phải chọn phép gộp (agg)");
                dimSelects.Add((alias, field.SqlExpr));
            }
            else
            {
                if (field.Role != DatasetFieldRole.Measure)
                    throw new ReportValidationException("REPORT_DEFINITION_INVALID", $"Trường '{col.Field}' là chiều (dimension), không thể gộp");
                if (!field.AllowedAggregations.Contains(col.Agg.Value))
                    throw new ReportValidationException("REPORT_DEFINITION_INVALID", $"Phép gộp '{col.Agg}' không hợp lệ cho trường '{col.Field}'");
                measureSelects.Add((alias, BuildAggExpr(col.Agg.Value, field.SqlExpr)));
            }
        }

        if (dimSelects.Count > MaxGroupByDims)
            throw new ReportValidationException("REPORT_DEFINITION_INVALID", $"Số chiều gộp nhóm vượt giới hạn cho phép ({MaxGroupByDims})");

        // ---- WHERE: tenant + soft-delete (bake san trong Dataset.BaseWhereSql) + date range + filters nguoi dung ---- //
        var p = new DynamicParameters();
        p.Add("tenantId", ctx.TenantId);
        p.Add("from", ctx.From.ToDateTime(TimeOnly.MinValue));
        p.Add("to", ctx.To.ToDateTime(TimeOnly.MaxValue));

        var dateField = dataset.FindField(dataset.DateFieldKey)
            ?? throw new ReportValidationException("REPORT_DEFINITION_INVALID", $"Dataset '{dataset.Key}' thiếu cấu hình trường ngày");

        var whereClauses = new List<string> { dataset.BaseWhereSql, $"{dateField.SqlExpr} BETWEEN @from AND @to" };

        var paramIndex = 0;
        foreach (var f in filters)
        {
            if (string.IsNullOrWhiteSpace(f.Field))
                throw new ReportValidationException("REPORT_DEFINITION_INVALID", "Bộ lọc thiếu tên trường");

            if (!ReportFilterOperators.IsAllowed(f.Op))
                throw new ReportValidationException("REPORT_DEFINITION_INVALID", $"Toán tử lọc '{f.Op}' không hợp lệ");

            var field = dataset.FindField(f.Field)
                ?? throw new ReportValidationException("REPORT_DEFINITION_INVALID", $"Trường lọc '{f.Field}' không thuộc dataset '{dataset.Key}'");

            if (field.Role != DatasetFieldRole.Dimension)
                throw new ReportValidationException("REPORT_DEFINITION_INVALID", $"Chỉ được lọc trên chiều (dimension) — '{f.Field}' là số đo");

            var values = f.Value ?? Array.Empty<string?>();

            switch (f.Op.ToLowerInvariant())
            {
                case ReportFilterOperators.In:
                    if (values.Count == 0)
                        throw new ReportValidationException("REPORT_DEFINITION_INVALID", $"Toán tử 'in' cần ít nhất 1 giá trị cho '{f.Field}'");
                    var pn = $"f{paramIndex++}";
                    p.Add(pn, values);
                    whereClauses.Add($"{field.SqlExpr} IN @{pn}");
                    break;

                case ReportFilterOperators.Between:
                    if (values.Count != 2)
                        throw new ReportValidationException("REPORT_DEFINITION_INVALID", $"Toán tử 'between' cần đúng 2 giá trị cho '{f.Field}'");
                    var pb1 = $"f{paramIndex++}";
                    var pb2 = $"f{paramIndex++}";
                    p.Add(pb1, values[0]);
                    p.Add(pb2, values[1]);
                    whereClauses.Add($"{field.SqlExpr} BETWEEN @{pb1} AND @{pb2}");
                    break;

                default:
                    if (values.Count != 1)
                        throw new ReportValidationException("REPORT_DEFINITION_INVALID", $"Toán tử '{f.Op}' cần đúng 1 giá trị cho '{f.Field}'");
                    var pv = $"f{paramIndex++}";
                    var sqlOp = f.Op.ToLowerInvariant() == ReportFilterOperators.Like ? "LIKE" : f.Op;
                    p.Add(pv, values[0]);
                    whereClauses.Add($"{field.SqlExpr} {sqlOp} @{pv}");
                    break;
            }
        }

        // ---- SELECT / GROUP BY ---- //
        var selectParts = new List<string>();
        selectParts.AddRange(dimSelects.Select(d => $"{d.Expr} AS `{d.Alias}`"));
        selectParts.AddRange(measureSelects.Select(m => $"{m.Expr} AS `{m.Alias}`"));

        var sql = $"SELECT {string.Join(", ", selectParts)} FROM {dataset.FromSql} WHERE {string.Join(" AND ", whereClauses)}";

        if (dimSelects.Count > 0 && measureSelects.Count > 0)
            sql += $" GROUP BY {string.Join(", ", dimSelects.Select(d => d.Expr))}";

        // ---- ORDER BY: chi chap nhan field da co trong select list (theo alias) ---- //
        var sort = definition.Sort ?? Array.Empty<ReportDefinitionSort>();
        if (sort.Count > 0)
        {
            var orderParts = new List<string>();
            foreach (var s in sort)
            {
                var alias = SanitizeAlias(s.Field);
                if (!seenKeys.Contains(alias))
                    throw new ReportValidationException("REPORT_DEFINITION_INVALID", $"Trường sắp xếp '{s.Field}' không nằm trong danh sách cột đã chọn");
                orderParts.Add($"`{alias}` {(s.Desc ? "DESC" : "ASC")}");
            }
            sql += $" ORDER BY {string.Join(", ", orderParts)}";
        }
        else if (dimSelects.Count > 0)
        {
            sql += $" ORDER BY `{dimSelects[0].Alias}`";
        }

        sql += $" LIMIT {Math.Clamp(limit, 1, 5000)}";

        return (sql, p);
    }

    private static string BuildAggExpr(ReportAggregation agg, string sqlExpr) => agg switch
    {
        ReportAggregation.Sum => $"SUM({sqlExpr})",
        ReportAggregation.Count => $"COUNT({sqlExpr})",
        ReportAggregation.Avg => $"AVG({sqlExpr})",
        ReportAggregation.Min => $"MIN({sqlExpr})",
        ReportAggregation.Max => $"MAX({sqlExpr})",
        ReportAggregation.CountDistinct => $"COUNT(DISTINCT {sqlExpr})",
        _ => throw new ReportValidationException("REPORT_DEFINITION_INVALID", $"Phép gộp '{agg}' không được hỗ trợ")
    };

    /// <summary>Alias cot chi cho phep chu/so/gach duoi — chan moi ky tu co the pha vo cau SQL du field key
    /// da duoc resolve tu whitelist (phong thu them 1 lop).</summary>
    public static string SanitizeAlias(string key)
    {
        var alias = new string(key.Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray());
        if (string.IsNullOrEmpty(alias))
            throw new ReportValidationException("REPORT_DEFINITION_INVALID", $"Tên trường '{key}' không hợp lệ");
        return alias;
    }
}
