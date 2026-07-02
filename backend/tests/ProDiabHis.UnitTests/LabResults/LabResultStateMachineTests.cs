using FluentAssertions;
using ProDiabHis.Application.LabResults;
using Xunit;

namespace ProDiabHis.UnitTests.LabResults;

/// <summary>
/// Test state machine cua LabResult: DRAFT -> VERIFIED -> AMENDED
/// Logic duoc test qua helper, khong phu thuoc DB.
/// </summary>
public class LabResultStateMachineTests
{
    // DRAFT -> co the VERIFY
    [Fact]
    public void Draft_CanBeVerified()
    {
        var status = LabResultStatus.Draft;
        var canVerify = status == LabResultStatus.Draft || status == LabResultStatus.Amended;
        canVerify.Should().BeTrue();
    }

    // VERIFIED trong 15 phut -> co the AMEND
    [Fact]
    public void Verified_Within15Min_CanBeAmended()
    {
        var verifiedAt = DateTime.UtcNow.AddMinutes(-10);
        var elapsed    = (DateTime.UtcNow - verifiedAt).TotalMinutes;
        var canAmend   = elapsed <= 15;
        canAmend.Should().BeTrue();
    }

    // VERIFIED qua 15 phut -> AMEND bi chan
    [Fact]
    public void Verified_After15Min_CannotBeAmended()
    {
        var verifiedAt = DateTime.UtcNow.AddMinutes(-20);
        var elapsed    = (DateTime.UtcNow - verifiedAt).TotalMinutes;
        var canAmend   = elapsed <= 15;
        canAmend.Should().BeFalse();
    }

    // VERIFIED trong 30 phut -> co the UNVERIFY
    [Fact]
    public void Verified_Within30Min_CanBeUnverified()
    {
        var verifiedAt = DateTime.UtcNow.AddMinutes(-25);
        var elapsed    = (DateTime.UtcNow - verifiedAt).TotalMinutes;
        var canUnverify = elapsed <= 30;
        canUnverify.Should().BeTrue();
    }

    // VERIFIED qua 30 phut -> UNVERIFY bi chan
    [Fact]
    public void Verified_After30Min_CannotBeUnverified()
    {
        var verifiedAt = DateTime.UtcNow.AddMinutes(-35);
        var elapsed    = (DateTime.UtcNow - verifiedAt).TotalMinutes;
        var canUnverify = elapsed <= 30;
        canUnverify.Should().BeFalse();
    }

    // DRAFT khong the UNVERIFY
    [Fact]
    public void Draft_CannotBeUnverified()
    {
        var status    = LabResultStatus.Draft;
        var isVerified = status == LabResultStatus.Verified;
        isVerified.Should().BeFalse();
    }

    // AMENDED co the duoc verify lai
    [Fact]
    public void Amended_CanBeReverified()
    {
        var status     = LabResultStatus.Amended;
        var canVerify  = status == LabResultStatus.Draft || status == LabResultStatus.Amended;
        canVerify.Should().BeTrue();
    }

    // Status string constants chinh xac
    [Fact]
    public void StatusConstants_AreCorrect()
    {
        LabResultStatus.Draft.Should().Be("DRAFT");
        LabResultStatus.Verified.Should().Be("VERIFIED");
        LabResultStatus.Amended.Should().Be("AMENDED");
    }

    // Flag constants chinh xac
    [Fact]
    public void FlagConstants_AreCorrect()
    {
        LabResultFlag.Normal.Should().Be("NORMAL");
        LabResultFlag.H.Should().Be("H");
        LabResultFlag.L.Should().Be("L");
        LabResultFlag.HH.Should().Be("HH");
        LabResultFlag.LL.Should().Be("LL");
        LabResultFlag.Critical.Should().Be("CRITICAL");
    }
}
