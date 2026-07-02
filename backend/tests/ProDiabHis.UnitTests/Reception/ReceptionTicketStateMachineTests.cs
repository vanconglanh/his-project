using FluentAssertions;
using ProDiabHis.Domain.Entities;
using Xunit;

namespace ProDiabHis.UnitTests.Reception;

/// <summary>Unit test state machine chuyen trang thai ticket tiep don</summary>
public class ReceptionTicketStateMachineTests
{
    [Theory]
    [InlineData(TicketStatus.Waiting, TicketStatus.Called, true)]
    [InlineData(TicketStatus.Waiting, TicketStatus.Skipped, true)]
    [InlineData(TicketStatus.Waiting, TicketStatus.Cancelled, true)]
    [InlineData(TicketStatus.Called, TicketStatus.InProgress, true)]
    [InlineData(TicketStatus.Called, TicketStatus.Cancelled, true)]
    [InlineData(TicketStatus.Called, TicketStatus.Skipped, true)]
    [InlineData(TicketStatus.InProgress, TicketStatus.Done, true)]
    [InlineData(TicketStatus.InProgress, TicketStatus.Cancelled, true)]
    public void CanTransition_ValidTransitions_ReturnsTrue(string from, string to, bool expected)
    {
        TicketStatus.CanTransition(from, to).Should().Be(expected);
    }

    [Theory]
    [InlineData(TicketStatus.Done, TicketStatus.Called)]
    [InlineData(TicketStatus.Done, TicketStatus.Waiting)]
    [InlineData(TicketStatus.Cancelled, TicketStatus.Called)]
    [InlineData(TicketStatus.Skipped, TicketStatus.Done)]
    [InlineData(TicketStatus.Waiting, TicketStatus.Done)]
    [InlineData(TicketStatus.Waiting, TicketStatus.InProgress)]
    [InlineData(TicketStatus.InProgress, TicketStatus.Called)]
    public void CanTransition_InvalidTransitions_ReturnsFalse(string from, string to)
    {
        TicketStatus.CanTransition(from, to).Should().BeFalse();
    }

    [Fact]
    public void TicketStatus_Constants_AreCorrectStrings()
    {
        TicketStatus.Waiting.Should().Be("WAITING");
        TicketStatus.Called.Should().Be("CALLED");
        TicketStatus.InProgress.Should().Be("IN_PROGRESS");
        TicketStatus.Done.Should().Be("DONE");
        TicketStatus.Skipped.Should().Be("SKIPPED");
        TicketStatus.Cancelled.Should().Be("CANCELLED");
    }

    [Fact]
    public void TicketPriority_Constants_AreCorrectStrings()
    {
        TicketPriority.Normal.Should().Be("NORMAL");
        TicketPriority.Priority.Should().Be("PRIORITY");
        TicketPriority.Emergency.Should().Be("EMERGENCY");
    }
}
