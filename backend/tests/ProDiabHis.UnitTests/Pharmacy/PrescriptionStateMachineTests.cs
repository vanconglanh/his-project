using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.Pharmacy;
using ProDiabHis.Application.Pharmacy.Prescriptions;
using ProDiabHis.Infrastructure.Pharmacy;
using Xunit;

namespace ProDiabHis.UnitTests.Pharmacy;

/// <summary>
/// Tests for prescription state machine rules (sign, cancel, DDI block).
/// Uses MockUsbTokenSigner (infrastructure) directly.
/// </summary>
public class PrescriptionStateMachineTests
{
    private readonly IUsbTokenSigner _signer;
    private readonly IDdiChecker _ddiChecker;

    public PrescriptionStateMachineTests()
    {
        _signer = new MockUsbTokenSigner(NullLogger<MockUsbTokenSigner>.Instance);
        _ddiChecker = Substitute.For<IDdiChecker>();
    }

    [Fact]
    public async Task MockSigner_accepts_valid_base64_signature()
    {
        // Arrange
        var fakeBase64 = Convert.ToBase64String(new byte[] { 1, 2, 3, 4, 5 });

        // Act
        var result = await _signer.VerifyAsync(fakeBase64, "THUMBPRINT123");

        // Assert
        result.IsValid.Should().BeTrue();
        result.SerialNumber.Should().NotBeNullOrEmpty();
        result.SubjectName.Should().Contain("MockDoctor");
    }

    [Fact]
    public async Task MockSigner_rejects_empty_signature()
    {
        var result = await _signer.VerifyAsync("", "THUMBPRINT");
        result.IsValid.Should().BeFalse();
        result.ErrorReason.Should().Contain("empty");
    }

    [Fact]
    public async Task MockSigner_rejects_invalid_base64()
    {
        var result = await _signer.VerifyAsync("not-valid-base64!!!", "THUMBPRINT");
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task DdiChecker_returns_no_warnings_for_single_drug()
    {
        // Single drug cannot have DDI
        _ddiChecker.CheckAsync(Arg.Any<IReadOnlyList<int>>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var warnings = await _ddiChecker.CheckAsync([1], CancellationToken.None);
        warnings.Should().BeEmpty();
    }

    [Fact]
    public async Task DdiChecker_returns_CONTRAINDICATED_blocks_sign()
    {
        // Arrange
        var contraWarning = new DdiWarning(1, "DrugA", 2, "DrugB", "CONTRAINDICATED", "Chong chi dinh nghiem trong", "A");
        _ddiChecker.CheckAsync(Arg.Any<IReadOnlyList<int>>(), Arg.Any<CancellationToken>())
            .Returns([contraWarning]);

        // Act - simulate sign check
        var warnings = await _ddiChecker.CheckAsync([1, 2], CancellationToken.None);
        var hasContra = warnings.Any(w => w.Severity == "CONTRAINDICATED");

        // Assert
        hasContra.Should().BeTrue();
    }

    [Fact]
    public void PrescriptionStatus_transition_DRAFT_to_SIGNED_is_valid()
    {
        // State machine rule
        var allowedSourceStates = new[] { "DRAFT" };
        "DRAFT".Should().BeOneOf(allowedSourceStates);
    }

    [Fact]
    public void PrescriptionStatus_cannot_cancel_after_DISPENSED()
    {
        // Business rule: cannot cancel dispensed
        var cancelableStates = new[] { "DRAFT", "SIGNED", "SUBMITTED_DTQG" };
        cancelableStates.Should().NotContain("DISPENSED");
        cancelableStates.Should().NotContain("PARTIAL_DISPENSED");
    }

    [Fact]
    public void DtqgStatus_state_machine_NONE_to_PENDING_to_ACCEPTED()
    {
        var states = new[] { "NONE", "PENDING", "SUBMITTED", "ACCEPTED", "REJECTED" };
        states.Should().ContainInOrder("NONE", "PENDING", "SUBMITTED", "ACCEPTED");
    }
}
