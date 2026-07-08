namespace ProDiabHis.Application.Appointments;

public record AppointmentResponse(
    int Id,
    DateTime AppointmentAt,
    int DurationMinutes,
    string Status,
    string Source,
    string? PatientRef,
    string? PatientName,
    string? PatientPhone,
    string? DoctorRef,
    string? DoctorName,
    string? Note);

public record CreateAppointmentRequest(
    string? PatientRef,
    string? PatientNameTemp,
    string? PatientPhone,
    string? DoctorRef,
    DateTime AppointmentAt,
    int? DurationMinutes,
    string? Source,
    string? Note);

public record UpdateAppointmentRequest(
    string? PatientRef,
    string? PatientNameTemp,
    string? PatientPhone,
    string? DoctorRef,
    DateTime AppointmentAt,
    int? DurationMinutes,
    string? Note);

public record UpdateAppointmentStatusRequest(string Status);

public record OptionDto(string Value, string Label);

public record PatientOptionDto(string Value, string Label, string? Phone);

/// <summary>Cac gia tri hop le cho cot status (ENUM trong diab_his_sch_appointments)</summary>
public static class AppointmentStatus
{
    public static readonly string[] All = { "PENDING", "CONFIRMED", "CHECKED_IN", "CANCELLED", "NO_SHOW" };
    public static bool IsValid(string status) => All.Contains(status);
}

/// <summary>Cac gia tri hop le cho cot source (ENUM trong diab_his_sch_appointments)</summary>
public static class AppointmentSource
{
    public static readonly string[] All = { "WALK_IN", "PHONE", "WEB", "API", "APP" };
    public static bool IsValid(string? source) => source is not null && All.Contains(source);
}
