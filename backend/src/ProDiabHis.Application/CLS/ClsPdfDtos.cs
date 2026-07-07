using ProDiabHis.Application.Reports;

namespace ProDiabHis.Application.CLS;

/// <summary>Mot dong chi dinh (XN hoac CDHA) tren phieu in.</summary>
public record ClsOrderSlipItemDto(
    int Stt,
    string Name,
    string? Detail,
    string Priority,
    string? Note);

/// <summary>Du lieu day du de render Phieu chi dinh XN / CDHA (QuestPDF).</summary>
public record ClsOrderSlipData(
    LetterheadDto Letterhead,
    string DocTitle,
    string DocSubtitle,
    string SlipCode,
    DateTime IssuedAt,
    string PatientCode,
    string PatientFullName,
    string? PatientGender,
    DateOnly? PatientDob,
    string? DiagnosisCode,
    string? DiagnosisName,
    string? DoctorFullName,
    IReadOnlyList<ClsOrderSlipItemDto> Items);
