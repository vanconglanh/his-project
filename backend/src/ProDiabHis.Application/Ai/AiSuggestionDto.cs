namespace ProDiabHis.Application.Ai;

public record TreatmentSuggestionResponse(
    Guid LogId,
    string DisclaimerText,
    string BodyText,
    bool FallbackUsed,
    IReadOnlyList<GuidelineRecommendation> RuleDerived);
