using System;
using System.Collections.Generic;
using ProDiabHis.Application.Encounters;
using Xunit;

namespace ProDiabHis.UnitTests.Encounters;

/// <summary>
/// BM-61: vai tro thieu quyen lam sang (vd Le tan: encounter.read nhung khong co emr.read/
/// vital_sign.read) KHONG duoc nhan chan doan / sinh hieu / EMR trong chi tiet luot kham.
/// EncounterClinicalGate strip du lieu lam sang khi khong co quyen.
/// </summary>
public class EncounterClinicalGateTests
{
    private static readonly IReadOnlyList<DiagnosisResponse> SampleDiags = new List<DiagnosisResponse>
    {
        new(Guid.NewGuid(), "E11", "Bệnh đái tháo đường type 2", "PRIMARY", null, DateTime.UtcNow),
        new(Guid.NewGuid(), "E66", "Bệnh thừa cân béo phì", "SECONDARY", null, DateTime.UtcNow),
    };

    [Fact]
    public void GateList_Returns_Original_When_Has_Permission()
    {
        var result = EncounterClinicalGate.GateList(true, SampleDiags);
        Assert.Equal(2, result.Count);
        Assert.Same(SampleDiags, result);
    }

    [Fact]
    public void GateList_Returns_Empty_When_No_Permission()
    {
        // BM-61 core: Le tan thieu emr.read -> chan doan bi bo (danh sach rong).
        var result = EncounterClinicalGate.GateList(false, SampleDiags);
        Assert.Empty(result);
    }

    [Fact]
    public void GateRef_Returns_Original_When_Has_Permission()
    {
        var emr = new EmrSummaryDto(Guid.NewGuid(), DateTime.UtcNow, 1);
        var result = EncounterClinicalGate.GateRef(true, emr);
        Assert.Same(emr, result);
    }

    [Fact]
    public void GateRef_Returns_Null_When_No_Permission()
    {
        // BM-61: thieu vital_sign.read -> sinh hieu bi bo (null); thieu emr.read -> EMR bi bo (null).
        var emr = new EmrSummaryDto(Guid.NewGuid(), DateTime.UtcNow, 1);
        var result = EncounterClinicalGate.GateRef(false, emr);
        Assert.Null(result);
    }

    [Theory]
    [InlineData(true, true, 2)]   // bac si: thay chan doan
    [InlineData(false, true, 0)]  // thieu emr.read: khong chan doan
    [InlineData(false, false, 0)] // Le tan: khong chan doan
    public void Diagnoses_Visibility_Depends_On_Emr_Read(bool canReadEmr, bool canReadVital, int expectedCount)
    {
        _ = canReadVital; // chan doan gate theo emr.read
        var diags = EncounterClinicalGate.GateList(canReadEmr, SampleDiags);
        Assert.Equal(expectedCount, diags.Count);
    }
}
