namespace ProDiabHis.Application.Telehealth.Integration;

/// <summary>
/// Boc REST API cua Docosan (FR-801..803). Khong chua business logic.
/// Xem thiet ke: docs/erd/telehealth-docosan.md muc 5.
/// </summary>
public interface IDocosanClient
{
    /// <summary>Kiem tra benh nhan da co tai khoan Docosan theo so dien thoai chua.</summary>
    Task<bool> IsUserExistAsync(string phoneNumber, CancellationToken ct);

    /// <summary>
    /// POST api/register-internal (x-www-form-urlencoded, chi can x-api-key).
    /// Idempotent phia Docosan theo so dien thoai — dung ca cho dang ky moi lan dau
    /// va lay lai access_token khi da co tai khoan.
    /// </summary>
    Task<DocosanRegisterResultDto> RegisterInternalUserAsync(DocosanRegisterUserRequest req, CancellationToken ct);

    /// <summary>
    /// Telehealth: BAT BUOC dung ham nay (khong dung create-order thuong).
    /// payment_info.services[].id phai tro toi service co service_type='telemedicine'.
    /// </summary>
    Task<DocosanAppointmentDto> CreateOrderPartnerAsync(
        DocosanCreateBookingRequest req, string patientToken, CancellationToken ct);

    /// <summary>Lay chi tiet lich hen (dung cho polling sync job).</summary>
    Task<DocosanAppointmentDto> GetAppointmentDetailAsync(
        int appointmentId, string patientToken, CancellationToken ct);

    /// <summary>Huy lich hen.</summary>
    Task<DocosanCommonResultDto> CancelAppointmentAsync(
        int appointmentId, string? reason, string patientToken, CancellationToken ct);
}

/// <summary>Ket qua goi API Docosan (bao boc code/data theo dung format cua Docosan).</summary>
public record DocosanCommonResultDto(bool Success, int? Code, string? Message);

public record DocosanRegisterUserRequest(
    string? Email,
    string PhoneNumber,
    string DisplayName,
    string? Gender,
    string Language,
    string Type = "patient",
    bool IsGetCaresOrderInfo = true);

public record DocosanRegisterResultDto(
    bool Success,
    string? AccessToken,
    int? DocosanUserId,
    string? ErrorCode,
    string? ErrorMessage);

public record DocosanCreateBookingRequest(
    int DocosanClinicId,
    int DocosanDoctorId,
    int DocosanServiceId,
    DateTime ScheduledStart,
    string? Symptom,
    string? PatientPhone,
    string? PatientName);

public record DocosanAppointmentDto(
    bool Success,
    int? AppointmentId,
    int? TeleMedicineId,
    int? DocosanPatientId,
    string? Mode,
    string? Status,
    string? AppointmentLink,
    DateTime? ScheduledStart,
    DateTime? ScheduledEnd,
    string? PaymentStatus,
    bool? ShowJoinCall,
    string? ErrorCode,
    string? ErrorMessage,
    object? RawPayload);
