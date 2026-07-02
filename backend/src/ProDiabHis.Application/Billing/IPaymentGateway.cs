namespace ProDiabHis.Application.Billing;

public record QrGenerateRequest(
    Guid BillingId,
    string Provider,
    decimal Amount,
    string TransactionRef,
    int ExpiresInSeconds);

public record QrGenerateResult(
    string QrPayloadBase64,
    string? QrUrl);

public record CardChargeRequest(
    Guid BillingId,
    decimal Amount,
    string CardToken,
    string Provider,
    string? ThreeDsNonce);

public record CardChargeResult(
    bool Success,
    string? ProviderTxnId,
    string? ErrorMessage);

/// <summary>Abstraction cho cong thanh toan</summary>
public interface IPaymentGateway
{
    string Provider { get; }
    Task<QrGenerateResult> GenerateQrAsync(QrGenerateRequest request, CancellationToken ct = default);
    Task<CardChargeResult> ChargeCardAsync(CardChargeRequest request, CancellationToken ct = default);
    bool VerifyWebhookSignature(string payload, string signature, string secret);
}
