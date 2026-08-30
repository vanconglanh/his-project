using FluentAssertions;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.Dashboard;
using ProDiabHis.Infrastructure.Auth;
using Xunit;

namespace ProDiabHis.UnitTests.Sprint20260830;

/// <summary>
/// Dot 4 da chi nhanh — US-6.1/BR-33/BR-93: pham vi S1/S2/S3 cua dashboard chuoi (bang xep hang
/// chi nhanh + drill-down bac si) phai tinh dung tu IBranchProvider, khong duoc "cap S3 roi loc UI".
/// </summary>
public class BranchScopeResolverTests
{
    // S1: user chi duoc gan dung 1 chi nhanh -> AC-3.2.1/AC-6.1.3 - khong duoc thay du lieu chi nhanh khac.
    [Fact]
    public void S1_ChiDuocGan1ChiNhanh_ChiThayDungChiNhanhDo()
    {
        var bp = new BranchProvider();
        bp.SetContext(branchId: 5, ignoreFilter: false, allowedBranchIds: new List<int> { 5 });

        var allowed = BranchScopeResolver.ResolveAllowedBranchIds(bp);

        allowed.Should().NotBeNull();
        allowed!.Should().ContainSingle().Which.Should().Be(5);
        BranchScopeResolver.IsBranchAllowed(bp, 5).Should().BeTrue();
        BranchScopeResolver.IsBranchAllowed(bp, 6).Should().BeFalse(); // AC-3.2.1: branch khac -> tu choi
    }

    // S2: branch.group_view - duoc gan nhieu chi nhanh cung group (AC-3.2.1: 2 CN trong nhom).
    [Fact]
    public void S2_DuocGanNhieuChiNhanh_ChiThayCacChiNhanhTrongScope()
    {
        var bp = new BranchProvider();
        bp.SetContext(branchId: 1, ignoreFilter: false, allowedBranchIds: new List<int> { 1, 2 });

        var allowed = BranchScopeResolver.ResolveAllowedBranchIds(bp);

        allowed.Should().BeEquivalentTo(new[] { 1, 2 });
        BranchScopeResolver.IsBranchAllowed(bp, 2).Should().BeTrue();
        BranchScopeResolver.IsBranchAllowed(bp, 99).Should().BeFalse();
    }

    // S3: branch.cross_view/super_admin -> IgnoreBranchFilter=true -> khong gioi han (null = tat ca).
    [Fact]
    public void S3_IgnoreBranchFilter_KhongGioiHan()
    {
        var bp = new BranchProvider();
        bp.SetContext(branchId: 1, ignoreFilter: true, allowedBranchIds: new List<int>());

        var allowed = BranchScopeResolver.ResolveAllowedBranchIds(bp);

        allowed.Should().BeNull();
        BranchScopeResolver.IsBranchAllowed(bp, 999).Should().BeTrue();
    }
}
