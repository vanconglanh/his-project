using DomainEncounter = ProDiabHis.Domain.Entities.Encounter;
using FhirEncounter = Hl7.Fhir.Model.Encounter;
using FluentAssertions;
using ProDiabHis.Application.Fhir.Mappers;
using ProDiabHis.Domain.Entities;
using Xunit;

namespace ProDiabHis.UnitTests.Fhir;

public class FhirEncounterMapperTests
{
    private readonly EncounterMapper _mapper = new();

    [Fact]
    public void Map_InProgressEncounter_StatusInProgress()
    {
        var encounter = new DomainEncounter
        {
            Id = Guid.NewGuid(),
            PatientId = Guid.NewGuid().ToString(),
            DoctorId = Guid.NewGuid().ToString(),
            Status = EncounterStatus.InProgress,
            EncounterType = EncounterTypes.FirstVisit,
            ReasonForVisit = "Kham tieu duong dinh ky",
            StartedAt = DateTime.UtcNow.AddHours(-1),
            TenantId = 1
        };

        var fhir = _mapper.Map(encounter);

        fhir.Should().NotBeNull();
        fhir.Id.Should().Be(encounter.Id.ToString());
        fhir.Status.Should().Be(FhirEncounter.EncounterStatus.InProgress);
        fhir.Class.Code.Should().Be("AMB");
        fhir.Subject!.Reference.Should().Contain(encounter.PatientId);
        fhir.Participant.Should().HaveCount(1);
        fhir.Participant[0].Individual!.Reference.Should().Contain(encounter.DoctorId);
        fhir.ReasonCode.Should().HaveCount(1);
        fhir.ReasonCode[0].Text.Should().Be("Kham tieu duong dinh ky");
        fhir.Period.Should().NotBeNull();
        fhir.Meta!.Profile.Should().Contain("http://hl7.org/fhir/StructureDefinition/Encounter");
    }

    [Fact]
    public void Map_DoneEncounter_StatusFinished()
    {
        var encounter = new DomainEncounter
        {
            Id = Guid.NewGuid(),
            PatientId = Guid.NewGuid().ToString(),
            Status = EncounterStatus.Done,
            EncounterType = EncounterTypes.FollowUp,
            StartedAt = DateTime.UtcNow.AddHours(-2),
            FinishedAt = DateTime.UtcNow.AddMinutes(-30),
            TenantId = 1
        };

        var fhir = _mapper.Map(encounter);

        fhir.Status.Should().Be(FhirEncounter.EncounterStatus.Finished);
        fhir.Period!.End.Should().NotBeNull();
    }

    [Fact]
    public void Map_CancelledEncounter_StatusCancelled()
    {
        var encounter = new DomainEncounter
        {
            Id = Guid.NewGuid(),
            PatientId = Guid.NewGuid().ToString(),
            Status = EncounterStatus.Cancelled,
            EncounterType = EncounterTypes.FirstVisit,
            TenantId = 1
        };

        var fhir = _mapper.Map(encounter);

        fhir.Status.Should().Be(FhirEncounter.EncounterStatus.Cancelled);
    }

    [Fact]
    public void Map_EncounterWithoutDoctor_ParticipantEmpty()
    {
        var encounter = new DomainEncounter
        {
            Id = Guid.NewGuid(),
            PatientId = Guid.NewGuid().ToString(),
            DoctorId = null,
            Status = EncounterStatus.Waiting,
            EncounterType = EncounterTypes.FirstVisit,
            TenantId = 1
        };

        var fhir = _mapper.Map(encounter);

        fhir.Participant.Should().BeEmpty();
    }
}
