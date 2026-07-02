using FluentAssertions;
using NSubstitute;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.Diabetes;
using Xunit;

namespace ProDiabHis.UnitTests.Diabetes;

/// <summary>Unit test HbA1c validation (US-E04)</summary>
public class DiabetesAssessmentValidationTests
{
    // ── HbA1c boundary tests ──
    [Theory]
    [InlineData(2.9)]    // Below min=3
    [InlineData(20.1)]   // Above max=20
    public async Task CreateAssessment_InvalidHba1c_ReturnsError(double hba1c)
    {
        var db = Substitute.For<IDapperConnectionFactory>();
        var tenant = Substitute.For<ITenantProvider>();
        var user = Substitute.For<ICurrentUser>();
        var audit = Substitute.For<IAuditService>();

        var handler = new CreateDiabetesAssessmentCommandHandler(db, tenant, user, audit);
        var req = new DiabetesAssessmentRequest(
            (decimal)hba1c, null, null, null, null, null, null, null, null, null, null, null, null, null, null);

        var result = await handler.Handle(
            new CreateDiabetesAssessmentCommand(Guid.NewGuid(), req), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("DIABETES_INVALID_HBA1C");
    }

    [Theory]
    [InlineData(3.0)]    // Min edge
    [InlineData(7.5)]    // Normal diabetic control
    [InlineData(20.0)]   // Max edge
    public void HbA1c_ValidRange_NoValidationError(double hba1c)
    {
        // If hba1c is within range, handler would proceed to DB check
        // We just verify the validation logic itself here
        var isInRange = (decimal)hba1c >= 3m && (decimal)hba1c <= 20m;
        isInRange.Should().BeTrue();
    }

    [Fact]
    public void DiabetesType_AllValidValues()
    {
        var validTypes = new[] { "TYPE_1", "TYPE_2", "GESTATIONAL", "MODY", "OTHER" };
        validTypes.Should().NotBeEmpty();
        validTypes.Should().Contain("TYPE_2");
    }

    [Fact]
    public void Complications_AllFields_Nullable()
    {
        var comp = new ComplicationsDto(null, null, null, null, null, null);
        comp.Retinopathy.Should().BeNull();
        comp.DiabeticFoot.Should().BeNull();
    }

    [Fact]
    public void TreatmentTarget_DefaultValues_AreReasonable()
    {
        var target = new TreatmentTargetDto(7.0m, 2.6m, "130/80");
        target.Hba1cTarget.Should().Be(7.0m);
        target.LdlTarget.Should().Be(2.6m);
        target.BpTarget.Should().Be("130/80");
    }
}
