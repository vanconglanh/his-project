using FluentAssertions;
using ProDiabHis.Domain.Entities;
using Xunit;

namespace ProDiabHis.UnitTests.CLS;

/// <summary>Unit tests state machine Lab/Rad order (US-E08)</summary>
public class ClsOrderStateMachineTests
{
    // ── Lab Order state machine ──
    [Theory]
    [InlineData("ordered",      "sample_taken", true)]
    [InlineData("ordered",      "cancelled",    true)]
    [InlineData("sample_taken", "processing",   true)]
    [InlineData("processing",   "done",         true)]
    public void LabOrder_ValidTransitions_ReturnsTrue(string from, string to, bool expected)
    {
        LabOrderStatus.CanTransition(from, to).Should().Be(expected);
    }

    [Theory]
    [InlineData("ordered",      "processing")]   // Skip sample_taken
    [InlineData("ordered",      "done")]          // Skip multiple steps
    [InlineData("done",         "ordered")]       // Reverse
    [InlineData("cancelled",    "ordered")]       // From terminal
    [InlineData("processing",   "sample_taken")]  // Backward
    public void LabOrder_InvalidTransitions_ReturnsFalse(string from, string to)
    {
        LabOrderStatus.CanTransition(from, to).Should().BeFalse();
    }

    [Fact]
    public void LabOrderStatus_Constants_AreCorrect()
    {
        LabOrderStatus.Ordered.Should().Be("ordered");
        LabOrderStatus.SampleTaken.Should().Be("sample_taken");
        LabOrderStatus.Processing.Should().Be("processing");
        LabOrderStatus.Done.Should().Be("done");
        LabOrderStatus.Cancelled.Should().Be("cancelled");
    }

    // ── Rad Order state machine ──
    [Theory]
    [InlineData("ordered",     "scheduled",   true)]
    [InlineData("ordered",     "cancelled",   true)]
    [InlineData("scheduled",   "in_progress", true)]
    [InlineData("in_progress", "done",        true)]
    [InlineData("in_progress", "cancelled",   true)]
    public void RadOrder_ValidTransitions_ReturnsTrue(string from, string to, bool expected)
    {
        RadOrderStatus.CanTransition(from, to).Should().Be(expected);
    }

    [Theory]
    [InlineData("ordered",     "in_progress")]  // Skip scheduled
    [InlineData("ordered",     "done")]          // Skip multiple
    [InlineData("done",        "ordered")]       // Reverse
    [InlineData("cancelled",   "scheduled")]     // From terminal
    public void RadOrder_InvalidTransitions_ReturnsFalse(string from, string to)
    {
        RadOrderStatus.CanTransition(from, to).Should().BeFalse();
    }

    [Fact]
    public void RadOrderStatus_Constants_AreCorrect()
    {
        RadOrderStatus.Ordered.Should().Be("ordered");
        RadOrderStatus.Scheduled.Should().Be("scheduled");
        RadOrderStatus.InProgress.Should().Be("in_progress");
        RadOrderStatus.Done.Should().Be("done");
        RadOrderStatus.Cancelled.Should().Be("cancelled");
    }

    [Fact]
    public void ClsPriority_Constants_AreCorrect()
    {
        ClsPriority.Normal.Should().Be("NORMAL");
        ClsPriority.Urgent.Should().Be("URGENT");
        ClsPriority.Stat.Should().Be("STAT");
    }

    // ── Delete only from ordered ──
    [Theory]
    [InlineData("ordered",     true)]
    [InlineData("sample_taken",false)]
    [InlineData("processing",  false)]
    [InlineData("done",        false)]
    [InlineData("cancelled",   false)]
    public void LabOrder_DeleteAllowed_OnlyWhenOrdered(string status, bool canDelete)
    {
        (status == LabOrderStatus.Ordered).Should().Be(canDelete);
    }
}
