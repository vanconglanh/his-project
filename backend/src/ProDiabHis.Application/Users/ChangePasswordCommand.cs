using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProDiabHis.Application.Auth;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.Users;

public record ChangePasswordCommand(string OldPassword, string NewPassword) : IRequest<Result>;

public class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.OldPassword).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().Must(p =>
            p.Length >= 12 && p.Any(char.IsUpper) && p.Any(char.IsLower)
            && p.Any(char.IsDigit) && p.Any(c => !char.IsLetterOrDigit(c)))
            .WithMessage("Mật khẩu phải có tối thiểu 12 ký tự, gồm chữ hoa, chữ thường, số và ký tự đặc biệt");
    }
}

public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IPasswordHasher _passwordHasher;

    public ChangePasswordCommandHandler(
        IApplicationDbContext db,
        ICurrentUser currentUser,
        IPasswordHasher passwordHasher)
    {
        _db = db;
        _currentUser = currentUser;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result> Handle(ChangePasswordCommand req, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Result.Failure("AUTH_TOKEN_INVALID", "Phiên đăng nhập đã hết hạn");

        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == _currentUser.UserId.Value && u.DeletedAt == null, ct);

        if (user is null)
            return Result.Failure("USER_NOT_FOUND", "Không tìm thấy người dùng");

        if (!_passwordHasher.Verify(req.OldPassword, user.PasswordHash))
            return Result.Failure("AUTH_INVALID_CREDENTIALS", "Email hoặc mật khẩu không đúng");

        user.PasswordHash = _passwordHasher.Hash(req.NewPassword);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
