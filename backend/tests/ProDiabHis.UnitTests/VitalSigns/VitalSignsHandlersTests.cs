using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.VitalSigns;
using ProDiabHis.Domain.Entities;
using Xunit;

namespace ProDiabHis.UnitTests.VitalSigns;

/// <summary>Unit tests cho VitalSigns EF Core handlers</summary>
public class VitalSignsHandlersTests
{
    private readonly FakeTenantProvider _tenant = new(1);
    private readonly ICurrentUser _user;
    private readonly IAuditService _audit;
    private readonly Guid _userId = Guid.NewGuid();

    public VitalSignsHandlersTests()
    {
        _user = Substitute.For<ICurrentUser>();
        _user.UserId.Returns(_userId);
        _audit = Substitute.For<IAuditService>();
    }

    // ─── Create VitalSigns ───
    [Fact]
    public async Task CreateVitalSigns_HappyPath_CreatesRecord()
    {
        using var db = TestDbContextFactory.Create(tenantId: 1);
        var encounterId = Guid.NewGuid();
        db.Encounters.Add(new Encounter
        {
            Id = encounterId, TenantId = 1,
            PatientId = Guid.NewGuid().ToString(), Status = "IN_PROGRESS"
        });
        await db.SaveChangesAsync();

        var handler = new CreateVitalSignsCommandHandler(db, _tenant, _user, _audit);
        var req = new VitalSignsRequest(
            RecordedAt: null,
            TemperatureC: 37.5m,
            HeartRateBpm: 80,
            RespiratoryRate: 16,
            BpSystolic: 120, BpDiastolic: 80,
            Spo2Percent: 98,
            WeightKg: 65m, HeightCm: 170m,
            PainScale: 0, GlucoseMgDl: null, Note: "Bình thường");

        var result = await handler.Handle(new CreateVitalSignsCommand(encounterId, req), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.EncounterId.Should().Be(encounterId);
        result.Value.TemperatureC.Should().Be(37.5m);
        result.Value.BpSystolic.Should().Be(120);
        result.Value.RecordSequence.Should().Be(1);
        result.Value.Bmi.Should().BeApproximately(22.5m, 0.5m);

        var count = await db.VitalSigns.CountAsync();
        count.Should().Be(1);
    }

    // ─── Validation ───
    [Fact]
    public async Task CreateVitalSigns_InvalidTemperature_ReturnsFailure()
    {
        using var db = TestDbContextFactory.Create(tenantId: 1);
        var encounterId = Guid.NewGuid();
        db.Encounters.Add(new Encounter
        {
            Id = encounterId, TenantId = 1,
            PatientId = Guid.NewGuid().ToString(), Status = "IN_PROGRESS"
        });
        await db.SaveChangesAsync();

        var handler = new CreateVitalSignsCommandHandler(db, _tenant, _user, _audit);
        var req = new VitalSignsRequest(null, TemperatureC: 20m, null, null, null, null, null, null, null, null, null, null);

        var result = await handler.Handle(new CreateVitalSignsCommand(encounterId, req), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("VITAL_INVALID_RANGE");
    }

    // ─── Create — encounter not found ───
    [Fact]
    public async Task CreateVitalSigns_EncounterNotFound_ReturnsFailure()
    {
        using var db = TestDbContextFactory.Create(tenantId: 1);
        var handler = new CreateVitalSignsCommandHandler(db, _tenant, _user, _audit);
        var req = new VitalSignsRequest(null, 37m, 80, 16, 120, 80, 98, null, null, null, null, null);

        var result = await handler.Handle(new CreateVitalSignsCommand(Guid.NewGuid(), req), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("ENCOUNTER_NOT_FOUND");
    }

    // ─── Multi-tenant filter ───
    [Fact]
    public async Task ListVitalSigns_TenantFilter_OnlyReturnsTenantData()
    {
        using var db = TestDbContextFactory.Create(tenantId: 1);
        var encounterId = Guid.NewGuid();
        var encIdStr    = encounterId.ToString();

        db.VitalSigns.Add(new Domain.Entities.VitalSigns
        {
            TenantId = 1, EncounterId = encIdStr,
            PatientId = Guid.NewGuid().ToString(),
            RecordedAt = DateTime.UtcNow, RecordSequence = 1
        });
        db.VitalSigns.Add(new Domain.Entities.VitalSigns
        {
            TenantId = 2, EncounterId = encIdStr,
            PatientId = Guid.NewGuid().ToString(),
            RecordedAt = DateTime.UtcNow, RecordSequence = 1
        });
        await db.SaveChangesAsync();

        var handler = new ListVitalSignsByEncounterQueryHandler(db);
        var result = await handler.Handle(
            new ListVitalSignsByEncounterQuery(encounterId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Should().HaveCount(1);
    }

    // ─── Delete — timeout ───
    [Fact]
    public async Task DeleteVitalSigns_AfterTimeout_ReturnsFailure()
    {
        using var db = TestDbContextFactory.Create(tenantId: 1);
        var vsId = Guid.NewGuid();
        var vsEntity = new Domain.Entities.VitalSigns
        {
            Id = vsId, TenantId = 1,
            EncounterId = Guid.NewGuid().ToString(),
            PatientId = Guid.NewGuid().ToString(),
            RecordedBy = _userId.ToString(),
            RecordedAt = DateTime.UtcNow.AddDays(-2),
            RecordSequence = 1
        };
        db.VitalSigns.Add(vsEntity);
        await db.SaveChangesAsync();

        // Manually force CreatedAt to > 24h ago (bypass SaveChangesAsync override)
        db.Database.EnsureCreated();
        var entry = db.Entry(vsEntity);
        entry.Property(e => e.CreatedAt).CurrentValue = DateTime.UtcNow.AddHours(-25);
        await db.SaveChangesAsync();

        var handler = new DeleteVitalSignsCommandHandler(db, _user);
        var result = await handler.Handle(new DeleteVitalSignsCommand(vsId), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("VITAL_EDIT_TIMEOUT");
    }

    // ─── GetLatest ───
    [Fact]
    public async Task GetLatestVitalSigns_ReturnsLatestByRecordedAt()
    {
        using var db = TestDbContextFactory.Create(tenantId: 1);
        var encounterId = Guid.NewGuid();
        var encIdStr    = encounterId.ToString();
        var patId       = Guid.NewGuid().ToString();

        db.VitalSigns.AddRange(
            new Domain.Entities.VitalSigns
            {
                TenantId = 1, EncounterId = encIdStr, PatientId = patId,
                RecordedAt = DateTime.UtcNow.AddHours(-2), RecordSequence = 1,
                HeartRateBpm = 72
            },
            new Domain.Entities.VitalSigns
            {
                TenantId = 1, EncounterId = encIdStr, PatientId = patId,
                RecordedAt = DateTime.UtcNow.AddHours(-1), RecordSequence = 2,
                HeartRateBpm = 85
            });
        await db.SaveChangesAsync();

        var handler = new GetLatestVitalSignsQueryHandler(db);
        var result = await handler.Handle(new GetLatestVitalSignsQuery(encounterId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.HeartRateBpm.Should().Be(85);
    }
}
