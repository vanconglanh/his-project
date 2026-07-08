namespace ProDiabHis.Application.Reports.Engine;

/// <summary>Vai tro cua 1 truong trong Dataset — Dimension (chieu, khong gop) hay Measure (so do, gop duoc).</summary>
public enum DatasetFieldRole
{
    Dimension,
    Measure
}

/// <summary>Phep gop cho phep tren 1 Measure. KHONG dung enum tu do — chi nhung gia tri nay duoc SafeQueryBuilder chap nhan.</summary>
public enum ReportAggregation
{
    Sum,
    Count,
    Avg,
    Min,
    Max,
    CountDistinct
}

/// <summary>
/// 1 truong duoc whitelist trong Dataset. <see cref="SqlExpr"/> la bieu thuc SQL noi bo (da bake san COLLATE
/// dung) — nguoi dung KHONG bao gio thay/sua chuoi nay, chi chon <see cref="Key"/> qua UI.
/// </summary>
public record DatasetField(
    string Key,
    string Label,
    DatasetFieldRole Role,
    string SqlExpr,
    ReportColumnType DataType,
    IReadOnlyList<ReportAggregation> AllowedAggregations)
{
    public static DatasetField Dimension(string key, string label, string sqlExpr, ReportColumnType dataType)
        => new(key, label, DatasetFieldRole.Dimension, sqlExpr, dataType, Array.Empty<ReportAggregation>());

    public static DatasetField Measure(string key, string label, string sqlExpr, ReportColumnType dataType, params ReportAggregation[] aggregations)
        => new(key, label, DatasetFieldRole.Measure, sqlExpr, dataType, aggregations);
}

/// <summary>
/// 1 nguon du lieu an toan (whitelist) do dev dinh nghia san — base table + joins co dinh (da bake COLLATE
/// dung theo tung bang) + danh sach truong duoc phep dung. Nguoi dung Report Builder CHI thao tac tren cac
/// Dataset nay, khong bao gio ghep bang/sua SQL tu do.
/// </summary>
public record Dataset(
    string Key,
    string Label,
    string FromSql,
    string BaseWhereSql,
    string DateFieldKey,
    IReadOnlyList<DatasetField> Fields)
{
    public DatasetField? FindField(string key) => Fields.FirstOrDefault(f => string.Equals(f.Key, key, StringComparison.OrdinalIgnoreCase));
}

/// <summary>Registry tap trung cac Dataset whitelist cho Report Builder (P1: 4 dataset).</summary>
public interface IDatasetRegistry
{
    IReadOnlyList<Dataset> GetAll();

    Dataset? GetByKey(string key);
}
