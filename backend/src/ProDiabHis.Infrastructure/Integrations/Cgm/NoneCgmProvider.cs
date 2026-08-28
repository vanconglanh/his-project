using ProDiabHis.Application.Diabetes.Cgm;

namespace ProDiabHis.Infrastructure.Integrations.Cgm;

/// <summary>
/// Provider mặc định khi chưa cấu hình CgmProvider:Type (hoặc = "None"). Mọi lời gọi throw
/// NotImplementedException rõ ràng — tương tự MockDigitalSignatureProvider chỉ dùng cho ký số,
/// KHÔNG dùng NoneCgmProvider này cho mục đích test/mock (test nên tự cấp fake implement riêng).
/// </summary>
public class NoneCgmProvider : ICgmDeviceProvider
{
    public string ProviderCode => "None";

    public Task<CgmLinkResult> LinkPatientAccountAsync(string patientExternalId, string authCode, CancellationToken ct = default)
        => throw new NotImplementedException(
            "Chua cau hinh nha cung cap CGM (CgmProvider:Type trong appsettings dang la None). " +
            "Hay dat CgmProvider:Type=Dexcom va cau hinh CgmProvider:Dexcom:ClientId/ClientSecret truoc khi su dung.");

    public Task<IReadOnlyList<CgmReading>> FetchReadingsAsync(
        string linkedAccountId, DateTime fromUtc, DateTime toUtc, CancellationToken ct = default)
        => throw new NotImplementedException(
            "Chua cau hinh nha cung cap CGM (CgmProvider:Type trong appsettings dang la None). " +
            "Hay dat CgmProvider:Type=Dexcom va cau hinh CgmProvider:Dexcom:ClientId/ClientSecret truoc khi su dung.");
}
