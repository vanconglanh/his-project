using ProDiabHis.Application.Pharmacy.Prescriptions;
using ProDiabHis.Infrastructure.Reports;
using Xunit;

namespace ProDiabHis.UnitTests.Pharmacy;

public class PrescriptionPdfBuilderTests
{
    [Fact]
    public void Build_VoiDuLieuDayDu_RaPdfKhongNemLoi()
    {
        var items = new List<PrescriptionPdfItem>
        {
            new(1, "Metformin", "500mg", "viên", 60, "1 viên", "2 lần/ngày", "ORAL", 30, "Uống sau ăn"),
            new(2, "Thuốc có tên rất dài để kiểm tra xuống dòng trong bảng đơn thuốc A5 dọc", "1000mg/5ml", "chai", 2.5m, "10ml", "3 lần/ngày", "ORAL", 7, "Lắc đều trước khi dùng, uống cách xa bữa ăn"),
        };
        var data = new PrescriptionPdfData("DT-001", new DateTime(2026, 10, 27), "Uống nhiều nước",
            "Phòng khám Pro-Diab", "123 Đường Dài, Quận 1, TP Hồ Chí Minh", "0901234567", "79001", null,
            "Nguyễn Văn A", "MALE", new DateOnly(1980, 1, 1), "HCM", "E11", "Đái tháo đường type 2",
            "BS. Trần B", items, "Công ty TNHH Pro-Diab", "Chăm sóc tiểu đường", "prodiab.vn", "info@prodiab.vn");

        var pdf = new PrescriptionPdfBuilder().Build(data);
        Assert.True(pdf.Length > 1000);
    }

    [Fact]
    public void Build_KhongCoLoiDanVaItem_VanRaPdf()
    {
        var data = new PrescriptionPdfData("DT-002", DateTime.Now, null, "PK", null, null, null, null,
            "Lê C", null, null, null, null, null, null, new List<PrescriptionPdfItem>());
        Assert.True(new PrescriptionPdfBuilder().Build(data).Length > 1000);
    }
}
