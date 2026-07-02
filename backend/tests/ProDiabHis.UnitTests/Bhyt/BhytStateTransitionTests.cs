using FluentAssertions;
using ProDiabHis.Domain.Entities.Bhyt;
using Xunit;

namespace ProDiabHis.UnitTests.Bhyt;

public class BhytStateTransitionTests
{
    [Fact]
    public void Draft_is_not_locked()
    {
        BhytExportStatus.IsLocked(BhytExportStatus.Draft).Should().BeFalse();
    }

    [Fact]
    public void Generated_is_not_locked()
    {
        BhytExportStatus.IsLocked(BhytExportStatus.Generated).Should().BeFalse();
    }

    [Fact]
    public void Validated_is_not_locked()
    {
        BhytExportStatus.IsLocked(BhytExportStatus.Validated).Should().BeFalse();
    }

    [Fact]
    public void Signed_is_not_locked()
    {
        BhytExportStatus.IsLocked(BhytExportStatus.Signed).Should().BeFalse();
    }

    [Theory]
    [InlineData(BhytExportStatus.Submitted)]
    [InlineData(BhytExportStatus.Approved)]
    [InlineData(BhytExportStatus.PartiallyRejected)]
    [InlineData(BhytExportStatus.Rejected)]
    public void Post_submit_statuses_are_locked(string status)
    {
        BhytExportStatus.IsLocked(status).Should().BeTrue();
    }

    [Fact]
    public void Locked_statuses_set_contains_exactly_four_statuses()
    {
        BhytExportStatus.LockedStatuses.Should().HaveCount(4);
        BhytExportStatus.LockedStatuses.Should().Contain(new[]
        {
            BhytExportStatus.Submitted,
            BhytExportStatus.Approved,
            BhytExportStatus.PartiallyRejected,
            BhytExportStatus.Rejected
        });
    }
}
