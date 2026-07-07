using MediatR;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.Appointments;

public record CreateAppointmentCommand(CreateAppointmentRequest Request) : IRequest<Result<AppointmentResponse>>;

public record UpdateAppointmentCommand(int Id, UpdateAppointmentRequest Request) : IRequest<Result<AppointmentResponse>>;

public record UpdateAppointmentStatusCommand(int Id, string Status) : IRequest<Result<AppointmentResponse>>;

public record ListAppointmentsQuery(
    DateTime? From,
    DateTime? To,
    string? DoctorRef,
    string? Status,
    string? Q,
    int Page,
    int PageSize) : IRequest<PagedResult<AppointmentResponse>>;

public record GetAppointmentQuery(int Id) : IRequest<Result<AppointmentResponse>>;

public record ListDoctorOptionsQuery() : IRequest<List<OptionDto>>;

public record ListPatientOptionsQuery(string? Q) : IRequest<List<PatientOptionDto>>;
