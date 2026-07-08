using System.Text;
using ProDiabHis.Application.Ai;

namespace ProDiabHis.Infrastructure.Ai;

/// <summary>
/// Trien khai goi y dieu tri guideline-driven (KHONG goi LLM). Format rule_derived
/// (suy tu GuidelineTreatmentReasoner) thanh van ban tieng Viet de bac si tham khao.
/// FallbackUsed = true vi chua co Azure OpenAI (danh cho phien ban sau, xem TODO).
/// </summary>
public class GuidelineSuggestionService : ITreatmentSuggestionService
{
    private const string DisclaimerV1 = "Gợi ý tham khảo — bác sĩ quyết định cuối cùng.";
    private readonly AzureOpenAiOptions _options;

    public GuidelineSuggestionService(AzureOpenAiOptions options)
    {
        _options = options;
    }

    public Task<TreatmentSuggestionResult> SuggestAsync(TreatmentSuggestionContext ctx, CancellationToken ct = default)
    {
        // TODO(phase-2): neu _options.Enabled, goi Azure OpenAI (chat completion) voi
        // system prompt guardrail + grounding tu ctx.RuleDerived, roi format BodyText
        // tu response LLM. Hien tai luon dung nhanh guideline-driven (fallback).

        string bodyText;
        if (ctx.RuleDerived.Count == 0)
        {
            bodyText = "Chưa phát hiện chỉ số nào lệch mục tiêu điều trị theo phác đồ hiện có. " +
                       "Tiếp tục theo dõi định kỳ theo lịch tái khám.";
        }
        else
        {
            var sb = new StringBuilder();
            sb.AppendLine("Các gợi ý tham khảo dựa trên chỉ số hiện tại và phác đồ điều trị:");
            foreach (var rec in ctx.RuleDerived)
            {
                sb.AppendLine($"- {rec.Text} (Nguồn: {rec.Source})");
            }
            bodyText = sb.ToString().TrimEnd();
        }

        var result = new TreatmentSuggestionResult(
            DisclaimerText: DisclaimerV1,
            BodyText: bodyText,
            FallbackUsed: true,
            RuleDerived: ctx.RuleDerived);

        return Task.FromResult(result);
    }
}
