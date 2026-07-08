using MediatR;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.Encounters;

// ── Create ──
public record CreateEncounterRequest(
    string PatientId,
    string? RoomId,
    string? DoctorId,
    string EncounterType,
    string ReasonForVisit,
    string? ChiefComplaint);

public record CreateEncounterCommand(CreateEncounterRequest Request)
    : IRequest<Result<EncounterResponse>>;

// ── Admit (Tiep don -> Kham): tao (hoac lay lai) luot kham tu ve hang doi ──
public record AdmitTicketToEncounterCommand(Guid TicketId)
    : IRequest<Result<AdmitTicketResponse>>;

/// <param name="EncounterId">Luot kham (moi tao hoac da co san).</param>
/// <param name="Created">true = vua tao moi; false = da ton tai luot kham active.</param>
public record AdmitTicketResponse(Guid EncounterId, bool Created);

// ── Update ──
public record UpdateEncounterRequest(
    string? RoomId,
    string? DoctorId,
    string? EncounterType,
    string? ReasonForVisit,
    string? ChiefComplaint);

public record UpdateEncounterCommand(Guid EncounterId, UpdateEncounterRequest Request)
    : IRequest<Result<EncounterResponse>>;

// ── Start ──
public record StartEncounterCommand(Guid EncounterId)
    : IRequest<Result<EncounterResponse>>;

// ── Close ──
public record CloseEncounterCommand(Guid EncounterId)
    : IRequest<Result<bool>>;

// ── Chief complaint ──
public record UpdateChiefComplaintCommand(Guid EncounterId, string ChiefComplaint)
    : IRequest<Result<bool>>;

// ── Diagnosis ──
public record DiagnosisRequest(string Icd10Code, string Type, string? Note);

public record AddDiagnosisCommand(Guid EncounterId, DiagnosisRequest Request)
    : IRequest<Result<DiagnosisResponse>>;

public record RemoveDiagnosisCommand(Guid EncounterId, Guid DiagnosisId)
    : IRequest<Result<bool>>;
