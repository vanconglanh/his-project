using System.Data;
using Microsoft.Extensions.Logging.Abstractions;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.Diabetes.Cgm;
using Xunit;

namespace ProDiabHis.UnitTests.Diabetes;

/// <summary>
/// FR-711 [P2]: Test cho luồng liên kết tài khoản CGM (Portal). Không cần DB thật cho các nhánh lỗi
/// dự kiến (provider không hỗ trợ / thiếu authCode / provider chưa cấu hình) — handler phải trả lỗi
/// TRƯỚC khi chạm DB, kiểm chứng bằng <see cref="ThrowingDapperConnectionFactory"/>.
/// </summary>
public class CgmHandlersTests
{
    private static readonly ITenantProvider Tenant = new FakeTenantProvider();
    private static readonly IEncryptionService Enc = new FakeEncryptionService();
    private static readonly IAuditService Audit = new NoopAuditService();

    [Fact]
    public async Task Handle_UnsupportedProvider_ReturnsFailure_KhongDungDenDb()
    {
        var handler = new LinkCgmAccountCommandHandler(
            new ThrowingDapperConnectionFactory(), Tenant, Enc,
            new NoneCgmProviderFake(), Audit,
            NullLogger<LinkCgmAccountCommandHandler>.Instance);

        var cmd = new LinkCgmAccountCommand(Guid.NewGuid(), new CgmLinkRequest("LibreView", "abc123"));

        var result = await handler.Handle(cmd, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("CGM_PROVIDER_NOT_SUPPORTED", result.ErrorCode);
    }

    [Fact]
    public async Task Handle_MissingAuthCode_ReturnsFailure_KhongDungDenDb()
    {
        var handler = new LinkCgmAccountCommandHandler(
            new ThrowingDapperConnectionFactory(), Tenant, Enc,
            new NoneCgmProviderFake(), Audit,
            NullLogger<LinkCgmAccountCommandHandler>.Instance);

        var cmd = new LinkCgmAccountCommand(Guid.NewGuid(), new CgmLinkRequest("Dexcom", ""));

        var result = await handler.Handle(cmd, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("CGM_AUTH_CODE_REQUIRED", result.ErrorCode);
    }

    private class FakeTenantProvider : ITenantProvider
    {
        public int TenantId => 1;
        public void SetTenantId(int tenantId) { }
    }

    private class FakeEncryptionService : IEncryptionService
    {
        public string Encrypt(string plaintext) => "enc:" + plaintext;
        public string Decrypt(string ciphertext) => ciphertext.Replace("enc:", "");
    }

    private class NoopAuditService : IAuditService
    {
        public Task LogAsync(string action, string? resourceType, string? resourceId, object? details = null, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task LogAsync(string action, string? resourceType, string? resourceId, AuditSeverity severity, bool crossTenantAttempt = false, string? requestId = null, object? details = null, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    /// <summary>Không tạo connection nào — nếu handler lỡ chạm DB ở nhánh validate-fail, test sẽ throw.</summary>
    private class ThrowingDapperConnectionFactory : IDapperConnectionFactory
    {
        public IDbConnection CreateConnection() =>
            throw new InvalidOperationException("Handler khong nen cham DB khi validate that bai truoc (provider/authCode)");
    }

    private class NoneCgmProviderFake : ICgmDeviceProvider
    {
        public string ProviderCode => "None";
        public Task<CgmLinkResult> LinkPatientAccountAsync(string patientExternalId, string authCode, CancellationToken ct = default)
            => throw new InvalidOperationException("Khong nen goi provider khi validate input that bai");
        public Task<IReadOnlyList<CgmReading>> FetchReadingsAsync(string linkedAccountId, DateTime fromUtc, DateTime toUtc, CancellationToken ct = default)
            => throw new NotSupportedException();
    }
}
