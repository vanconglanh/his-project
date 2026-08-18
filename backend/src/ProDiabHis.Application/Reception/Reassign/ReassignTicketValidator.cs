using FluentValidation;

namespace ProDiabHis.Application.Reception.Reassign;

/// <summary>[G05] Validate yeu cau dieu phoi luot kham.</summary>
public class ReassignTicketCommandValidator : AbstractValidator<ReassignTicketCommand>
{
    public ReassignTicketCommandValidator()
    {
        RuleFor(x => x.Request).NotNull().WithMessage("Thiếu nội dung điều phối");

        RuleFor(x => x.Request.Reason)
            .NotEmpty().WithMessage("Bắt buộc nhập lý do điều phối")
            .MinimumLength(5).WithMessage("Lý do điều phối phải có tối thiểu 5 ký tự")
            .MaximumLength(500).WithMessage("Lý do điều phối tối đa 500 ký tự")
            .When(x => x.Request is not null);

        RuleFor(x => x.Request)
            .Must(r => r.DoctorId.HasValue || r.RoomId.HasValue)
            .WithMessage("Phải chọn bác sĩ hoặc phòng khám mới để điều phối")
            .When(x => x.Request is not null);
    }
}
