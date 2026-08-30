using System.Linq;
using FluentAssertions;
using ProDiabHis.Application.Branches;
using Xunit;

namespace ProDiabHis.UnitTests.Sprint20260830;

/// <summary>
/// Dot 5 da chi nhanh — BR-112/US-8.1: checklist go-live chi nhanh.
/// </summary>
public class BranchReadinessCalculatorTests
{
    private static BranchReadinessInput FullyReady() => new(
        ExamRoomCount: 1,
        WarehouseCount: 1,
        DoctorCount: 1,
        ReceptionistCount: 1,
        UpcomingShiftCount: 1,
        CounterCount: 1,
        BhytEnabled: false,
        HasCskcbCode: false,
        BhytContractValid: false,
        DtqgEnabled: false,
        DtqgConnected: false,
        DtqgTokenValid: false);

    [Fact]
    public void DuDieuKien_AllPassed_PhaiLaTrue()
    {
        var dto = BranchReadinessCalculator.Build(10, FullyReady());

        dto.AllPassed.Should().BeTrue();
        dto.Items.Should().Contain(i => i.Key == "einvoice" && i.Passed); // BR: HDDT khong ap dung -> luon passed
    }

    // AC: thieu bac si -> muc "staff" fail -> all_passed = false
    [Fact]
    public void ThieuBacSi_MucStaffFail_AllPassedFalse()
    {
        var input = FullyReady() with { DoctorCount = 0 };
        var dto = BranchReadinessCalculator.Build(10, input);

        dto.AllPassed.Should().BeFalse();
        dto.Items.Single(i => i.Key == "staff").Passed.Should().BeFalse();
    }

    [Fact]
    public void ThieuLeTan_MucStaffFail()
    {
        var input = FullyReady() with { ReceptionistCount = 0 };
        var dto = BranchReadinessCalculator.Build(10, input);

        dto.Items.Single(i => i.Key == "staff").Passed.Should().BeFalse();
        dto.AllPassed.Should().BeFalse();
    }

    [Fact]
    public void KhongCoPhongKham_MucRoomExamFail()
    {
        var input = FullyReady() with { ExamRoomCount = 0 };
        var dto = BranchReadinessCalculator.Build(10, input);

        dto.Items.Single(i => i.Key == "room_exam").Passed.Should().BeFalse();
        dto.AllPassed.Should().BeFalse();
    }

    // BR-107: bhyt_enabled=true nhung chua co cskcb_code -> muc bhyt fail
    [Fact]
    public void BhytEnabled_ChuaCoCskcb_MucBhytFail()
    {
        var input = FullyReady() with { BhytEnabled = true, HasCskcbCode = false, BhytContractValid = true };
        var dto = BranchReadinessCalculator.Build(10, input);

        dto.Items.Should().Contain(i => i.Key == "bhyt");
        dto.Items.Single(i => i.Key == "bhyt").Passed.Should().BeFalse();
        dto.AllPassed.Should().BeFalse();
    }

    [Fact]
    public void BhytEnabled_DuCskcbVaHopDong_MucBhytPass()
    {
        var input = FullyReady() with { BhytEnabled = true, HasCskcbCode = true, BhytContractValid = true };
        var dto = BranchReadinessCalculator.Build(10, input);

        dto.Items.Single(i => i.Key == "bhyt").Passed.Should().BeTrue();
        dto.AllPassed.Should().BeTrue();
    }

    // BR-108/BR-107: dtqg_enabled=true nhung chua co credential/token het han -> muc dtqg fail
    [Fact]
    public void DtqgEnabled_ChuaKetNoi_MucDtqgFail()
    {
        var input = FullyReady() with { DtqgEnabled = true, DtqgConnected = false, DtqgTokenValid = false };
        var dto = BranchReadinessCalculator.Build(10, input);

        dto.Items.Single(i => i.Key == "dtqg").Passed.Should().BeFalse();
        dto.AllPassed.Should().BeFalse();
    }

    [Fact]
    public void BhytVaDtqgKhongBatBuoc_KhiKhongEnable_KhongXuatHienMuc()
    {
        var dto = BranchReadinessCalculator.Build(10, FullyReady());

        dto.Items.Should().NotContain(i => i.Key == "bhyt");
        dto.Items.Should().NotContain(i => i.Key == "dtqg");
    }
}
