using FluentValidation;

namespace ProDiabHis.Application.Appointments;

public class CreateAppointmentCommandValidator : AbstractValidator<CreateAppointmentCommand>
{
    public CreateAppointmentCommandValidator()
    {
        RuleFor(x => x.Request).SetValidator(new CreateAppointmentRequestValidator());
    }
}

public class CreateAppointmentRequestValidator : AbstractValidator<CreateAppointmentRequest>
{
    public CreateAppointmentRequestValidator()
    {
        RuleFor(x => x.AppointmentAt).NotEqual(default(DateTime))
            .WithMessage("Thời điểm hẹn khám không được để trống");

        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.PatientRef) || !string.IsNullOrWhiteSpace(x.PatientNameTemp))
            .WithMessage("Phải chọn bệnh nhân đã có hồ sơ hoặc nhập tên bệnh nhân vãng lai");

        RuleFor(x => x.DurationMinutes).GreaterThan(0)
            .WithMessage("Thời gian dự kiến khám phải lớn hơn 0")
            .When(x => x.DurationMinutes.HasValue);

        RuleFor(x => x.Source).Must(AppointmentSource.IsValid)
            .WithMessage("Kênh đặt lịch không hợp lệ")
            .When(x => !string.IsNullOrEmpty(x.Source));

        RuleFor(x => x.PatientPhone).Matches(@"^(\+84|0)\d{9,10}$")
            .WithMessage("Số điện thoại không hợp lệ")
            .When(x => !string.IsNullOrEmpty(x.PatientPhone));
    }
}

public class UpdateAppointmentCommandValidator : AbstractValidator<UpdateAppointmentCommand>
{
    public UpdateAppointmentCommandValidator()
    {
        RuleFor(x => x.Request).SetValidator(new UpdateAppointmentRequestValidator());
    }
}

public class UpdateAppointmentRequestValidator : AbstractValidator<UpdateAppointmentRequest>
{
    public UpdateAppointmentRequestValidator()
    {
        RuleFor(x => x.AppointmentAt).NotEqual(default(DateTime))
            .WithMessage("Thời điểm hẹn khám không được để trống");

        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.PatientRef) || !string.IsNullOrWhiteSpace(x.PatientNameTemp))
            .WithMessage("Phải chọn bệnh nhân đã có hồ sơ hoặc nhập tên bệnh nhân vãng lai");

        RuleFor(x => x.DurationMinutes).GreaterThan(0)
            .WithMessage("Thời gian dự kiến khám phải lớn hơn 0")
            .When(x => x.DurationMinutes.HasValue);

        RuleFor(x => x.PatientPhone).Matches(@"^(\+84|0)\d{9,10}$")
            .WithMessage("Số điện thoại không hợp lệ")
            .When(x => !string.IsNullOrEmpty(x.PatientPhone));
    }
}

public class UpdateAppointmentStatusCommandValidator : AbstractValidator<UpdateAppointmentStatusCommand>
{
    public UpdateAppointmentStatusCommandValidator()
    {
        RuleFor(x => x.Status).Must(AppointmentStatus.IsValid)
            .WithMessage("Trạng thái lịch hẹn không hợp lệ");
    }
}
