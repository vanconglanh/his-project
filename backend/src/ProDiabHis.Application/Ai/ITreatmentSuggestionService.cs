namespace ProDiabHis.Application.Ai;

public record TreatmentSuggestionContext(
    Guid PatientId,
    Guid? EncounterId,
    PatientClinicalState State,
    CarePathwayTargetSnapshot Targets,
    IReadOnlyList<GuidelineRecommendation> RuleDerived);

public record TreatmentSuggestionResult(
    string DisclaimerText,
    string BodyText,
    bool FallbackUsed,
    object RuleDerived);

/// <summary>
/// Dich vu goi y dieu tri (tham khao). Hien tai chi format rule_derived thanh van ban
/// tieng Viet (guideline-driven, khong goi LLM). Se cam Azure OpenAI o phien ban sau.
/// </summary>
public interface ITreatmentSuggestionService
{
    Task<TreatmentSuggestionResult> SuggestAsync(TreatmentSuggestionContext ctx, CancellationToken ct = default);
}
