namespace ProDiabHis.Application.Reports.Engine;

/// <summary>Nhom bao cao (dung de sap xep menu FE + phan quyen theo nhom).</summary>
public enum ReportGroupCategory
{
    Financial,
    Clinical,
    Statistics,
    Bhyt,
    Pharmacy
}

/// <summary>Huong trang PDF — tu suy ra theo so cot (>=11 cot => Landscape) khi khoi tao descriptor.</summary>
public enum ReportOrientation
{
    Portrait,
    Landscape
}

public enum ReportColumnType
{
    Text,
    Money,
    Number,
    Date,
    DateTime,
    Enum
}

public enum ReportAlign
{
    Left,
    Right,
    Center
}

public enum ReportFilterType
{
    DateRange,
    Select,
    MultiSelect,
    Enum
}

/// <summary>Mo ta 1 cot du lieu cua bao cao (dung chung cho JSON grid / PDF / Excel).</summary>
public record ReportColumn(
    string Key,
    string Label,
    ReportColumnType Type,
    ReportAlign Align = ReportAlign.Left,
    float Width = 1f,
    bool IsGroupSubtotal = false);

/// <summary>Mo ta 1 KPI card hien thi dau bao cao — tinh tu du lieu that (khong bia).</summary>
public record ReportKpiSpec(
    string Label,
    string Tint,
    Func<IReadOnlyList<IDictionary<string, object?>>, decimal> Compute,
    bool IsMoney = true);

/// <summary>Mo ta 1 filter tren man hinh xuat bao cao.</summary>
public record ReportFilter(
    string Key,
    string Label,
    ReportFilterType Type,
    string? OptionsSource = null,
    bool Required = false);

/// <summary>Ngu canh truyen vao khi build cau SQL cho 1 bao cao — luon co TenantId + khoang ngay + filter rieng.</summary>
public record ReportQueryContext(
    int TenantId,
    DateOnly From,
    DateOnly To,
    IReadOnlyDictionary<string, string?> Filters)
{
    public string? Filter(string key) => Filters.TryGetValue(key, out var v) ? v : null;

    public Guid? FilterGuid(string key)
        => Guid.TryParse(Filter(key), out var g) ? g : null;

    public int? FilterInt(string key)
        => int.TryParse(Filter(key), out var i) ? i : null;
}

/// <summary>
/// Mo ta config-driven cho 1 bao cao — thay the viec liet ke tay report o nhieu noi
/// (ReportsController switch / ReportCodeGenerator.TypeCode switch / GetReportPdfHandler switch / FE).
/// Dang ky trong <see cref="IReportRegistry"/>.
/// </summary>
public record ReportDescriptor
{
    public required string Code { get; init; }
    public required string Title { get; init; }
    public required ReportGroupCategory Group { get; init; }
    public required int GroupOrder { get; init; }
    public required string Icon { get; init; }
    public ReportOrientation Orientation { get; init; }
    public required IReadOnlyList<ReportColumn> Columns { get; init; }

    /// <summary>Cot dung de group-by (vd "collectorName"). Null neu bao cao khong nhom.</summary>
    public string? GroupByKey { get; init; }

    /// <summary>Hien thi dong dem so phieu/dong trong tung nhom.</summary>
    public bool ShowGroupCount { get; init; }

    public IReadOnlyList<ReportKpiSpec> Kpis { get; init; } = Array.Empty<ReportKpiSpec>();

    public IReadOnlyList<ReportFilter> Filters { get; init; } = Array.Empty<ReportFilter>();

    /// <summary>Build cau SQL Dapper (parameterized) tu ngu canh. BAT BUOC WHERE tenant_id = @tenantId.</summary>
    public required Func<ReportQueryContext, (string Sql, object Parameters)> BuildQuery { get; init; }

    /// <summary>Ma 2-3 ky tu dung cho ReportCodeGenerator (vd "RVD" cho revenue-daily).</summary>
    public required string PdfTypeCode { get; init; }

    /// <summary>Tu suy huong trang theo so cot: >=11 cot => Landscape, con lai Portrait.</summary>
    public static ReportOrientation OrientationFor(int columnCount)
        => columnCount >= 11 ? ReportOrientation.Landscape : ReportOrientation.Portrait;
}
