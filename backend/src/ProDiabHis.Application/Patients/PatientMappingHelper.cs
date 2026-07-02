namespace ProDiabHis.Application.Patients;

/// <summary>Helper map DB row → DTO</summary>
public static class PatientMappingHelper
{
    /// <summary>Mask CMND: giu 2 ki tu dau va 2 ki tu cuoi, an phan giua</summary>
    public static string? MaskIdNumber(string? plain)
    {
        if (string.IsNullOrEmpty(plain) || plain.Length <= 4) return plain;
        return plain[..2] + new string('*', plain.Length - 4) + plain[^2..];
    }

    /// <summary>Mask ma the BHYT: giu 5 ki tu dau, an phan con lai</summary>
    public static string? MaskCardNo(string? plain)
    {
        if (string.IsNullOrEmpty(plain) || plain.Length <= 5) return plain;
        return plain[..5] + new string('*', plain.Length - 5);
    }

    /// <summary>Tinh tuoi tu ngay sinh</summary>
    public static int? CalcAge(DateOnly? dob)
    {
        if (dob is null) return null;
        var today = DateOnly.FromDateTime(DateTime.Today);
        var age = today.Year - dob.Value.Year;
        if (dob.Value.AddYears(age) > today) age--;
        return age;
    }

    public static PatientResponse MapRow(dynamic row)
    {
        var address = (row.province_code != null || row.district_code != null ||
                       row.ward_code != null || row.street != null)
            ? new AddressDto(
                (string?)row.province_code,
                (string?)row.district_code,
                (string?)row.ward_code,
                (string?)row.street)
            : null;

        DateOnly? dob = row.date_of_birth is DateTime dt
            ? DateOnly.FromDateTime(dt)
            : null;

        DateOnly? idCardIssuedDate = row.id_card_issued_date is DateTime icd
            ? DateOnly.FromDateTime(icd)
            : null;

        return new PatientResponse(
            Id: row.id is Guid g ? g : Guid.Parse((string)row.id),
            TenantId: (int)(row.tenant_id ?? 0),
            Code: (string)row.code,
            FullName: (string)row.full_name,
            Gender: (string?)row.gender,
            DateOfBirth: dob,
            Age: CalcAge(dob),
            IdNumber: MaskIdNumber((string?)row.id_number_masked),
            Phone: (string?)row.phone,
            Email: (string?)row.email,
            Address: address,
            Occupation: (string?)row.occupation,
            Ethnicity: (string?)row.ethnicity,
            AvatarUrl: (string?)row.avatar_url,
            ReceptionNote: (string?)row.reception_note,
            BloodType: (string?)row.blood_type,
            AllergiesSummary: (string?)row.allergies_summary,
            BhytCardNo: (string?)row.bhyt_card_no_masked,
            BhytValidTo: null,
            Status: (string?)row.status ?? "ACTIVE",
            CreatedAt: (DateTime)row.created_at,
            UpdatedAt: (DateTime)row.updated_at,
            IdCardIssuedDate: idCardIssuedDate,
            IdCardIssuedPlace: (string?)row.id_card_issued_place,
            Nationality: (string?)row.nationality ?? "VN",
            PatientType: (string?)row.patient_type ?? "SERVICE",
            MaritalStatus: (string?)row.marital_status,
            VisitType: (string?)row.visit_type);
    }
}
