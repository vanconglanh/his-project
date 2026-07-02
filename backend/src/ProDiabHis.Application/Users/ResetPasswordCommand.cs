using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProDiabHis.Application.Auth;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.Users;

public record ResetPasswordCommand(string Token, string NewPassword) : IRequest<Result>;

public class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.Token).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().Must(p =>
            p.Length >= 12 && p.Any(char.IsUpper) && p.Any(char.IsLower)
            && p.Any(char.IsDigit) && p.Any(c => !char.IsLetterOrDigit(c)))
            .WithMessage("Mật khẩu phải có tối thiểu 12 ký tự, gồm chữ hoa, chữ thường, số và ký tự đặc biệt");
    }
}

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _passwordHasher;

    public ResetPasswordCommandHandler(IApplicationDbContext db, IPasswordHasher passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result> Handle(ResetPasswordCommand req, CancellationToken ct)
    {
        var user = await _db.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.PasswordResetToken == req.Token && u.DeletedAt == null, ct);

        if (user is null || user.PasswordResetExpiresAt < DateTime.UtcNow)
            return Result.Failure("USER_INVITE_EXPIRED", "Liên kết đặt lại mật khẩu đã hết hạn hoặc không hợp lệ");

        user.PasswordHash = _passwordHasher.Hash(req.NewPassword);
        user.PasswordResetToken = null;
        user.PasswordResetExpiresAt = null;
        user.Status = Domain.Entities.UserStatus.Active;
        user.IsActive = true;
        user.FailedLoginCount = 0;

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
