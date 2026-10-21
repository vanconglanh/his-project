using FluentAssertions;
using ProDiabHis.Application.Patients;
using ProDiabHis.Domain.Entities;
using Xunit;

namespace ProDiabHis.UnitTests.Patients;

/// <summary>BM-94 / BM-91: entity -> response map ExamServiceId, CustomerSource.</summary>
public class PatientExamServiceMappingTests
{
    [Fact]
    public void ToResponse_Maps_ExamService_And_CustomerSource()
    {
        var svcId = Guid.NewGuid();
        var p = new Patient
        {
            FullName = "Nguyen Van A",
            ExamServiceId = svcId,
            CustomerSource = "B2B",
            CustomerSourceDetail = "Cong ty ABC",
            VisitType = "RE_EXAM_IN_PACKAGE"
        };

        var r = PatientEntityMapper.ToResponse(p);

        r.ExamServiceId.Should().Be(svcId);
        r.CustomerSource.Should().Be("B2B");
        r.CustomerSourceDetail.Should().Be("Cong ty ABC");
        r.VisitType.Should().Be("RE_EXAM_IN_PACKAGE");
    }

    [Fact]
    public void ToResponse_Defaults_Null_When_Not_Set()
    {
        var r = PatientEntityMapper.ToResponse(new Patient { FullName = "B" });
        r.ExamServiceId.Should().BeNull();
        r.CustomerSource.Should().BeNull();
    }
}
