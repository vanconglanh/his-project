using FluentValidation;
using ProDiabHis.Domain.Entities;

namespace ProDiabHis.Application.Billing;

/// <summary>Quy tac dung chung cho tao/sua voucher (BM-95).</summary>
internal static class VoucherRules
{
    public static IRuleBuilderOptions<T, decimal> Percent<T>(this IRuleBuilder<T, decimal> rb) =>
        rb.InclusiveBetween(0m, 100m).WithMessage("Phần trăm giảm phải trong khoảng 0 - 100");

    public static IRuleBuilderOptions<T, string> Scope<T>(this IRuleBuilder<T, string> rb) =>
        rb.Must(v => v != null && VoucherScope.Valid.Contains(v))
          .WithMessage("Nhóm áp dụng phải thuộc: EXAM, LAB, IMAGING, DRUG, ALL");
}

public class CreateVoucherRequestValidator : AbstractValidator<CreateVoucherRequest>
{
    public CreateVoucherRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().WithMessage("Mã voucher không được để trống")
            .MaximumLength(50).WithMessage("Mã voucher tối đa 50 ký tự");
        RuleFor(x => x.Description).MaximumLength(255).WithMessage("Mô tả tối đa 255 ký tự");
        RuleFor(x => x.DiscountPercent).Percent();
        RuleFor(x => x.AppliesTo).Scope();
        RuleFor(x => x).Must(x => !x.ValidFrom.HasValue || !x.ValidTo.HasValue || x.ValidFrom <= x.ValidTo)
            .WithMessage("Ngày bắt đầu phải nhỏ hơn hoặc bằng ngày kết thúc");
        RuleFor(x => x.MaxUses).GreaterThan(0).When(x => x.MaxUses.HasValue)
            .WithMessage("Số lượt dùng tối đa phải lớn hơn 0");
    }
}

public class UpdateVoucherRequestValidator : AbstractValidator<UpdateVoucherRequest>
{
    public UpdateVoucherRequestValidator()
    {
        RuleFor(x => x.Description).MaximumLength(255).WithMessage("Mô tả tối đa 255 ký tự");
        RuleFor(x => x.DiscountPercent).Percent();
        RuleFor(x => x.AppliesTo).Scope();
        RuleFor(x => x).Must(x => !x.ValidFrom.HasValue || !x.ValidTo.HasValue || x.ValidFrom <= x.ValidTo)
            .WithMessage("Ngày bắt đầu phải nhỏ hơn hoặc bằng ngày kết thúc");
        RuleFor(x => x.MaxUses).GreaterThan(0).When(x => x.MaxUses.HasValue)
            .WithMessage("Số lượt dùng tối đa phải lớn hơn 0");
    }
}

// Lop boc cap Command de MediatR ValidationBehavior (IValidator<TCommand>) that su chay.
public class CreateVoucherCommandValidator : AbstractValidator<CreateVoucherCommand>
{
    public CreateVoucherCommandValidator()
    {
        RuleFor(x => x.Request).NotNull().WithMessage("Thiếu dữ liệu voucher")
            .SetValidator(new CreateVoucherRequestValidator());
    }
}

public class UpdateVoucherCommandValidator : AbstractValidator<UpdateVoucherCommand>
{
    public UpdateVoucherCommandValidator()
    {
        RuleFor(x => x.Request).NotNull().WithMessage("Thiếu dữ liệu voucher")
            .SetValidator(new UpdateVoucherRequestValidator());
    }
}

public class ApplyVoucherCommandValidator : AbstractValidator<ApplyVoucherCommand>
{
    public ApplyVoucherCommandValidator()
    {
        RuleFor(x => x.Request).NotNull().WithMessage("Thiếu mã voucher");
        RuleFor(x => x.Request.Code).NotEmpty().WithMessage("Mã voucher không được để trống")
            .When(x => x.Request != null);
    }
}
