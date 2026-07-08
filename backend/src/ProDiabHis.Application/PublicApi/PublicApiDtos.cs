namespace ProDiabHis.Application.PublicApi;

// --- VAPID ---
public record VapidStatusResponse(bool Configured, string? PublicKey, DateTime? GeneratedAt);
public record VapidGenerateResponse(string PublicKey, DateTime GeneratedAt);

// --- Public Patient ---
public record PublicRegisterPatientRequest(
    string FullName,
    string Gender,
    DateOnly Dob,
    string Phone,
    string? Address = null,
    string? Email = null
);

public record PublicPatientResponse(
    string PatientCode,
    string FullName,
    DateTime CreatedAt
);

// --- Public Appointment ---
public record PublicAppointmentBookRequest(
    string PatientPhone,
    string PatientName,
    Guid? DoctorId,
    Guid? DepartmentId,
    Guid? ServiceId,
    DateTime AppointmentAt,
    string? Note = null,
    string? PartnerReference = null
);

public record PublicAppointmentResponse(
    Guid Id,
    string AppointmentCode,
    string Status,
    DateTime AppointmentAt,
    string? DoctorName,
    Guid? SourcePartnerId,
    string? PartnerReference
);

// --- Visit Lookup ---
public record LookupTokenResponse(string LookupToken, int ExpiresIn);

// --- API Partners ---
public record ApiPartnerResponse(
    Guid Id,
    string Name,
    string? ContactEmail,
    string ApiKeyMasked,
    List<string> Scopes,
    int RateLimitPerMin,
    int DailyQuota,
    string Status,
    DateTime? ExpiresAt,
    List<string> IpWhitelist,
    DateTime CreatedAt
);

public record ApiPartnerCreatedResponse(
    Guid Id,
    string Name,
    string? ContactEmail,
    string ApiKeyMasked,
    string ApiKeyPlain,
    List<string> Scopes,
    int RateLimitPerMin,
    int DailyQuota,
    string Status,
    DateTime? ExpiresAt,
    List<string> IpWhitelist,
    DateTime CreatedAt
);

public record ApiPartnerCreateRequest(
    string Name,
    string? ContactEmail,
    List<string> Scopes,
    int RateLimitPerMin = 60,
    int DailyQuota = 10000,
    DateTime? ExpiresAt = null,
    List<string>? IpWhitelist = null
);

public record ApiPartnerUpdateRequest(
    string? Name,
    string? ContactEmail,
    List<string>? Scopes,
    int? RateLimitPerMin,
    int? DailyQuota,
    string? Status,
    DateTime? ExpiresAt,
    List<string>? IpWhitelist
);

public record ApiUsageStatsResponse(
    int TotalRequests,
    int SuccessCount,
    int ErrorCount,
    List<EndpointStat> ByEndpoint,
    List<DayStat> ByDay
);

public record EndpointStat(string Path, int Count);
public record DayStat(DateOnly Date, int Count);

public record ApiRequestLogEntry(
    Guid Id,
    string Method,
    string Path,
    int StatusCode,
    int DurationMs,
    string? Ip,
    DateTime CalledAt,
    string? ErrorCode
);

// --- Notifications ---
public record NotificationResponse(
    Guid Id,
    string Type,
    string Title,
    string Body,
    object? DataJson,
    DateTime? ReadAt,
    DateTime CreatedAt
);

public record WebPushSubscriptionRequest(
    string Endpoint,
    string P256dhKey,
    string AuthKey,
    string? UserAgent = null
);

public record NotificationPreferenceRequest(
    string Position = "TOP_RIGHT",
    bool SoundEnabled = true,
    string SoundName = "default",
    bool BrowserPushEnabled = false,
    List<string>? TypesDisabled = null
);

public record TestSendNotificationRequest(
    string Title,
    string Body,
    string? UserId = null
);

public record NotificationPreferenceResponse(
    string Position,
    bool SoundEnabled,
    string SoundName,
    bool BrowserPushEnabled,
    List<string> TypesDisabled,
    DateTime UpdatedAt
);

