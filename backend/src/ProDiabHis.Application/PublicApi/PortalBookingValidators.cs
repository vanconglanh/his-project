using FluentValidation;

namespace ProDiabHis.Application.PublicApi;

/// <summary>
/// Validator cho luong dat lich qua Cong benh nhan (Patient Portal).
/// Bat buoc BranchId de tranh lich hen "mo coi" khong xac dinh chi nhanh
/// (xem docs/erd/branch-multi-chi-nhanh.md muc 3.1 #3 diab_his_sch_appointments).
/// </summary>
public class CreatePortalAppointmentCommandValidator : AbstractValidator<CreatePortalAppointmentCommand>
{
    public CreatePortalAppointmentCommandValidator()
    {
        RuleFor(x => x.Request).SetValidator(new PortalAppointmentCreateRequestValidator());
    }
}

public class PortalAppointmentCreateRequestValidator : AbstractValidator<PortalAppointmentCreateRequest>
{
    public PortalAppointmentCreateRequestValidator()
    {
        RuleFor(x => x.BranchId)
            .GreaterThan(0)
            .WithMessage("Vui lòng chọn chi nhánh trước khi đặt lịch khám");

        RuleFor(x => x.AppointmentAt)
            .NotEmpty()
            .WithMessage("Vui lòng chọn thời gian đặt lịch");
    }
}
