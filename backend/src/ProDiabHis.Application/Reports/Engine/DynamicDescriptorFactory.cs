namespace ProDiabHis.Application.Reports.Engine;

/// <summary>
/// Dung 1 <see cref="ReportDescriptor"/> "dong" tu 1 <see cref="ReportDefinition"/> (do nguoi dung tu tao)
/// + Dataset whitelist cua no. Descriptor sinh ra chay xuyen suot pipeline co san (GenericReportDataService,
/// PDF/Excel exporter, catalog) khong can sua gi them.
/// </summary>
public static class DynamicDescriptorFactory
{
    public const string PdfTypeCode = "UDR";
    public const int DataLimit = 5000;
    public const int PreviewLimit = 200;

    public static ReportDescriptor Create(ReportDefinition definition, Dataset dataset, int limit = DataLimit)
    {
        var calcFields = definition.CalcFields ?? Array.Empty<ReportDefinitionCalcField>();

        var columns = new List<ReportColumn>();
        foreach (var col in definition.Columns)
        {
            var calcField = calcFields.FirstOrDefault(c => string.Equals(c.Key, col.Field, StringComparison.OrdinalIgnoreCase));
            var alias = SafeQueryBuilder.SanitizeAlias(col.Field);

            ReportColumnType dataType;
            if (calcField is not null)
            {
                dataType = ParseColumnType(calcField.DataType);
            }
            else
            {
                var field = dataset.FindField(col.Field)
                    ?? throw new ReportValidationException("REPORT_DEFINITION_INVALID", $"Trường '{col.Field}' không thuộc dataset '{dataset.Key}'");
                dataType = field.DataType;
            }

            var align = dataType is ReportColumnType.Money or ReportColumnType.Number
                ? ReportAlign.Right
                : ReportAlign.Left;

            columns.Add(new ReportColumn(alias, col.Label, dataType, align, 1f, col.IsSubtotal));
        }

        var kpis = new List<ReportKpiSpec>();
        foreach (var kpi in definition.Kpis)
        {
            // KPI phai tham chieu dung 1 cot measure da chon (cung field + cung phep gop) — tranh phai chay
            // them truy van rieng cho tung KPI (giu SafeQueryBuilder la nguon SQL DUY NHAT).
            var matchedColumn = definition.Columns.FirstOrDefault(c =>
                c.Agg == kpi.Agg && string.Equals(c.Field, kpi.Field, StringComparison.OrdinalIgnoreCase));

            if (matchedColumn is null)
                throw new ReportValidationException("REPORT_DEFINITION_INVALID",
                    $"KPI '{kpi.Label}' phải tham chiếu 1 cột số đo đã chọn trong báo cáo (field='{kpi.Field}', agg='{kpi.Agg}')");

            var alias = SafeQueryBuilder.SanitizeAlias(kpi.Field);
            var field = dataset.FindField(kpi.Field)!;
            var isMoney = field.DataType == ReportColumnType.Money;

            kpis.Add(new ReportKpiSpec(
                kpi.Label,
                "#F0FDFA",
                rows => rows.Sum(r => ReportValueConverter.ToDecimal(ReportValueConverter.Get(r, alias))),
                isMoney));
        }

        var inputForQuery = new ReportDefinitionInput(
            definition.Title, definition.DatasetKey, definition.Columns, definition.Filters,
            definition.GroupBy, definition.Sort, definition.Kpis, definition.Chart,
            definition.ViewType, definition.Visibility, calcFields, definition.SharedRoles);

        return new ReportDescriptor
        {
            Code = definition.Code,
            Title = definition.Title,
            Group = ReportGroupCategory.UserDefined,
            GroupOrder = 0,
            Icon = "layout-grid",
            Columns = columns,
            GroupByKey = null,
            ShowGroupCount = false,
            Kpis = kpis,
            Filters = Array.Empty<ReportFilter>(),
            PdfTypeCode = PdfTypeCode,
            ViewType = definition.ViewType,
            Chart = definition.Chart,
            BuildQuery = ctx => SafeQueryBuilder.Build(dataset, inputForQuery, ctx, limit)
        };
    }

    /// <summary>Parse chuoi data_type cua calc field (nhap tu request/JSON) sang <see cref="ReportColumnType"/>.
    /// Chi chap nhan dung ten enum (khong phan biet hoa/thuong) — sai -> 400 REPORT_DEFINITION_INVALID.</summary>
    private static ReportColumnType ParseColumnType(string raw)
        => Enum.TryParse<ReportColumnType>(raw, ignoreCase: true, out var type)
            ? type
            : throw new ReportValidationException("REPORT_DEFINITION_INVALID", $"Kiểu dữ liệu '{raw}' không hợp lệ cho cột tính toán");
}
