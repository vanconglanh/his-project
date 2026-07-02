using FluentAssertions;
using ProDiabHis.Domain.Entities;
using Xunit;

namespace ProDiabHis.UnitTests.Encounters;

/// <summary>Unit test state machine chuyen trang thai encounter (US-E05, US-E10)</summary>
public class EncounterStateMachineTests
{
    // ── Happy path transitions ──
    [Theory]
    [InlineData(EncounterStatus.Waiting,    EncounterStatus.InProgress, true)]
    [InlineData(EncounterStatus.Waiting,    EncounterStatus.Cancelled,  true)]
    [InlineData(EncounterStatus.InProgress, EncounterStatus.Done,       true)]
    [InlineData(EncounterStatus.InProgress, EncounterStatus.Cancelled,  true)]
    public void CanTransition_ValidTransitions_ReturnsTrue(string from, string to, bool expected)
    {
        EncounterStatus.CanTransition(from, to).Should().Be(expected);
    }

    // ── Invalid transitions ──
    [Theory]
    [InlineData(EncounterStatus.Waiting,    EncounterStatus.Done)]
    [InlineData(EncounterStatus.Done,       EncounterStatus.InProgress)]
    [InlineData(EncounterStatus.Done,       EncounterStatus.Waiting)]
    [InlineData(EncounterStatus.Cancelled,  EncounterStatus.InProgress)]
    [InlineData(EncounterStatus.InProgress, EncounterStatus.Waiting)]
    public void CanTransition_InvalidTransitions_ReturnsFalse(string from, string to)
    {
        EncounterStatus.CanTransition(from, to).Should().BeFalse();
    }

    [Fact]
    public void EncounterStatus_Constants_AreCorrectStrings()
    {
        EncounterStatus.Waiting.Should().Be("WAITING");
        EncounterStatus.InProgress.Should().Be("IN_PROGRESS");
        EncounterStatus.Done.Should().Be("DONE");
        EncounterStatus.Cancelled.Should().Be("CANCELLED");
    }

    // ── Edge: terminal states cannot transition to anything ──
    [Theory]
    [InlineData(EncounterStatus.Done,      EncounterStatus.Waiting)]
    [InlineData(EncounterStatus.Done,      EncounterStatus.InProgress)]
    [InlineData(EncounterStatus.Done,      EncounterStatus.Cancelled)]
    [InlineData(EncounterStatus.Cancelled, EncounterStatus.Done)]
    [InlineData(EncounterStatus.Cancelled, EncounterStatus.Waiting)]
    public void CanTransition_TerminalStates_AlwaysFalse(string from, string to)
    {
        EncounterStatus.CanTransition(from, to).Should().BeFalse();
    }
}
