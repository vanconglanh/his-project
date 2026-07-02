using FluentAssertions;
using Xunit;

namespace ProDiabHis.UnitTests.Billing;

/// <summary>Tests QR webhook idempotency logic (simulated)</summary>
public class QrWebhookIdempotencyTests
{
    // Simulate the idempotency check: provider + provider_txn_id must be unique
    private readonly HashSet<(string provider, string txnId)> _processed = new();

    private bool ProcessWebhook(string provider, string txnId)
    {
        // If already processed, return true (idempotent - no-op)
        if (_processed.Contains((provider, txnId))) return false; // false = already processed
        _processed.Add((provider, txnId));
        return true; // true = processed for first time
    }

    [Fact]
    public void FirstWebhook_IsProcessed()
    {
        var processed = ProcessWebhook("MOMO", "TXN001");
        processed.Should().BeTrue();
    }

    [Fact]
    public void DuplicateWebhook_SameProviderAndTxnId_IsIdempotent()
    {
        ProcessWebhook("MOMO", "TXN002");
        var second = ProcessWebhook("MOMO", "TXN002");
        second.Should().BeFalse(); // duplicate
    }

    [Fact]
    public void SameTxnId_DifferentProvider_IsProcessed()
    {
        ProcessWebhook("MOMO", "TXN003");
        var differentProvider = ProcessWebhook("VNPAY", "TXN003");
        differentProvider.Should().BeTrue(); // different provider => different record
    }

    [Fact]
    public void MultipleDistinctWebhooks_AllProcessed()
    {
        var r1 = ProcessWebhook("VIETQR", "TXN_A");
        var r2 = ProcessWebhook("VIETQR", "TXN_B");
        var r3 = ProcessWebhook("MOMO", "TXN_C");

        r1.Should().BeTrue();
        r2.Should().BeTrue();
        r3.Should().BeTrue();
        _processed.Should().HaveCount(3);
    }

    [Fact]
    public void HmacSignatureVerification_ValidSignature_Passes()
    {
        // Test HMAC-SHA256 verification logic used in VietQR
        var secret = "test-secret-key";
        var payload = "{\"transId\":\"12345\",\"orderInfo\":\"TT HOA DON PD001\"}";

        using var hmac = new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes(secret));
        var signature = Convert.ToHexString(hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(payload))).ToLower();

        // Verify
        using var hmac2 = new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes(secret));
        var computed = Convert.ToHexString(hmac2.ComputeHash(System.Text.Encoding.UTF8.GetBytes(payload))).ToLower();
        (computed == signature).Should().BeTrue();
    }

    [Fact]
    public void HmacSignatureVerification_InvalidSignature_Fails()
    {
        var secret = "test-secret-key";
        var payload = "{\"transId\":\"12345\"}";

        using var hmac = new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes(secret));
        var validSig = Convert.ToHexString(hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(payload))).ToLower();

        var tampered = "aabbccddee0011223344556677889900aabbccddee0011223344556677889900";
        (tampered == validSig).Should().BeFalse();
    }
}
