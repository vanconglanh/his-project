namespace ProDiabHis.Infrastructure.Ai;

/// <summary>
/// Cau hinh Azure OpenAI (danh cho phien ban sau khi tich hop LLM that). Hien tai
/// GuidelineSuggestionService CHUA goi Azure OpenAI - chi doc config de san sang cam vao.
/// </summary>
public class AzureOpenAiOptions
{
    public bool Enabled { get; set; } = false;
    public string Endpoint { get; set; } = "";
    public string ApiKey { get; set; } = "";
    public string Deployment { get; set; } = "gpt-4o";
    public int TimeoutSeconds { get; set; } = 15;
}
