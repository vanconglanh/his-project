using FluentAssertions;
using ProDiabHis.Domain.Entities;
using Xunit;

namespace ProDiabHis.UnitTests.Reception;

/// <summary>[G05] Ma tran quyen dieu phoi luot kham theo 6 trang thai ve.</summary>
public class TicketReassignPolicyTests
{
    // ── Doi bac si ──
    [Theory]
    [InlineData(TicketStatus.Waiting, true)]
    [InlineData(TicketStatus.Called, true)]
    [InlineData(TicketStatus.InProgress, false)]
    [InlineData(TicketStatus.WaitingCls, false)]
    [InlineData(TicketStatus.Done, false)]
    [InlineData(TicketStatus.Skipped, false)]
    [InlineData(TicketStatus.Cancelled, false)]
    public void CanChangeDoctor_TheoTrangThai(string status, bool expected)
        => TicketReassignPolicy.CanChangeDoctor(status).Should().Be(expected);

    // ── Doi phong ──
    [Theory]
    [InlineData(TicketStatus.Waiting, true)]
    [InlineData(TicketStatus.Called, true)]
    [InlineData(TicketStatus.InProgress, true)]
    [InlineData(TicketStatus.WaitingCls, true)]
    [InlineData(TicketStatus.Done, false)]
    [InlineData(TicketStatus.Skipped, false)]
    [InlineData(TicketStatus.Cancelled, false)]
    public void CanChangeRoom_TheoTrangThai(string status, bool expected)
        => TicketReassignPolicy.CanChangeRoom(status).Should().Be(expected);

    // ── Check(): doi CA bac si va phong ──
    [Theory]
    [InlineData(TicketStatus.Waiting, true, null)]
    [InlineData(TicketStatus.Called, true, null)]
    [InlineData(TicketStatus.InProgress, false, "TICKET_REASSIGN_DOCTOR_FORBIDDEN")]
    [InlineData(TicketStatus.WaitingCls, false, "TICKET_REASSIGN_DOCTOR_FORBIDDEN")]
    [InlineData(TicketStatus.Done, false, "TICKET_REASSIGN_FORBIDDEN")]
    [InlineData(TicketStatus.Skipped, false, "TICKET_REASSIGN_FORBIDDEN")]
    [InlineData(TicketStatus.Cancelled, false, "TICKET_REASSIGN_FORBIDDEN")]
    public void Check_DoiBacSiVaPhong(string status, bool allowed, string? errorCode)
    {
        var r = TicketReassignPolicy.Check(status, changingDoctor: true, changingRoom: true);
        r.Allowed.Should().Be(allowed);
        r.ErrorCode.Should().Be(errorCode);
    }

    // ── Check(): chi doi phong (chuyen phong giua ca) ──
    [Theory]
    [InlineData(TicketStatus.Waiting, true, null)]
    [InlineData(TicketStatus.Called, true, null)]
    [InlineData(TicketStatus.InProgress, true, null)]
    [InlineData(TicketStatus.WaitingCls, true, null)]
    [InlineData(TicketStatus.Done, false, "TICKET_REASSIGN_FORBIDDEN")]
    [InlineData(TicketStatus.Skipped, false, "TICKET_REASSIGN_FORBIDDEN")]
    [InlineData(TicketStatus.Cancelled, false, "TICKET_REASSIGN_FORBIDDEN")]
    public void Check_ChiDoiPhong(string status, bool allowed, string? errorCode)
    {
        var r = TicketReassignPolicy.Check(status, changingDoctor: false, changingRoom: true);
        r.Allowed.Should().Be(allowed);
        r.ErrorCode.Should().Be(errorCode);
    }

    // ── Check(): chi doi bac si ──
    [Theory]
    [InlineData(TicketStatus.Waiting, true, null)]
    [InlineData(TicketStatus.Called, true, null)]
    [InlineData(TicketStatus.InProgress, false, "TICKET_REASSIGN_DOCTOR_FORBIDDEN")]
    [InlineData(TicketStatus.WaitingCls, false, "TICKET_REASSIGN_DOCTOR_FORBIDDEN")]
    [InlineData(TicketStatus.Done, false, "TICKET_REASSIGN_FORBIDDEN")]
    [InlineData(TicketStatus.Skipped, false, "TICKET_REASSIGN_FORBIDDEN")]
    [InlineData(TicketStatus.Cancelled, false, "TICKET_REASSIGN_FORBIDDEN")]
    public void Check_ChiDoiBacSi(string status, bool allowed, string? errorCode)
    {
        var r = TicketReassignPolicy.Check(status, changingDoctor: true, changingRoom: false);
        r.Allowed.Should().Be(allowed);
        r.ErrorCode.Should().Be(errorCode);
    }

    [Theory]
    [InlineData(TicketStatus.Waiting)]
    [InlineData(TicketStatus.Called)]
    [InlineData(TicketStatus.InProgress)]
    [InlineData(TicketStatus.WaitingCls)]
    public void Check_KhongCoThayDoi_TraNoChange(string status)
    {
        var r = TicketReassignPolicy.Check(status, changingDoctor: false, changingRoom: false);
        r.Allowed.Should().BeFalse();
        r.ErrorCode.Should().Be("TICKET_REASSIGN_NO_CHANGE");
    }

    [Fact]
    public void Check_TrangThaiKetThuc_UuTienLoiForbidden()
    {
        // Ve da ket thuc thi bao "da ket thuc" ke ca khi body khong co thay doi nao
        var r = TicketReassignPolicy.Check(TicketStatus.Done, false, false);
        r.ErrorCode.Should().Be("TICKET_REASSIGN_FORBIDDEN");
        r.ErrorMessage.Should().Be("Lượt khám đã kết thúc — không thể điều phối");
    }

    [Theory]
    [InlineData(TicketStatus.Done, true)]
    [InlineData(TicketStatus.Skipped, true)]
    [InlineData(TicketStatus.Cancelled, true)]
    [InlineData(TicketStatus.Waiting, false)]
    [InlineData(TicketStatus.Called, false)]
    [InlineData(TicketStatus.InProgress, false)]
    [InlineData(TicketStatus.WaitingCls, false)]
    public void IsTerminal_TheoTrangThai(string status, bool expected)
        => TicketReassignPolicy.IsTerminal(status).Should().Be(expected);

    [Theory]
    [InlineData(TicketStatus.InProgress, true)]
    [InlineData(TicketStatus.WaitingCls, true)]
    [InlineData(TicketStatus.Waiting, false)]
    [InlineData(TicketStatus.Called, false)]
    public void IsInSession_TheoTrangThai(string status, bool expected)
        => TicketReassignPolicy.IsInSession(status).Should().Be(expected);

    [Theory]
    [InlineData(true, true, "BOTH")]
    [InlineData(true, false, "DOCTOR")]
    [InlineData(false, true, "ROOM")]
    public void ChangeType_From(bool doctor, bool room, string expected)
        => ReassignChangeType.From(doctor, room).Should().Be(expected);
}
