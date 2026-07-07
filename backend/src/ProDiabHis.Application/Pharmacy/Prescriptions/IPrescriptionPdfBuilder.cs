namespace ProDiabHis.Application.Pharmacy.Prescriptions;

/// <summary>Sinh file PDF don thuoc that (khong phai stub) dua tren du lieu da nap qua Dapper.</summary>
public interface IPrescriptionPdfBuilder
{
    byte[] Build(PrescriptionPdfData data);
}

/// <summary>Du lieu day du de render PDF don thuoc.</summary>
public record PrescriptionPdfData(
    string PrescriptionCode,
    DateTime PrescribedAt,
    string? Note,

    // Thong tin phong kham (letterhead)
    string ClinicName,
    string? ClinicAddress,
    string? ClinicPhone,
    string? CskcbCode,
    byte[]? ClinicLogo,

    // Thong tin benh nhan
    string PatientFullName,
    string? PatientGender,
    DateOnly? PatientDateOfBirth,
    string? PatientAddress,

    // Thong tin lam sang
    string? DiagnosisCode,
    string? DiagnosisName,

    // Bac si ke don
    string? DoctorFullName,

    IReadOnlyList<PrescriptionPdfItem> Items);

public record PrescriptionPdfItem(
    int Stt,
    string DrugName,
    string? Strength,
    string? Unit,
    decimal Quantity,
    string Dosage,
    string Frequency,
    string Route,
    int DurationDays,
    string? Instructions);
