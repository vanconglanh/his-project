using FluentValidation;

namespace ProDiabHis.Application.Encounters.Addenda;

/// <summary>
/// [G03] Rang buoc do dai/dinh dang cho ban dinh chinh.
/// LUU Y: truong hop "thieu ly do" duoc xu ly trong handler de tra dung ma loi nghiep vu
/// AMENDMENT_REASON_REQUIRED (409/422) thay vi VALIDATION_ERROR chung (400).
/// </summary>
public class CreateEncounterAddendumValidator : AbstractValidator<CreateEncounterAddendumCommand>
{
    public CreateEncounterAddendumValidator()
    {
        RuleFor(x => x.Request.Reason)
            .MaximumLength(2000).WithMessage("Lý do đính chính tối đa 2000 ký tự");

        RuleFor(x => x.Request.TargetTable)
            .MaximumLength(64).WithMessage("Tên bảng đích tối đa 64 ký tự");

        RuleFor(x => x.Request.TargetId)
            .MaximumLength(36).WithMessage("ID nội dung đính chính không hợp lệ");
    }
}
