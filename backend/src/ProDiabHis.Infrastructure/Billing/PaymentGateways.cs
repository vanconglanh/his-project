using ProDiabHis.Application.Billing;
using QRCoder;
using System.Security.Cryptography;
using System.Text;

namespace ProDiabHis.Infrastructure.Billing;

/// <summary>Gateway tien mat - luon thanh cong, khong goi external</summary>
public class CashGateway : IPaymentGateway
{
    public string Provider => "CASH";

    public Task<QrGenerateResult> GenerateQrAsync(QrGenerateRequest request, CancellationToken ct = default)
        => Task.FromResult(new QrGenerateResult(string.Empty, null));

    public Task<CardChargeResult> ChargeCardAsync(CardChargeRequest request, CancellationToken ct = default)
        => Task.FromResult(new CardChargeResult(true, Guid.NewGuid().ToString("N"), null));

    public bool VerifyWebhookSignature(string payload, string signature, string secret) => true;
}

/// <summary>
/// VietQR gateway - build EMV QR format chuan VN
/// Ref: https://vietqr.io/danh-sach-api/tao-ma-qr/
/// </summary>
public class VietQrGateway : IPaymentGateway
{
    public string Provider => "VIETQR";

    // Bank info config (demo: MB Bank)
    private const string BankBin = "970422";
    private const string AccountNo = "0123456789";
    private const string AccountName = "PHONG KHAM PRO DIAB";

    public Task<QrGenerateResult> GenerateQrAsync(QrGenerateRequest request, CancellationToken ct = default)
    {
        // Build EMV QR string for VietQR
        var addInfo = $"TT HOA DON {request.TransactionRef}";
        var qrString = BuildVietQrString(request.Amount, addInfo);
        var png = GenerateQrPng(qrString);
        var base64 = Convert.ToBase64String(png);
        var deepLink = $"https://img.vietqr.io/image/{BankBin}-{AccountNo}-compact2.jpg?amount={request.Amount}&addInfo={Uri.EscapeDataString(addInfo)}&accountName={Uri.EscapeDataString(AccountName)}";

        return Task.FromResult(new QrGenerateResult(base64, deepLink));
    }

    public Task<CardChargeResult> ChargeCardAsync(CardChargeRequest request, CancellationToken ct = default)
        => Task.FromResult(new CardChargeResult(false, null, "VietQR khong ho tro charge card"));

    public bool VerifyWebhookSignature(string payload, string signature, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload))).ToLower();
        return hash == signature.ToLower();
    }

    /// <summary>Build VietQR EMV QR string (simplified EMVCo format)</summary>
    public static string BuildVietQrString(decimal amount, string addInfo)
    {
        // EMVCo QR Code Specification for Payment Systems
        // ID 00: Payload Format Indicator = "01"
        // ID 01: Point of Initiation = "12" (dynamic)
        // ID 38: NAPAS QR -> bank_bin + account_no
        // ID 49: Country Code = "VN"
        // ID 54: Transaction Amount
        // ID 58: Country Code
        // ID 62: Additional Data

        var sb = new StringBuilder();
        sb.Append("000201"); // Payload format indicator
        sb.Append("010212"); // Dynamic QR

        // ID 38: Merchant Account Information (NAPAS)
        var napas = "A000000727"; // NAPAS AID
        var binTag = $"0006{BankBin}";
        var accTag = $"01{AccountNo.Length:D2}{AccountNo}";
        var merchantInfo = $"0010{napas}{binTag}{accTag}";
        sb.Append($"38{merchantInfo.Length:D2}{merchantInfo}");

        sb.Append("5303704"); // Currency VND
        var amountStr = ((long)amount).ToString();
        sb.Append($"54{amountStr.Length:D2}{amountStr}");
        sb.Append("5802VN"); // Country Code

        // Merchant name
        var merName = AccountName[..Math.Min(AccountName.Length, 25)];
        sb.Append($"59{merName.Length:D2}{merName}");
        sb.Append("6007HANOI");

        // Additional data (bill reference)
        var billRef = addInfo[..Math.Min(addInfo.Length, 25)];
        var addData = $"08{billRef.Length:D2}{billRef}";
        sb.Append($"62{addData.Length:D2}{addData}");

        // CRC placeholder, then compute
        sb.Append("6304");
        var crc = ComputeCrc16(sb.ToString());
        sb.Append(crc.ToString("X4"));

        return sb.ToString();
    }

    private static ushort ComputeCrc16(string input)
    {
        ushort crc = 0xFFFF;
        foreach (var b in Encoding.UTF8.GetBytes(input))
        {
            crc ^= (ushort)(b << 8);
            for (int i = 0; i < 8; i++)
                crc = (crc & 0x8000) != 0 ? (ushort)((crc << 1) ^ 0x1021) : (ushort)(crc << 1);
        }
        return crc;
    }

    private static byte[] GenerateQrPng(string data)
    {
        using var gen = new QRCodeGenerator();
        var qrData = gen.CreateQrCode(data, QRCodeGenerator.ECCLevel.M);
        using var qrCode = new PngByteQRCode(qrData);
        return qrCode.GetGraphic(10);
    }
}

