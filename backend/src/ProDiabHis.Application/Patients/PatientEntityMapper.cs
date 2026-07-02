using ProDiabHis.Domain.Entities;

namespace ProDiabHis.Application.Patients;

/// <summary>Map Patient entity → PatientResponse DTO (EF Core path)</summary>
public static class PatientEntityMapper
{
    public static PatientResponse ToResponse(Patient p)
    {
        var address = (p.ProvinceCode != null || p.DistrictCode != null || p.WardCode != null || p.Street != null)
            ? new AddressDto(p.ProvinceCode, p.DistrictCode, p.WardCode, p.Street)
            : null;

        return new PatientResponse(
            Id: p.Id,
            TenantId: p.TenantId,
            Code: p.Code,
            FullName: p.FullName,
            Gender: p.Gender,
            DateOfBirth: p.DateOfBirth,
            Age: PatientMappingHelper.CalcAge(p.DateOfBirth),
            IdNumber: PatientMappingHelper.MaskIdNumber(p.IdNumberMasked),
            Phone: p.Phone,
            Email: p.Email,
            Address: address,
            Occupation: p.Occupation,
            Ethnicity: p.Ethnicity,
            AvatarUrl: p.AvatarUrl,
            ReceptionNote: p.ReceptionNote,
            BloodType: p.BloodType,
            AllergiesSummary: p.AllergiesSummary,
            BhytCardNo: null,
            BhytValidTo: null,
            Status: p.Status,
            CreatedAt: p.CreatedAt,
            UpdatedAt: p.UpdatedAt,
            IdCardIssuedDate: p.IdCardIssuedDate,
            IdCardIssuedPlace: p.IdCardIssuedPlace,
            Nationality: p.Nationality,
            PatientType: p.PatientType,
            MaritalStatus: p.MaritalStatus,
            VisitType: p.VisitType);
    }
}
