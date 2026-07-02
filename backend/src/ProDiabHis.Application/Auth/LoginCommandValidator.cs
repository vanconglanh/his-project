using FluentValidation;

namespace ProDiabHis.Application.Auth;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email khong duoc de trong")
            .EmailAddress().WithMessage("Email khong dung dinh dang");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Mat khau khong duoc de trong")
            .MinimumLength(6).WithMessage("Mat khau phai co it nhat 6 ky tu");
    }
}