/// <summary>MoMo gateway - mock dev</summary>
public class MomoGateway : IPaymentGateway
{
    public string Provider => "MOMO";

    public Task<QrGenerateResult> GenerateQrAsync(QrGenerateRequest request, CancellationToken ct = default)
    {
        var deepLink = $"momo://app?action=payWithApp&isScanQR=true&serviceType=qr&sid=mock_{request.TransactionRef}&v=3.0&appId=PRODIAB";
        var mockPng = GenerateMockQrPng($"MOMO|{request.Amount}|{request.TransactionRef}");
        return Task.FromResult(new QrGenerateResult(Convert.ToBase64String(mockPng), deepLink));
    }

    public Task<CardChargeResult> ChargeCardAsync(CardChargeRequest request, CancellationToken ct = default)
        => Task.FromResult(new CardChargeResult(false, null, "MoMo khong ho tro card charge"));

    public bool VerifyWebhookSignature(string payload, string signature, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload))).ToLower();
        return hash == signature.ToLower();
    }

    private static byte[] GenerateMockQrPng(string data)
    {
        using var gen = new QRCodeGenerator();
        var qrData = gen.CreateQrCode(data, QRCodeGenerator.ECCLevel.M);
        using var qrCode = new PngByteQRCode(qrData);
        return qrCode.GetGraphic(8);
    }
}

/// <summary>VNPay gateway - mock dev, build signed URL</summary>
public class VnpayGateway : IPaymentGateway
{
    public string Provider => "VNPAY";

    public Task<QrGenerateResult> GenerateQrAsync(QrGenerateRequest request, CancellationToken ct = default)
    {
        var mockUrl = $"https://sandbox.vnpayment.vn/paymentv2/vpcpay.html?vnp_TxnRef={request.TransactionRef}&vnp_Amount={(long)(request.Amount * 100)}&vnp_Locale=vn";
        var mockPng = GenerateMockQrPng(mockUrl);
        return Task.FromResult(new QrGenerateResult(Convert.ToBase64String(mockPng), mockUrl));
    }

    public Task<CardChargeResult> ChargeCardAsync(CardChargeRequest request, CancellationToken ct = default)
        => Task.FromResult(new CardChargeResult(false, null, "VNPay card charge chua ho tro"));

    public bool VerifyWebhookSignature(string payload, string signature, string secret)
    {
        // VNPay uses HMAC-SHA512
        using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(secret));
        var hash = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload))).ToLower();
        return hash == signature.ToLower();
    }

    private static byte[] GenerateMockQrPng(string data)
    {
        using var gen = new QRCodeGenerator();
        var qrData = gen.CreateQrCode(data, QRCodeGenerator.ECCLevel.M);
        using var qrCode = new PngByteQRCode(qrData);
        return qrCode.GetGraphic(8);
    }
}

/// <summary>Visa/Master gateway - Stripe-like mock</summary>
public class VisaMasterGateway : IPaymentGateway
{
    public string Provider => "VISA";

    public Task<QrGenerateResult> GenerateQrAsync(QrGenerateRequest request, CancellationToken ct = default)
        => Task.FromResult(new QrGenerateResult(string.Empty, null));

    public Task<CardChargeResult> ChargeCardAsync(CardChargeRequest request, CancellationToken ct = default)
    {
        // Mock: any card_token starting with "fail" will fail
        if (request.CardToken.StartsWith("fail", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(new CardChargeResult(false, null, "Card declined by mock gateway"));

        var txnId = $"ch_{Guid.NewGuid().ToString("N")[..16]}";
        return Task.FromResult(new CardChargeResult(true, txnId, null));
    }

    public bool VerifyWebhookSignature(string payload, string signature, string secret) => true;
}