// --- Portal ---
public record PortalAuthOtpRequest(string Phone, string? TenantCode = null);
public record PortalVerifyRequest(string Phone, string Otp);

public record PortalAuthResponse(
    string AccessToken,
    string TokenType,
    int ExpiresIn,
    string PatientCode,
    string FullName
);

public record PortalMeResponse(
    string PatientCode,
    string FullName,
    string Gender,
    DateOnly Dob,
    string Phone,
    string? Address,
    string? BhytNumber
);

public record PortalEncounterResponse(
    Guid Id,
    string EncounterCode,
    DateTime VisitedAt,
    string DoctorName,
    string ChiefComplaint,
    List<DiagnosisItem> Diagnosis,
    string Status
);

public record DiagnosisItem(string Icd10, string Name);

public record PortalPrescriptionResponse(
    Guid Id,
    string PrescriptionCode,
    DateTime IssuedAt,
    string DoctorName,
    string? Note,
    string? DtqgCode,
    List<PrescriptionItemDto> Items
);

public record PrescriptionItemDto(
    string DrugName,
    string Dosage,
    decimal Quantity,
    string UsageInstruction
);

public record PortalAppointmentCreateRequest(
    DateTime AppointmentAt,
    Guid? DoctorId = null,
    Guid? DepartmentId = null,
    string? Note = null
);

// --- Portal: Lab results ---
public record PortalLabResultResponse(
    Guid Id,
    string TestName,
    DateTime ResultDate,
    string? Conclusion,
    string Status
);

// --- Portal: Encounter detail ---
public record PortalEncounterDetailResponse(
    Guid Id,
    string EncounterCode,
    DateTime VisitedAt,
    string DoctorName,
    string ChiefComplaint,
    List<DiagnosisItem> Diagnosis,
    string? Conclusion,
    string? DoctorAdvice,
    List<PortalEncounterPrescriptionItem> PrescriptionItems
);

public record PortalEncounterPrescriptionItem(
    string DrugName,
    string Dosage,
    string? Frequency,
    int? DurationDays,
    string? Instructions
);

// --- Portal: Queue status ---
public record PortalQueueStatusResponse(
    string TicketNo,
    string? RoomName,
    string Status,
    string? CurrentCalledNo,
    int WaitingAhead,
    int EstWaitMinutes
);

// --- Portal: Booking ---
public record PortalDoctorOptionResponse(Guid DoctorRef, string FullName);

public record PortalSlotResponse(DateTime SlotAt, bool Available);

// --- Portal: Med reminders ---
public record PortalMedReminderResponse(
    Guid Id,
    string DrugName,
    string? DoseLabel,
    string TimeSlot,
    TimeOnly RemindTime,
    DateOnly StartDate,
    DateOnly? EndDate,
    bool Enabled
);

public record UpdateMedReminderRequest(bool Enabled);

// --- Portal: Notification preferences ---
public record PortalNotifyPreferencesResponse(bool Push, bool Email);
public record UpdatePortalNotifyPreferencesRequest(bool Push, bool Email);

public record PortalPushSubscribeRequest(string Endpoint, string P256dh, string Auth);
public record PortalPushUnsubscribeRequest(string Endpoint);

// --- Admin: Doctor schedules ---
public record DoctorScheduleResponse(
    int Id,
    Guid DoctorRef,
    int DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int SlotMinutes,
    int MaxPerSlot,
    DateOnly? EffectiveFrom,
    DateOnly? EffectiveTo,
    bool Enabled
);

public record DoctorScheduleUpsertRequest(
    Guid DoctorRef,
    int DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int SlotMinutes = 15,
    int MaxPerSlot = 1,
    DateOnly? EffectiveFrom = null,
    DateOnly? EffectiveTo = null,
    bool Enabled = true
);

public record ScheduleBlockResponse(
    int Id,
    Guid DoctorRef,
    DateOnly BlockDate,
    TimeOnly? StartTime,
    TimeOnly? EndTime,
    string? Reason
);

public record ScheduleBlockCreateRequest(
    Guid DoctorRef,
    DateOnly BlockDate,
    TimeOnly? StartTime = null,
    TimeOnly? EndTime = null,
    string? Reason = null
);
