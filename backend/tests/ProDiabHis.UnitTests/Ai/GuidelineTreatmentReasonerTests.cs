using ProDiabHis.Application.Ai;
using Xunit;

namespace ProDiabHis.UnitTests.Ai;

public class GuidelineTreatmentReasonerTests
{
    private static readonly CarePathwayTargetSnapshot DefaultTargets = new(7.0m, 130, 80, 30m);

    [Fact]
    public void Reason_Hba1cAboveTarget_ReturnsRecommendation()
    {
        var state = new PatientClinicalState(8.2m, 60m, 120, 78, false);

        var recs = GuidelineTreatmentReasoner.Reason(state, DefaultTargets);

        Assert.Contains(recs, r => r.Code == "HBA1C_ABOVE_TARGET");
    }

    [Fact]
    public void Reason_LowEgfrOnMetformin_ReturnsRecommendation()
    {
        var state = new PatientClinicalState(6.5m, 25m, 120, 78, true);

        var recs = GuidelineTreatmentReasoner.Reason(state, DefaultTargets);

        Assert.Contains(recs, r => r.Code == "EGFR_LOW_ON_METFORMIN");
    }

    [Fact]
    public void Reason_LowEgfrNotOnMetformin_NoRecommendation()
    {
        var state = new PatientClinicalState(6.5m, 25m, 120, 78, false);

        var recs = GuidelineTreatmentReasoner.Reason(state, DefaultTargets);

        Assert.DoesNotContain(recs, r => r.Code == "EGFR_LOW_ON_METFORMIN");
    }

    [Fact]
    public void Reason_BpAboveTarget_ReturnsRecommendation()
    {
        var state = new PatientClinicalState(6.5m, 60m, 145, 92, false);

        var recs = GuidelineTreatmentReasoner.Reason(state, DefaultTargets);

        Assert.Contains(recs, r => r.Code == "BP_ABOVE_TARGET");
    }

    [Fact]
    public void Reason_AllWithinTarget_ReturnsEmpty()
    {
        var state = new PatientClinicalState(6.5m, 90m, 120, 78, false);

        var recs = GuidelineTreatmentReasoner.Reason(state, DefaultTargets);

        Assert.Empty(recs);
    }
}
