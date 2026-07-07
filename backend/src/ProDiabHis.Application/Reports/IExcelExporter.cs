namespace ProDiabHis.Application.Reports;

public interface IExcelExporter
{
    /// <summary>Xuat danh sach object sang file Excel, tra ve bytes.</summary>
    byte[] Export<T>(IEnumerable<T> rows, string sheetName);

    /// <summary>
    /// Xuat bao cao config-driven (Report Engine) sang Excel theo ReportDescriptor.Columns.
    /// Freeze 2 hang dau + cot dau, co dong Cong nhom / TONG CONG.
    /// </summary>
    byte[] ExportGeneric(
        Engine.ReportDescriptor descriptor,
        Engine.ReportDataResult data,
        string sheetName);
}
