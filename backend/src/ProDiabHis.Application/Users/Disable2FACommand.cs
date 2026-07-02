using MediatR;
using Microsoft.EntityFrameworkCore;
using ProDiabHis.Application.Auth;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.Users;

public record Disable2FACommand(string Password, string? Code) : IRequest<Result>;

public class Disable2FACommandHandler : IRequestHandler<Disable2FACommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IPasswordHasher _passwordHasher;

    public Disable2FACommandHandler(
        IApplicationDbContext db,
        ICurrentUser currentUser,
        IPasswordHasher passwordHasher)
    {
        _db = db;
        _currentUser = currentUser;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result> Handle(Disable2FACommand req, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Result.Failure("AUTH_TOKEN_INVALID", "Phiên đăng nhập đã hết hạn");

        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == _currentUser.UserId.Value && u.DeletedAt == null, ct);

        if (user is null)
            return Result.Failure("USER_NOT_FOUND", "Không tìm thấy người dùng");

        if (!_passwordHasher.Verify(req.Password, user.PasswordHash))
            return Result.Failure("AUTH_INVALID_CREDENTIALS", "Mật khẩu không đúng");

        user.TwoFaEnabled = false;
        user.TwoFaSecret = null;
        user.TwoFaRecoveryCodesJson = null;
        await _db.SaveChangesAsync(ct);

        return Result.Success();
    }
}
