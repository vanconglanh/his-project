using FluentValidation;

namespace ProDiabHis.Application.PublicApi;

public class CreateApiPartnerCommandValidator : AbstractValidator<CreateApiPartnerCommand>
{
    public CreateApiPartnerCommandValidator()
    {
        RuleFor(x => x.Request).SetValidator(new ApiPartnerCreateRequestValidator());
    }
}

public class ApiPartnerCreateRequestValidator : AbstractValidator<ApiPartnerCreateRequest>
{
    public ApiPartnerCreateRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Tên đối tác không được để trống")
            .MinimumLength(2).WithMessage("Tên đối tác tối thiểu 2 ký tự")
            .MaximumLength(255).WithMessage("Tên đối tác tối đa 255 ký tự");
        RuleFor(x => x.ContactEmail).EmailAddress().WithMessage("Email liên hệ không hợp lệ")
            .When(x => !string.IsNullOrEmpty(x.ContactEmail));
        RuleFor(x => x.Scopes).NotEmpty().WithMessage("Phải chọn ít nhất một quyền truy cập (scope)");
        RuleFor(x => x.RateLimitPerMin).InclusiveBetween(1, 10000)
            .WithMessage("Giới hạn request/phút phải trong khoảng 1-10000");
        RuleFor(x => x.DailyQuota).InclusiveBetween(1, 10000000)
            .WithMessage("Hạn mức request/ngày phải trong khoảng 1-10000000");
    }
}
