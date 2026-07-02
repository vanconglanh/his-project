namespace ProDiabHis.Application.Reports;

public interface IExcelExporter
{
    /// <summary>Xuat danh sach object sang file Excel, tra ve bytes.</summary>
    byte[] Export<T>(IEnumerable<T> rows, string sheetName);
}
