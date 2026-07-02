using MediatR;
using Microsoft.EntityFrameworkCore;
using ProDiabHis.Application.Auth;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.Users;

public record Setup2FACommand : IRequest<Result<Setup2FAResponse>>;

public record Setup2FAResponse(string Secret, string OtpauthUrl, string QrPngBase64);

public class Setup2FACommandHandler : IRequestHandler<Setup2FACommand, Result<Setup2FAResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IEncryptionService _encryption;

    public Setup2FACommandHandler(
        IApplicationDbContext db,
        ICurrentUser currentUser,
        IEncryptionService encryption)
    {
        _db = db;
        _currentUser = currentUser;
        _encryption = encryption;
    }

    public async Task<Result<Setup2FAResponse>> Handle(Setup2FACommand req, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Result<Setup2FAResponse>.Failure("AUTH_TOKEN_INVALID", "Phiên đăng nhập đã hết hạn");

        var uid = _currentUser.UserId.Value;
        var user = await _db.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == uid && u.DeletedAt == null, ct);

        if (user is null)
            return Result<Setup2FAResponse>.Failure("USER_NOT_FOUND", "Không tìm thấy người dùng");

        // Generate TOTP secret (20 bytes = base32 encoded)
        var secretBytes = System.Security.Cryptography.RandomNumberGenerator.GetBytes(20);
        var secret = Base32Encode(secretBytes);

        // Luu secret ma hoa
        user.TwoFaSecret = _encryption.Encrypt(secret);
        await _db.SaveChangesAsync(ct);

        var issuer = "ProDiabHIS";
        var otpauthUrl = $"otpauth://totp/{issuer}:{Uri.EscapeDataString(user.Email)}?secret={secret}&issuer={issuer}";

        // Tra ve QR PNG base64 don gian (URL encoded, frontend tu render QR)
        // De tich hop QR generator thu vien nhu QRCoder sau; hien tai tra base64 cua URL
        var qrBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(otpauthUrl));

        return Result<Setup2FAResponse>.Success(new Setup2FAResponse(secret, otpauthUrl, qrBase64));
    }

    // Base32 encode don gian theo RFC 4648
    private static string Base32Encode(byte[] data)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var output = new System.Text.StringBuilder();
        var bits = 0;
        var accumulator = 0;

        foreach (var b in data)
        {
            accumulator = (accumulator << 8) | b;
            bits += 8;
            while (bits >= 5)
            {
                bits -= 5;
                output.Append(alphabet[(accumulator >> bits) & 31]);
            }
        }

        if (bits > 0)
            output.Append(alphabet[(accumulator << (5 - bits)) & 31]);

        return output.ToString();
    }
}
