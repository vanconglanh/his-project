using Microsoft.Extensions.Configuration;
using ProDiabHis.Application.Auth;

namespace ProDiabHis.Infrastructure.Auth;

public class PasswordHasher : IPasswordHasher
{
    // BUG UX#7: work factor lay tu config de moi truong dev co the ha cost (login nhanh hon),
    // production giu 12. Chan trong khoang an toan [10..14]. Verify tu suy ra cost tu chinh hash
    // nen ha work factor chi anh huong hash MOI, khong pha vo hash cu.
    private readonly int _workFactor;

    public PasswordHasher(IConfiguration configuration)
    {
        var configured = configuration.GetValue<int?>("Security:BCryptWorkFactor") ?? 12;
        _workFactor = Math.Clamp(configured, 10, 14);
    }

    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password, workFactor: _workFactor);

    public bool Verify(string password, string hash) => BCrypt.Net.BCrypt.Verify(password, hash);
}
