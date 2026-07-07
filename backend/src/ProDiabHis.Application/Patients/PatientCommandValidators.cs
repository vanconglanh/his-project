using FluentValidation;

namespace ProDiabHis.Application.Patients;

public class CreatePatientCommandValidator : AbstractValidator<CreatePatientCommand>
{
    public CreatePatientCommandValidator()
    {
        RuleFor(x => x.Request).SetValidator(new CreatePatientRequestValidator());
    }
}

public class UpdatePatientCommandValidator : AbstractValidator<UpdatePatientCommand>
{
    public UpdatePatientCommandValidator()
    {
        RuleFor(x => x.Request).SetValidator(new UpdatePatientRequestValidator());
    }
}

public class CreatePatientRequestValidator : AbstractValidator<CreatePatientRequest>
{
    public CreatePatientRequestValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().WithMessage("Họ tên không được để trống")
            .MinimumLength(2).WithMessage("Họ tên tối thiểu 2 ký tự")
            .MaximumLength(200).WithMessage("Họ tên tối đa 200 ký tự");

        RuleFor(x => x.IdNumber).Matches(@"^\d{9}$|^\d{12}$")
            .WithMessage("Số CMND/CCCD phải gồm 9 hoặc 12 chữ số")
            .When(x => !string.IsNullOrEmpty(x.IdNumber));

        RuleFor(x => x.Phone).Matches(@"^(\+84|0)\d{9,10}$")
            .WithMessage("Số điện thoại không hợp lệ")
            .When(x => !string.IsNullOrEmpty(x.Phone));

        RuleFor(x => x.Email).EmailAddress().WithMessage("Email không hợp lệ")
            .When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x.BloodType).MaximumLength(10).WithMessage("Nhóm máu tối đa 10 ký tự")
            .When(x => !string.IsNullOrEmpty(x.BloodType));

        RuleFor(x => x.IdCardIssuedPlace).MaximumLength(100).WithMessage("Nơi cấp CMND/CCCD tối đa 100 ký tự")
            .When(x => !string.IsNullOrEmpty(x.IdCardIssuedPlace));

        RuleFor(x => x.Occupation).MaximumLength(100).WithMessage("Nghề nghiệp tối đa 100 ký tự")
            .When(x => !string.IsNullOrEmpty(x.Occupation));

        RuleFor(x => x.Ethnicity).MaximumLength(50).WithMessage("Dân tộc tối đa 50 ký tự")
            .When(x => !string.IsNullOrEmpty(x.Ethnicity));
    }
}

public class UpdatePatientRequestValidator : AbstractValidator<UpdatePatientRequest>
{
    public UpdatePatientRequestValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().WithMessage("Họ tên không được để trống")
            .MinimumLength(2).WithMessage("Họ tên tối thiểu 2 ký tự")
            .MaximumLength(200).WithMessage("Họ tên tối đa 200 ký tự");

        RuleFor(x => x.IdNumber).Matches(@"^\d{9}$|^\d{12}$")
            .WithMessage("Số CMND/CCCD phải gồm 9 hoặc 12 chữ số")
            .When(x => !string.IsNullOrEmpty(x.IdNumber));

        RuleFor(x => x.Phone).Matches(@"^(\+84|0)\d{9,10}$")
            .WithMessage("Số điện thoại không hợp lệ")
            .When(x => !string.IsNullOrEmpty(x.Phone));

        RuleFor(x => x.Email).EmailAddress().WithMessage("Email không hợp lệ")
            .When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x.BloodType).MaximumLength(10).WithMessage("Nhóm máu tối đa 10 ký tự")
            .When(x => !string.IsNullOrEmpty(x.BloodType));

        RuleFor(x => x.IdCardIssuedPlace).MaximumLength(100).WithMessage("Nơi cấp CMND/CCCD tối đa 100 ký tự")
            .When(x => !string.IsNullOrEmpty(x.IdCardIssuedPlace));

        RuleFor(x => x.Occupation).MaximumLength(100).WithMessage("Nghề nghiệp tối đa 100 ký tự")
            .When(x => !string.IsNullOrEmpty(x.Occupation));

        RuleFor(x => x.Ethnicity).MaximumLength(50).WithMessage("Dân tộc tối đa 50 ký tự")
            .When(x => !string.IsNullOrEmpty(x.Ethnicity));
    }
}
