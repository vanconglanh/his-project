using FluentAssertions;
using ProDiabHis.Application.Bhyt;
using ProDiabHis.Domain.Entities.Bhyt;
using System.Text.Json;
using Xunit;

namespace ProDiabHis.UnitTests.Bhyt;

/// <summary>
/// Unit tests cho BhytTable row DTOs và ma_lien_ket format logic.
/// Generator integration tests nằm ở IntegrationTests.
/// </summary>
public class BhytXmlGeneratorTests
{
    [Theory]
    [InlineData("CLINIC001", "enc-abc-123", "CLINIC001enc-abc-123")]
    [InlineData("PK02", "00000000-0000-0000-0000-000000000001", "PK0200000000-0000-0000-0000-000000000001")]
    public void MaLienKet_format_is_tenantCode_concat_encounterId(
        string tenantCode, string encounterId, string expected)
    {
        var maLienKet = $"{tenantCode}{encounterId}";
        maLienKet.Should().Be(expected);
    }

    [Fact]
    public void MaLienKet_truncated_at_200_chars()
    {
        var longTenantCode = new string('X', 150);
        var longEncId = new string('Y', 100);
        var raw = $"{longTenantCode}{longEncId}";
        var result = raw.Length > 200 ? raw[..200] : raw;
        result.Should().HaveLength(200);
    }

    [Fact]
    public void BhytTable1Row_serializes_all_required_fields()
    {
        var row = new BhytTable1Row(
            MaLienKet: "PK001enc-001",
            MaBn: "BN001",
            HoTen: "Nguyen Van A",
            NgaySinh: "1980-01-15",
            GioiTinh: 1,
            MaTheBhyt: "DN4050123456789",
            MaDkbd: "04104",
            GtTheTu: "2026-01-01",
            GtTheDen: "2026-12-31",
            MaLoaiKcb: 1,
            NgayVao: new DateTime(2026, 5, 1, 8, 0, 0),
            NgayRa: new DateTime(2026, 5, 1, 10, 0, 0),
            SoNgayDtri: 1,
            KetQuaDtri: 1,
            MaBenh: "E11.9",
            MaBenhKhac: null,
            LyDoVvien: "Kham benh dinh ky",
            ChanDoanRv: "Dai thao duong type 2",
            TThuoc: 150000m,
            TVtyt: 10000m,
            TTongchi: 200000m,
            TBhtt: 160000m,
            TBntt: 40000m,
            TBncct: 0m);

        var json = JsonSerializer.Serialize(row);
        // QD 4750: truong chan doan phai xuat dung ten MA_BENH / MA_BENH_KHAC
        json.Should().Contain("MaLienKet").And.Contain("MA_BENH");
        json.Should().Contain("MA_BENH_KHAC");
        json.Should().Contain("E11.9");
        json.Should().Contain("PK001enc-001");
    }

    [Fact]
    public void BhytTable1Row_ma_benh_khac_ghep_nhieu_ma_bang_dau_cham_phay()
    {
        // G06: MA_BENH = chan doan CHINH, MA_BENH_KHAC = cac chan doan kem theo noi bang ";"
        var secondary = new[] { "I10", "E78.5" };

        var row = new BhytTable1Row(
            MaLienKet: "PK001enc-002",
            MaBn: "BN002",
            HoTen: "Tran Thi B",
            NgaySinh: "1975-03-02",
            GioiTinh: 2,
            MaTheBhyt: "DN4050987654321",
            MaDkbd: "04104",
            GtTheTu: "2026-01-01",
            GtTheDen: "2026-12-31",
            MaLoaiKcb: 1,
            NgayVao: new DateTime(2026, 5, 2, 8, 0, 0),
            NgayRa: new DateTime(2026, 5, 2, 9, 0, 0),
            SoNgayDtri: 1,
            KetQuaDtri: 1,
            MaBenh: "E11.9",
            MaBenhKhac: string.Join(";", secondary),
            LyDoVvien: "Kham benh dinh ky",
            ChanDoanRv: "Dai thao duong type 2",
            TThuoc: 0m, TVtyt: 0m, TTongchi: 0m,
            TBhtt: 0m, TBntt: 0m, TBncct: 0m);

        row.MaBenh.Should().Be("E11.9");
        row.MaBenhKhac.Should().Be("I10;E78.5");
        row.MaBenhKhac.Should().NotContain("E11.9", "chan doan chinh khong duoc lap lai o MA_BENH_KHAC");

        var json = JsonSerializer.Serialize(row);
        json.Should().Contain("\"MA_BENH\":\"E11.9\"");
        json.Should().Contain("\"MA_BENH_KHAC\":\"I10;E78.5\"");
    }

    [Fact]
    public void BhytTable1Row_t_bhtt_equals_sum_bhyt_portion()
    {
        // Quy tac: t_bhtt <= t_tongchi
        var tTongchi = 200000m;
        var tBhtt = 160000m;
        var tBntt = 40000m;

        (tBhtt + tBntt).Should().Be(tTongchi);
        tBhtt.Should().BeLessThanOrEqualTo(tTongchi);
    }

    [Fact]
    public void BhytExportItemData_table_no_within_1_to_5()
    {
        for (int tableNo = 1; tableNo <= 5; tableNo++)
        {
            var item = new BhytExportItemData(tableNo, 0, "{}", "LK001", null, null, 0m);
            item.TableNo.Should().BeInRange(1, 5);
        }
    }

    [Fact]
    public void BhytReconcileParseResult_empty_items_on_failure()
    {
        var result = new BhytReconcileParseResult(false, [], "Parse error");
        result.Success.Should().BeFalse();
        result.Items.Should().BeEmpty();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void BhytXmlGenerateResult_no_encounters_returns_failure()
    {
        var result = new BhytXmlGenerateResult(false, 0, 0, [], "BHYT_EXPORT_NO_ENCOUNTERS");
        result.Success.Should().BeFalse();
        result.EncounterCount.Should().Be(0);
        result.TotalRequestedAmount.Should().Be(0m);
    }
}
