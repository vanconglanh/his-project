using FhirPatient = Hl7.Fhir.Model.Patient;
using DomainPatient = ProDiabHis.Domain.Entities.Patient;
using Hl7.Fhir.Model;

namespace ProDiabHis.Application.Fhir.Mappers;

/// <summary>
/// Maps internal Patient entity sang FHIR R4 Patient resource.
/// Profile: http://hl7.org/fhir/StructureDefinition/Patient
/// </summary>
public class PatientMapper : IFhirMapper<DomainPatient, FhirPatient>
{
    private const string SystemPid = "https://prodiab.vn/fhir/patient-id";
    private const string SystemCode = "https://prodiab.vn/fhir/patient-code";

    public FhirPatient Map(DomainPatient entity)
    {
        var fhir = new FhirPatient
        {
            Id = entity.Id.ToString(),
            Meta = new Meta
            {
                Profile = new[] { "http://hl7.org/fhir/StructureDefinition/Patient" }
            }
        };

        fhir.Identifier.Add(new Identifier(SystemPid, entity.Id.ToString()));
        fhir.Identifier.Add(new Identifier(SystemCode, entity.Code));

        fhir.Name.Add(new HumanName { Text = entity.FullName, Use = HumanName.NameUse.Official });

        fhir.Gender = entity.Gender switch
        {
            Domain.Entities.Gender.Male   => AdministrativeGender.Male,
            Domain.Entities.Gender.Female => AdministrativeGender.Female,
            _                             => AdministrativeGender.Other
        };

        if (entity.DateOfBirth.HasValue)
            fhir.BirthDate = entity.DateOfBirth.Value.ToString("yyyy-MM-dd");

        if (!string.IsNullOrEmpty(entity.Phone))
            fhir.Telecom.Add(new ContactPoint
            {
                System = ContactPoint.ContactPointSystem.Phone,
                Value = entity.Phone,
                Use = ContactPoint.ContactPointUse.Mobile
            });

        if (!string.IsNullOrEmpty(entity.Email))
            fhir.Telecom.Add(new ContactPoint
            {
                System = ContactPoint.ContactPointSystem.Email,
                Value = entity.Email
            });

        if (!string.IsNullOrEmpty(entity.Street))
            fhir.Address.Add(new Address
            {
                Text = entity.Street,
                City = entity.DistrictCode,
                District = entity.WardCode,
                State = entity.ProvinceCode,
                Country = "VN"
            });

        fhir.Active = entity.Status == Domain.Entities.PatientStatus.Active && entity.DeletedAt == null;

        return fhir;
    }
}
