using ProDiabHis.Application.Reports;

namespace ProDiabHis.Application.Appointments;

/// <summary>
/// Du lieu render Giay hen tai kham. Nguon: diab_his_sch_appointments (schema legacy INT-id,
/// khong join truc tiep duoc sang diab_his_pat_patients / diab_his_sec_users vi khac kieu du lieu
/// (int vs char(36)) — dung cac cot du phong (patient_name_temp/patient_phone) tren chinh bang appointment.
/// </summary>
public record AppointmentSlipData(
    LetterheadDto Letterhead,
    int AppointmentId,
    DateTime AppointmentAt,
    int DurationMinutes,
    string Status,
    string PatientName,
    string? PatientPhone,
    string? DoctorName,
    int? DepartmentId,
    string? Note);
