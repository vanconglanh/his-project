using FluentValidation;

namespace ProDiabHis.Application.Pharmacy.Drugs;

public class CreateDrugCommandValidator : AbstractValidator<CreateDrugCommand>
{
    public CreateDrugCommandValidator()
    {
        RuleFor(x => x.Request).SetValidator(new DrugMasterRequestValidator());
    }
}

public class UpdateDrugCommandValidator : AbstractValidator<UpdateDrugCommand>
{
    public UpdateDrugCommandValidator()
    {
        RuleFor(x => x.Request).SetValidator(new DrugMasterRequestValidator());
    }
}

public class DrugMasterRequestValidator : AbstractValidator<DrugMasterRequest>
{
    public DrugMasterRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().WithMessage("Mã thuốc không được để trống")
            .MaximumLength(50).WithMessage("Mã thuốc tối đa 50 ký tự");
        RuleFor(x => x.NameVi).NotEmpty().WithMessage("Tên thuốc không được để trống")
            .MaximumLength(255).WithMessage("Tên thuốc tối đa 255 ký tự");
        RuleFor(x => x.NameEn).MaximumLength(255).WithMessage("Tên thuốc (tiếng Anh) tối đa 255 ký tự")
            .When(x => !string.IsNullOrEmpty(x.NameEn));
        RuleFor(x => x.GenericName).MaximumLength(255).WithMessage("Tên hoạt chất tối đa 255 ký tự")
            .When(x => !string.IsNullOrEmpty(x.GenericName));
        RuleFor(x => x.Unit).NotEmpty().WithMessage("Đơn vị tính không được để trống")
            .MaximumLength(20).WithMessage("Đơn vị tính tối đa 20 ký tự");
        RuleFor(x => x.Form).NotEmpty().WithMessage("Dạng bào chế không được để trống");
        RuleFor(x => x.AtcCode).MaximumLength(20).WithMessage("Mã ATC tối đa 20 ký tự")
            .When(x => !string.IsNullOrEmpty(x.AtcCode));
        RuleFor(x => x.Manufacturer).MaximumLength(255).WithMessage("Tên nhà sản xuất tối đa 255 ký tự")
            .When(x => !string.IsNullOrEmpty(x.Manufacturer));
        RuleFor(x => x.Country).MaximumLength(100).WithMessage("Tên quốc gia tối đa 100 ký tự")
            .When(x => !string.IsNullOrEmpty(x.Country));
        RuleFor(x => x.DtqgDrugCode).MaximumLength(50).WithMessage("Mã thuốc ĐTQG tối đa 50 ký tự")
            .When(x => !string.IsNullOrEmpty(x.DtqgDrugCode));
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0).WithMessage("Giá thuốc phải lớn hơn hoặc bằng 0")
            .When(x => x.Price.HasValue);
    }
}
