using System.Security.Cryptography;
using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OtpNet;
using ProDiabHis.Application.Auth;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.Users;

public record Enable2FACommand(string Code) : IRequest<Result<Enable2FAResponse>>;

public record Enable2FAResponse(IReadOnlyList<string> RecoveryCodes);

public class Enable2FACommandHandler : IRequestHandler<Enable2FACommand, Result<Enable2FAResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IEncryptionService _encryption;

    public Enable2FACommandHandler(
        IApplicationDbContext db,
        ICurrentUser currentUser,
        IEncryptionService encryption)
    {
        _db = db;
        _currentUser = currentUser;
        _encryption = encryption;
    }

    public async Task<Result<Enable2FAResponse>> Handle(Enable2FACommand req, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Result<Enable2FAResponse>.Failure("AUTH_TOKEN_INVALID", "Phiên đăng nhập đã hết hạn");

        var uid = _currentUser.UserId.Value;
        var user = await _db.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == uid && u.DeletedAt == null, ct);

        if (user is null)
            return Result<Enable2FAResponse>.Failure("USER_NOT_FOUND", "Không tìm thấy người dùng");

        if (user.TwoFaEnabled)
            return Result<Enable2FAResponse>.Failure("TWO_FA_ALREADY_ENABLED", "Xác thực 2 lớp đã được kích hoạt");

        if (string.IsNullOrEmpty(user.TwoFaSecret))
            return Result<Enable2FAResponse>.Failure("TWO_FA_NOT_SETUP", "Vui lòng thiết lập 2FA trước");

        // Giai ma secret va verify TOTP code
        var secret = _encryption.Decrypt(user.TwoFaSecret);
        var secretBytes = Base32Decode(secret);
        var totp = new Totp(secretBytes);

        if (!totp.VerifyTotp(req.Code, out _, new VerificationWindow(2, 2)))
            return Result<Enable2FAResponse>.Failure("TWO_FA_INVALID_CODE", "Mã xác thực 2 lớp không đúng");

        // Tao 10 recovery codes
        var recoveryCodes = GenerateRecoveryCodes(10);
        var hashedCodes = recoveryCodes.Select(c => ComputeSha256(c)).ToList();

        user.TwoFaEnabled = true;
        user.TwoFaRecoveryCodesJson = _encryption.Encrypt(JsonSerializer.Serialize(hashedCodes));
        await _db.SaveChangesAsync(ct);

        return Result<Enable2FAResponse>.Success(new Enable2FAResponse(recoveryCodes));
    }

    private static List<string> GenerateRecoveryCodes(int count)
    {
        var codes = new List<string>();
        for (int i = 0; i < count; i++)
        {
            var bytes = RandomNumberGenerator.GetBytes(10);
            // Format: XXXXX-XXXXX
            var hex = Convert.ToHexString(bytes).ToLower();
            codes.Add($"{hex[..5]}-{hex[5..10]}");
        }
        return codes;
    }

    private static string ComputeSha256(string input)
    {
        var bytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(input));
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
