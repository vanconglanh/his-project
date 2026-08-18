using FluentAssertions;
using ProDiabHis.Domain.Entities;
using Xunit;

namespace ProDiabHis.UnitTests.Reception;

/// <summary>
/// Unit tests state machine ve hang doi - trong tam trang thai WAITING_CLS (G01/G02).
/// </summary>
public class TicketStateMachineTests
{
    [Fact]
    public void WaitingCls_Constant_IsCorrect()
    {
        TicketStatus.WaitingCls.Should().Be("WAITING_CLS");
    }

    // ── Transition hai chieu IN_PROGRESS <-> WAITING_CLS ──
    [Fact]
    public void InProgress_To_WaitingCls_IsAllowed()
    {
        TicketStatus.CanTransition(TicketStatus.InProgress, TicketStatus.WaitingCls).Should().BeTrue();
    }

    [Fact]
    public void WaitingCls_To_InProgress_IsAllowed()
    {
        TicketStatus.CanTransition(TicketStatus.WaitingCls, TicketStatus.InProgress).Should().BeTrue();
    }

    // ── WAITING_CLS -> ket thuc ──
    [Theory]
    [InlineData("DONE")]
    [InlineData("SKIPPED")]
    [InlineData("CANCELLED")]
    public void WaitingCls_To_TerminalStates_IsAllowed(string to)
    {
        TicketStatus.CanTransition(TicketStatus.WaitingCls, to).Should().BeTrue();
    }

    // ── Cac transition KHONG hop le vao/ra WAITING_CLS ──
    [Theory]
    [InlineData("WAITING",     "WAITING_CLS")]  // chua vao kham
    [InlineData("CALLED",      "WAITING_CLS")]  // moi goi, chua bat dau kham
    [InlineData("DONE",        "WAITING_CLS")]  // da ket thuc
    [InlineData("CANCELLED",   "WAITING_CLS")]  // terminal
    [InlineData("SKIPPED",     "WAITING_CLS")]  // terminal
    [InlineData("WAITING_CLS", "WAITING")]      // khong quay ve hang doi dau
    [InlineData("WAITING_CLS", "CALLED")]       // khong quay ve trang thai goi
    [InlineData("WAITING_CLS", "WAITING_CLS")]  // self-transition
    public void WaitingCls_InvalidTransitions_ReturnFalse(string from, string to)
    {
        TicketStatus.CanTransition(from, to).Should().BeFalse();
    }

    // ── Khong pha vo state machine cu ──
    [Theory]
    [InlineData("WAITING",     "CALLED",      true)]
    [InlineData("WAITING",     "SKIPPED",     true)]
    [InlineData("WAITING",     "CANCELLED",   true)]
    [InlineData("CALLED",      "IN_PROGRESS", true)]
    [InlineData("CALLED",      "SKIPPED",     true)]
    [InlineData("CALLED",      "CANCELLED",   true)]
    [InlineData("IN_PROGRESS", "DONE",        true)]
    [InlineData("IN_PROGRESS", "CANCELLED",   true)]
    [InlineData("WAITING",     "IN_PROGRESS", false)]
    [InlineData("DONE",        "IN_PROGRESS", false)]
    public void LegacyTransitions_Unchanged(string from, string to, bool expected)
    {
        TicketStatus.CanTransition(from, to).Should().Be(expected);
    }
}

/// <summary>Unit tests state machine dot chi dinh CLS (G01/G02)</summary>
public class ClsRoundStateMachineTests
{
    [Theory]
    [InlineData("OPEN",        "SUBMITTED")]
    [InlineData("OPEN",        "CANCELLED")]
    [InlineData("SUBMITTED",   "IN_PROGRESS")]
    [InlineData("SUBMITTED",   "CANCELLED")]
    [InlineData("IN_PROGRESS", "COMPLETED")]
    [InlineData("IN_PROGRESS", "CANCELLED")]
    public void Status_ValidTransitions_ReturnTrue(string from, string to)
    {
        ClsRoundStatus.CanTransition(from, to).Should().BeTrue();
    }

    [Theory]
    [InlineData("OPEN",      "COMPLETED")]
    [InlineData("OPEN",      "IN_PROGRESS")]
    [InlineData("COMPLETED", "OPEN")]
    [InlineData("CANCELLED", "OPEN")]
    [InlineData("SUBMITTED", "OPEN")]
    public void Status_InvalidTransitions_ReturnFalse(string from, string to)
    {
        ClsRoundStatus.CanTransition(from, to).Should().BeFalse();
    }

    [Theory]
    [InlineData("UNPAID", "PAID",   true)]
    [InlineData("UNPAID", "WAIVED", true)]
    [InlineData("WAIVED", "PAID",   true)]
    [InlineData("PAID",   "UNPAID", false)]
    [InlineData("PAID",   "WAIVED", false)]
    [InlineData("WAIVED", "UNPAID", false)]
    public void PaymentStatus_Transitions(string from, string to, bool expected)
    {
        ClsRoundPaymentStatus.CanTransition(from, to).Should().Be(expected);
    }

    [Theory]
    [InlineData("PAID",   true)]
    [InlineData("WAIVED", true)]
    [InlineData("UNPAID", false)]
    public void AllowsExecution_OnlyPaidOrWaived(string paymentStatus, bool expected)
    {
        ClsRoundPaymentStatus.AllowsExecution(paymentStatus).Should().Be(expected);
    }
}
