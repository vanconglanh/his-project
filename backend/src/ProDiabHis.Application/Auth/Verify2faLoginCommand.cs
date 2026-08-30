using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OtpNet;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.PublicApi;
using ProDiabHis.Contracts.Auth;

namespace ProDiabHis.Application.Auth;

/// <summary>Buoc 2 dang nhap: xac thuc ma TOTP (hoac recovery code) bang mfa-pending token de lay
/// token day du. Chi ap dung cho user DA bat 2FA.</summary>
public record Verify2faLoginCommand(string MfaPendingToken, string Code) : IRequest<Result<LoginResponse>>;

public class Verify2faLoginCommandHandler : IRequestHandler<Verify2faLoginCommand, Result<LoginResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly IJwtService _jwtService;
    private readonly IRateLimiter _rateLimiter;
    private readonly IEncryptionService _encryption;
    private readonly LoginCommandHandler _loginHandler;
    private readonly ILogger<Verify2faLoginCommandHandler> _logger;

    public Verify2faLoginCommandHandler(
        IApplicationDbContext db,
        IJwtService jwtService,
        IRateLimiter rateLimiter,
        IEncryptionService encryption,
        LoginCommandHandler loginHandler,
        ILogger<Verify2faLoginCommandHandler> logger)
    {
        _db = db;
        _jwtService = jwtService;
        _rateLimiter = rateLimiter;
        _encryption = encryption;
        _loginHandler = loginHandler;
        _logger = logger;
    }

    public async Task<Result<LoginResponse>> Handle(Verify2faLoginCommand request, CancellationToken cancellationToken)
    {
        // Validate mfa-pending token
        var payload = _jwtService.ValidateMfaToken(request.MfaPendingToken, "mfa-pending");
        if (payload is null)
        {
            _logger.LogWarning("Verify 2FA that bai: mfa-pending token khong hop le hoac het han");
            return Result<LoginResponse>.Failure(
                "AUTH_MFA_TOKEN_INVALID", "Phiên xác thực 2 lớp đã hết hạn, vui lòng đăng nhập lại");
        }

        var userId = payload.Value.UserId;

        // Rate limit chong brute-force: 5 lan / 5 phut / user
        var rateKey = $"mfa-verify:{userId}";
        var allowed = await _rateLimiter.AllowAsync(rateKey, 5, TimeSpan.FromMinutes(5), cancellationToken);
        if (!allowed)
        {
            _logger.LogWarning("Verify 2FA bi chan do vuot rate limit cho user {UserId}", userId);
            return Result<LoginResponse>.Failure(
                "AUTH_MFA_TOO_MANY_ATTEMPTS", "Bạn đã nhập sai mã quá nhiều lần, vui lòng thử lại sau ít phút");
        }

        var user = await _db.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == userId && u.DeletedAt == null, cancellationToken);

        if (user is null || !user.TwoFaEnabled || string.IsNullOrEmpty(user.TwoFaSecret))
        {
            _logger.LogWarning("Verify 2FA that bai: user {UserId} khong ton tai hoac chua bat 2FA", userId);
            return Result<LoginResponse>.Failure(
                "AUTH_MFA_TOKEN_INVALID", "Phiên xác thực 2 lớp đã hết hạn, vui lòng đăng nhập lại");
        }

        // 1) Thu verify ma TOTP
        var secret = _encryption.Decrypt(user.TwoFaSecret);
        var secretBytes = Base32Decode(secret);
        var totp = new Totp(secretBytes);
        var totpOk = totp.VerifyTotp(request.Code, out _, new VerificationWindow(2, 2));

        if (!totpOk)
        {
            // 2) Thu recovery code (dung 1 lan): SHA256(code) so voi list da luu (ma hoa)
            if (!TryConsumeRecoveryCode(user, request.Code))
            {
                _logger.LogWarning("Verify 2FA that bai: ma khong dung cho user {UserId}", userId);
                return Result<LoginResponse>.Failure(
                    "AUTH_MFA_INVALID_CODE", "Mã xác thực 2 lớp không đúng");
            }
        }

        // Thanh cong -> cap token day du (BuildSuccessResponseAsync cung goi SaveChanges, luu ca viec xoa
        // recovery code da dung neu co).
        var response = await _loginHandler.BuildSuccessResponseAsync(user, cancellationToken);
        return Result<LoginResponse>.Success(response);
    }

    /// <summary>Neu <paramref name="code"/> khop mot recovery code chua dung -> xoa khoi list (dung 1 lan)
    /// va tra ve true. Khong luu DB o day — BuildSuccessResponseAsync se SaveChanges.</summary>
    private bool TryConsumeRecoveryCode(Domain.Entities.User user, string code)
    {
        if (string.IsNullOrEmpty(user.TwoFaRecoveryCodesJson)) return false;

        List<string>? hashes;
        try
        {
            var json = _encryption.Decrypt(user.TwoFaRecoveryCodesJson);
            hashes = JsonSerializer.Deserialize<List<string>>(json);
        }
        catch
        {
            return false;
        }
        if (hashes is null || hashes.Count == 0) return false;

        var candidate = ComputeSha256(code);
        var idx = hashes.FindIndex(h => string.Equals(h, candidate, StringComparison.OrdinalIgnoreCase));
        if (idx < 0) return false;

        hashes.RemoveAt(idx);
        user.TwoFaRecoveryCodesJson = _encryption.Encrypt(JsonSerializer.Serialize(hashes));
        return true;
    }

    private static string ComputeSha256(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLower();
    }

    private static byte[] Base32Decode(string input)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        input = input.TrimEnd('=').ToUpperInvariant();
        var output = new List<byte>();
        var bits = 0;
        var accumulator = 0;

        foreach (var c in input)
        {
            var index = alphabet.IndexOf(c);
            if (index < 0) continue;
            accumulator = (accumulator << 5) | index;
            bits += 5;
            if (bits >= 8)
            {
                bits -= 8;
                output.Add((byte)((accumulator >> bits) & 255));
            }
        }

        return output.ToArray();
    }
}
