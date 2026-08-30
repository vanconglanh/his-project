using FluentAssertions;
using ProDiabHis.Application.Patients;
using ProDiabHis.Domain.Entities;
using Xunit;

namespace ProDiabHis.UnitTests.Patients;

/// <summary>
/// Unit test cho logic phan biet 3 case check trung CCCD (BR-DUP-002..005),
/// test truc tiep CheckCccdDuplicateQueryHandler.BuildDiffs de khong phu thuoc DbContext.
/// </summary>
public class CccdDuplicateCheckTests
{
    private static Patient MakeExistingPatient() => new()
    {
        Id = Guid.NewGuid(),
        TenantId = 1,
        Code = "BNT01000001",
        FullName = "Nguyễn Văn A",
        Gender = Gender.Male,
        DateOfBirth = new DateOnly(1999, 3, 15),
        Street = "Số 1 Đường ABC, Quận Y",
        Status = PatientStatus.Active,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    [Fact]
    public void BuildDiffs_AllFieldsMatch_ReturnsNoDiffs_Case2ExactMatch()
    {
        var patient = MakeExistingPatient();
        var query = new CheckCccdDuplicateQuery(
            IdNumber: "001099012345",
            FullName: "Nguyễn Văn A",
            DateOfBirth: new DateOnly(1999, 3, 15),
            Gender: Gender.Male,
            Address: "Số 1 Đường ABC, Quận Y");

        var diffs = CheckCccdDuplicateQueryHandler.BuildDiffs(patient, query);

        diffs.Should().BeEmpty("dữ liệu quét khớp hoàn toàn với hồ sơ hiện có -> Case 2");
    }

    [Fact]
    public void BuildDiffs_NameCasingAndWhitespaceDiffer_StillConsideredMatch()
    {
        // BR-DUP-005: normalize truoc khi so sanh (trim + lowercase)
        var patient = MakeExistingPatient();
        var query = new CheckCccdDuplicateQuery(
            IdNumber: "001099012345",
            FullName: "  nguyễn văn a  ",
            DateOfBirth: new DateOnly(1999, 3, 15),
            Gender: Gender.Male,
            Address: "số 1 đường abc, quận y");

        var diffs = CheckCccdDuplicateQueryHandler.BuildDiffs(patient, query);

        diffs.Should().BeEmpty();
    }

    [Fact]
    public void BuildDiffs_AddressDiffers_ReturnsMismatchOnAddressOnly_Case3()
    {
        var patient = MakeExistingPatient();
        var query = new CheckCccdDuplicateQuery(
            IdNumber: "001099012345",
            FullName: "Nguyễn Văn A",
            DateOfBirth: new DateOnly(1999, 3, 15),
            Gender: Gender.Male,
            Address: "Số 1 Đường ABC, Phường X, Quận Y, TP Z");

        var diffs = CheckCccdDuplicateQueryHandler.BuildDiffs(patient, query);

        diffs.Should().ContainSingle(d => d.Field == CccdComparableField.Address);
        diffs.Single().OldValue.Should().Be("Số 1 Đường ABC, Quận Y");
        diffs.Single().NewValue.Should().Be("Số 1 Đường ABC, Phường X, Quận Y, TP Z");
    }

    [Fact]
    public void BuildDiffs_NameAndDobDiffer_ReturnsBothMismatches()
    {
        var patient = MakeExistingPatient();
        var query = new CheckCccdDuplicateQuery(
            IdNumber: "001099012345",
            FullName: "Nguyễn Văn B",
            DateOfBirth: new DateOnly(1998, 1, 1),
            Gender: Gender.Male,
            Address: "Số 1 Đường ABC, Quận Y");

        var diffs = CheckCccdDuplicateQueryHandler.BuildDiffs(patient, query);

        diffs.Should().HaveCount(2);
        diffs.Should().Contain(d => d.Field == CccdComparableField.FullName);
        diffs.Should().Contain(d => d.Field == CccdComparableField.DateOfBirth);
    }
}
