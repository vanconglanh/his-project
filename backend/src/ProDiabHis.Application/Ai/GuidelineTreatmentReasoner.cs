namespace ProDiabHis.Application.Ai;

public record PatientClinicalState(
    decimal? Hba1c,
    decimal? Egfr,
    int? BpSystolic,
    int? BpDiastolic,
    bool OnMetformin);

public record CarePathwayTargetSnapshot(decimal? Hba1cTarget, int? BpSysTarget, int? BpDiaTarget, decimal? EgfrThreshold);

public record GuidelineRecommendation(string Code, string Text, string Source);

/// <summary>
/// Suy khuyen nghi dieu tri THUAN theo guideline (QD 5481/QD-BYT, ADA Standards of Care).
/// KHONG neu lieu cu the, chi goi y huong xu tri de bac si quyet dinh cuoi cung.
/// </summary>
public static class GuidelineTreatmentReasoner
{
    private const string Source5481 = "QĐ 5481/QĐ-BYT";
    private const string SourceAda = "ADA Standards of Care";

    public static List<GuidelineRecommendation> Reason(PatientClinicalState state, CarePathwayTargetSnapshot targets)
    {
        var recs = new List<GuidelineRecommendation>();

        if (state.Hba1c.HasValue && targets.Hba1cTarget.HasValue && state.Hba1c.Value > targets.Hba1cTarget.Value)
        {
            recs.Add(new GuidelineRecommendation(
                "HBA1C_ABOVE_TARGET",
                $"Chưa đạt mục tiêu HbA1c ({state.Hba1c.Value:0.0}% > {targets.Hba1cTarget.Value:0.0}%): " +
                "cân nhắc tăng cường điều trị theo phác đồ QĐ 5481.",
                Source5481));
        }

        if (state.Egfr.HasValue && targets.EgfrThreshold.HasValue && state.Egfr.Value < targets.EgfrThreshold.Value && state.OnMetformin)
        {
            recs.Add(new GuidelineRecommendation(
                "EGFR_LOW_ON_METFORMIN",
                $"eGFR thấp ({state.Egfr.Value:0.0} mL/phút/1.73m2 < {targets.EgfrThreshold.Value:0.0}): " +
                "xem lại chỉ định metformin, cân nhắc điều chỉnh hoặc đổi nhóm thuốc phù hợp chức năng thận.",
                SourceAda));
        }

        if (state.BpSystolic.HasValue && state.BpDiastolic.HasValue
            && targets.BpSysTarget.HasValue && targets.BpDiaTarget.HasValue
            && (state.BpSystolic.Value > targets.BpSysTarget.Value || state.BpDiastolic.Value > targets.BpDiaTarget.Value))
        {
            recs.Add(new GuidelineRecommendation(
                "BP_ABOVE_TARGET",
                $"Huyết áp chưa đạt mục tiêu ({state.BpSystolic.Value}/{state.BpDiastolic.Value} mmHg > " +
                $"{targets.BpSysTarget.Value}/{targets.BpDiaTarget.Value} mmHg): cân nhắc điều chỉnh thuốc hạ áp.",
                Source5481));
        }

        return recs;
    }
}
