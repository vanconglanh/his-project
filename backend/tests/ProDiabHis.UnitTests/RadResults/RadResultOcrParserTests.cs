using ProDiabHis.Application.RadResults.Ocr;
using Xunit;

namespace ProDiabHis.UnitTests.RadResults;

public class RadResultOcrParserTests
{
    // Phieu X-quang nguc thang — layout tu do, 2 nhan chinh Mo ta / Ket luan
    private const string XrayChestText = @"
BENH VIEN ABC — KHOA CHAN DOAN HINH ANH
PHIEU KET QUA X-QUANG

Ho ten: Nguyen Van A    Tuoi: 45    Gioi: Nam
Ky thuat: X-quang nguc thang

Mo ta:
Hai phe truong sang, khong thay dam mo bat thuong.
Ron phoi hai ben khong to. Bong tim khong to, chi so tim nguc trong gioi han.
Goc suon hoanh hai ben nhon. Khong tran dich mang phoi.

Ket luan:
Hinh anh X-quang nguc trong gioi han binh thuong.

De nghi:
Tai kham khi co trieu chung ho keo dai.

Bac si thuc hien: BS. Tran Van B
Ngay 30 thang 08 nam 2026
";

    [Fact]
    public void Parse_XrayChest_TachDungMoTaVaKetLuan()
    {
        var r = RadResultOcrParser.Parse(XrayChestText);

        Assert.True(r.HasAnyExtracted);
        Assert.NotNull(r.Findings);
        Assert.Contains("Hai phe truong sang", r.Findings!);
        Assert.Contains("Khong tran dich mang phoi", r.Findings!);

        Assert.NotNull(r.Conclusion);
        Assert.Contains("trong gioi han binh thuong", r.Conclusion!);

        Assert.NotNull(r.Recommendations);
        Assert.Contains("Tai kham", r.Recommendations!);

        // Phan chu ky bac si KHONG bi gom vao noi dung y khoa
        Assert.DoesNotContain("Tran Van B", r.Findings ?? "");
        Assert.DoesNotContain("Tran Van B", r.Conclusion ?? "");
        Assert.DoesNotContain("Tran Van B", r.Recommendations ?? "");
    }

    [Fact]
    public void Parse_CoDauTiengViet_GiuNguyenDauTrongOutput()
    {
        const string text = @"
Mô tả: Gan kích thước bình thường, bờ đều, nhu mô đồng nhất.
Kết luận: Không phát hiện bất thường trên siêu âm ổ bụng.
";
        var r = RadResultOcrParser.Parse(text);

        // Output phai giu nguyen dau tieng Viet (khong bi chuan hoa mat dau)
        Assert.Contains("kích thước bình thường", r.Findings!);
        Assert.Contains("Không phát hiện bất thường", r.Conclusion!);
    }

    [Fact]
    public void Parse_NhanTrenCungDong_GomLuonPhanText()
    {
        const string text = "Kết quả: Nhu mô phổi đều. Kết luận: Bình thường.";
        var r = RadResultOcrParser.Parse(text);

        Assert.Equal("Nhu mô phổi đều.", r.Findings);
        Assert.Equal("Bình thường.", r.Conclusion);
    }

    [Fact]
    public void Parse_NhanXetVaHinhAnh_MapVaoFindings()
    {
        const string text = @"
Hình ảnh ghi nhận: Nhiều nốt mờ rải rác thùy trên phổi phải.
Chẩn đoán: Theo dõi lao phổi.
";
        var r = RadResultOcrParser.Parse(text);

        Assert.Contains("Nhiều nốt mờ", r.Findings!);
        Assert.Contains("Theo dõi lao phổi", r.Conclusion!);
    }

    [Fact]
    public void Parse_TextRong_KhongCrash_HasAnyExtractedFalse()
    {
        var r = RadResultOcrParser.Parse("");
        Assert.False(r.HasAnyExtracted);
        Assert.Null(r.Findings);
        Assert.Null(r.Conclusion);
    }

    [Fact]
    public void Parse_KhongCoNhanNhanBiet_KhongGomBuaBai()
    {
        // Text khong co bat ky nhan section nao -> khong gom gi (tranh nhet ca phieu vao 1 o)
        const string text = @"
BENH VIEN ABC
Ho ten benh nhan: Nguyen Van A
So phieu: 12345
";
        var r = RadResultOcrParser.Parse(text);
        Assert.False(r.HasAnyExtracted);
    }
}
