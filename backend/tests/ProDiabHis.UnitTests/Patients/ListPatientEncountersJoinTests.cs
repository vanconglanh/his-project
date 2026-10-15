using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using ProDiabHis.Application.Patients;
using ProDiabHis.Domain.Entities;
using Xunit;

namespace ProDiabHis.UnitTests.Patients;

/// <summary>
/// FIX 2026-10-16 (A2): ListPatientEncountersQueryHandler (GET /patients/{id}/encounters) truoc day
/// LUON tra DoctorName = null (hard-code) -> danh sach luot kham cua benh nhan khong hien ten bac si.
/// Sau fix: batch-load ten bac si theo doctor_id. doctor_id rong (legacy) -> van null (FE hien
/// "Chua ghi nhan"). RoomName lay qua Dapper (bang diab_his_sys_rooms) - test dung factory rong nen null.
/// </summary>
public class ListPatientEncountersJoinTests
{
    [Fact]
    public async Task DoctorName_DuocResolve_TheoDoctorId()
    {
        using var db = TestDbContextFactory.Create(tenantId: 1);

        var doctor = new User { TenantId = 1, FullName = "BS. Nguyễn Văn A", Email = "bs.a@clinic.vn" };
        db.Users.Add(doctor);

        var patientId = Guid.NewGuid();
        db.Encounters.Add(new Encounter
        {
            TenantId = 1,
            PatientId = patientId.ToString(),
            DoctorId = doctor.Id.ToString(),
            EncounterType = EncounterTypes.FirstVisit,
            Status = EncounterStatus.Done,
            CreatedAt = DateTime.UtcNow
        });
        db.SaveChanges();

        var handler = new ListPatientEncountersQueryHandler(db, new FakeEmptyDapperConnectionFactory());
        var result = await handler.Handle(new ListPatientEncountersQuery(patientId, 1, 20), CancellationToken.None);

        result.Total.Should().Be(1);
        result.Items[0].DoctorName.Should().Be("BS. Nguyễn Văn A");
    }

    [Fact]
    public async Task DoctorIdRong_DoctorName_Null_KhongLoi()
    {
        using var db = TestDbContextFactory.Create(tenantId: 1);

        var patientId = Guid.NewGuid();
        db.Encounters.Add(new Encounter
        {
            TenantId = 1,
            PatientId = patientId.ToString(),
            DoctorId = "",  // du lieu legacy: doctor_id rong
            EncounterType = EncounterTypes.FirstVisit,
            Status = EncounterStatus.Done,
            CreatedAt = DateTime.UtcNow
        });
        db.SaveChanges();

        var handler = new ListPatientEncountersQueryHandler(db, new FakeEmptyDapperConnectionFactory());
        var result = await handler.Handle(new ListPatientEncountersQuery(patientId, 1, 20), CancellationToken.None);

        result.Total.Should().Be(1);
        result.Items[0].DoctorName.Should().BeNull();
    }
}
