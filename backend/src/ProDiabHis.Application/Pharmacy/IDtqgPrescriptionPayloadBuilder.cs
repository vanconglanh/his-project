namespace ProDiabHis.Application.Pharmacy;

/// <summary>
/// Dựng dữ liệu đơn thuốc (trường <c>don_thuoc</c>) cho payload gửi ĐTQG: đọc dòng thuốc + chẩn đoán
/// ICD-10 + thông tin bệnh nhân (kể cả số thẻ BHYT đã giải mã) từ schema canonical <c>diab_his_*</c>.
/// </summary>
public interface IDtqgPrescriptionPayloadBuilder
{
    /// <summary>Trả <c>null</c> nếu không tìm thấy đơn thuốc/bệnh nhân theo tenant.</summary>
    Task<DtqgPrescriptionData?> BuildAsync(string prescriptionId, int tenantId, CancellationToken ct = default);
}

/// <summary>Dữ liệu đơn thuốc chuẩn hoá cho payload ĐTQG (field envelope cuối cùng theo spec donthuocquocgia.vn).</summary>
public record DtqgPrescriptionData(
    DtqgPatientInfo Patient,
    string? DiagnosisIcd10,
    string? DiagnosisName,
    IReadOnlyList<DtqgDrugItem> Drugs,
    string? Note);

/// <summary>Thông tin bệnh nhân trong đơn (số thẻ BHYT đã giải mã).</summary>
public record DtqgPatientInfo(string? Code, string FullName, string? Gender, int? YearOfBirth, string? InsuranceCardNo);

/// <summary>Một dòng thuốc trong đơn.</summary>
public record DtqgDrugItem(
    int No,
    string DrugName,
    string? GenericName,
    string? Strength,
    string? Unit,
    decimal Quantity,
    string? Dosage,
    string? Frequency,
    string? Route,
    int? DurationDays,
    string? DtqgDrugCode,
    string? Note);
