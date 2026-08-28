namespace ProDiabHis.Infrastructure.Integrations.Docosan;

/// <summary>Cau hinh tich hop Docosan (docs/erd/telehealth-docosan.md muc 5.1). KHONG hardcode gia tri that.</summary>
public class DocosanOptions
{
    public const string SectionName = "Docosan";

    public string BaseUrl { get; set; } = "https://api.staging.docosan.com/";
    public string ApiKey { get; set; } = string.Empty;
    public string Environment { get; set; } = "staging";
    public string ClientAppUrl { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 20;
    public int RetryCount { get; set; } = 3;

    public DocosanSyncJobOptions SyncJob { get; set; } = new();
}

public class DocosanSyncJobOptions
{
    public int IntervalMinutes { get; set; } = 5;
    public int LookAheadHours { get; set; } = 48;
    public int LookBackHours { get; set; } = 24;
}
