using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.LabResults;
using ProDiabHis.Domain.Entities;
using Xunit;

namespace ProDiabHis.UnitTests.LabResults;

/// <summary>Unit tests cho LabResult EF Core handlers</summary>
public class LabResultHandlersTests
{
    private readonly FakeTenantProvider _tenant = new(1);
    private readonly ICurrentUser _user;
    private readonly IAuditService _audit;
    private readonly ILabResultFlagCalculator _flagCalc;

    public LabResultHandlersTests()
    {
        _user = Substitute.For<ICurrentUser>();
        _user.UserId.Returns(Guid.NewGuid());
        _audit = Substitute.For<IAuditService>();
        _flagCalc = Substitute.For<ILabResultFlagCalculator>();
        _flagCalc.Calculate(Arg.Any<decimal?>(), Arg.Any<decimal?>(), Arg.Any<decimal?>())
                 .Returns("NORMAL");
    }

    // ─── List ───
    [Fact]
    public async Task ListLabResults_MultiTenantFilter_OnlyReturnsTenantData()
    {
        using var db = TestDbContextFactory.Create(tenantId: 1);

        // Seed 1 kết quả tenant 1, 1 kết quả tenant 2
        db.LabResults.Add(new LabResult
        {
            TenantId = 1, TestCode = "GLU", TestName = "Glucose", Value = "100",
            LabOrderId = Guid.NewGuid().ToString(), PatientId = Guid.NewGuid().ToString(),
            EncounterId = Guid.NewGuid().ToString(), Status = "PRELIMINARY", Source = "MANUAL",
            Flag = "NORMAL", PerformedAt = DateTime.UtcNow
        });
        db.LabResults.Add(new LabResult
        {
            TenantId = 2, TestCode = "HBA1C", TestName = "HbA1c", Value = "7.5",
            LabOrderId = Guid.NewGuid().ToString(), PatientId = Guid.NewGuid().ToString(),
            EncounterId = Guid.NewGuid().ToString(), Status = "PRELIMINARY", Source = "MANUAL",
            Flag = "H", PerformedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var handler = new ListLabResultsQueryHandler(db);
        var result = await handler.Handle(
            new ListLabResultsQuery(null, null, null, null, null, null, null, 1, 20),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Item1.Should().HaveCount(1);
        result.Value.Item1[0].TestCode.Should().Be("GLU");
        result.Value.Item2.Should().Be(1);
    }

    // ─── Verify ───
    [Fact]
    public async Task VerifyLabResult_HappyPath_SetsStatusVerified()
    {
        using var db = TestDbContextFactory.Create(tenantId: 1);
        var labResultId = Guid.NewGuid();
        db.LabResults.Add(new LabResult
        {
            Id = labResultId, TenantId = 1, TestCode = "GLU", TestName = "Glucose",
            Value = "100", Status = "PRELIMINARY", Source = "MANUAL", Flag = "NORMAL",
            LabOrderId = Guid.NewGuid().ToString(), PatientId = Guid.NewGuid().ToString(),
            EncounterId = Guid.NewGuid().ToString(), PerformedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var handler = new VerifyLabResultCommandHandler(db, _user, _audit);
        var result = await handler.Handle(new VerifyLabResultCommand(labResultId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var entity = await db.LabResults.AsNoTracking().FirstAsync(e => e.Id == labResultId);
        entity.Status.Should().Be(LabResultStatus.Verified);
        entity.VerifiedAt.Should().NotBeNull();
        entity.VerifiedBy.Should().NotBeNullOrEmpty();
    }

    // ─── Verify already verified ───
    [Fact]
    public async Task VerifyLabResult_AlreadyVerified_ReturnsFailure()
    {
        using var db = TestDbContextFactory.Create(tenantId: 1);
        var labResultId = Guid.NewGuid();
        db.LabResults.Add(new LabResult
        {
            Id = labResultId, TenantId = 1, TestCode = "HBA1C", TestName = "HbA1c",
            Value = "7.5", Status = LabResultStatus.Verified, Source = "MANUAL", Flag = "H",
            LabOrderId = Guid.NewGuid().ToString(), PatientId = Guid.NewGuid().ToString(),
            EncounterId = Guid.NewGuid().ToString(), PerformedAt = DateTime.UtcNow,
            VerifiedAt = DateTime.UtcNow, VerifiedBy = Guid.NewGuid().ToString()
        });
        await db.SaveChangesAsync();

        var handler = new VerifyLabResultCommandHandler(db, _user, _audit);
        var result = await handler.Handle(new VerifyLabResultCommand(labResultId), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("LAB_RESULT_ALREADY_VERIFIED");
    }

    // ─── Unverify ───
    [Fact]
    public async Task UnverifyLabResult_WithinTimeout_SetsStatusDraft()
    {
        using var db = TestDbContextFactory.Create(tenantId: 1);
        var labResultId = Guid.NewGuid();
        db.LabResults.Add(new LabResult
        {
            Id = labResultId, TenantId = 1, TestCode = "GLU", TestName = "Glucose",
            Value = "120", Status = LabResultStatus.Verified, Source = "MANUAL", Flag = "NORMAL",
            LabOrderId = Guid.NewGuid().ToString(), PatientId = Guid.NewGuid().ToString(),
            EncounterId = Guid.NewGuid().ToString(), PerformedAt = DateTime.UtcNow,
            VerifiedAt = DateTime.UtcNow.AddMinutes(-5), VerifiedBy = Guid.NewGuid().ToString()
        });
        await db.SaveChangesAsync();

        var handler = new UnverifyLabResultCommandHandler(db, _user, _audit);
        var result = await handler.Handle(new UnverifyLabResultCommand(labResultId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var entity = await db.LabResults.AsNoTracking().FirstAsync(e => e.Id == labResultId);
        entity.Status.Should().Be(LabResultStatus.Draft);
        entity.VerifiedAt.Should().BeNull();
    }

    // ─── GetAbnormal ───
    [Fact]
    public async Task GetAbnormalLabResults_FiltersCriticalOnly()
    {
        using var db = TestDbContextFactory.Create(tenantId: 1);
        db.LabResults.AddRange(
            new LabResult
            {
                TenantId = 1, TestCode = "GLU", TestName = "Glu", Value = "500",
                Status = "VERIFIED", Source = "MANUAL", Flag = LabResultFlag.Critical,
                LabOrderId = Guid.NewGuid().ToString(), PatientId = Guid.NewGuid().ToString(),
                EncounterId = Guid.NewGuid().ToString(), PerformedAt = DateTime.UtcNow
            },
            new LabResult
            {
                TenantId = 1, TestCode = "HBA1C", TestName = "HbA1c", Value = "7.5",
                Status = "VERIFIED", Source = "MANUAL", Flag = LabResultFlag.H,
                LabOrderId = Guid.NewGuid().ToString(), PatientId = Guid.NewGuid().ToString(),
                EncounterId = Guid.NewGuid().ToString(), PerformedAt = DateTime.UtcNow
            });
        await db.SaveChangesAsync();

        var handler = new GetAbnormalLabResultsQueryHandler(db);
        var result = await handler.Handle(
            new GetAbnormalLabResultsQuery("CRITICAL_ONLY", null, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Should().HaveCount(1);
        result.Value[0].Flag.Should().Be(LabResultFlag.Critical);
    }

    // ─── HistoryTrend ───
    [Fact]
    public async Task GetLabResultHistoryTrend_ReturnsOrderedPoints()
    {
        using var db = TestDbContextFactory.Create(tenantId: 1);
        var patientId = Guid.NewGuid();
        db.LabResults.AddRange(
            new LabResult
            {
                TenantId = 1, PatientId = patientId.ToString(), TestCode = "GLU", TestName = "Glucose",
                Value = "100", ValueNumeric = 100m, Status = LabResultStatus.Verified,
                Flag = "NORMAL", Source = "MANUAL",
                LabOrderId = Guid.NewGuid().ToString(), EncounterId = Guid.NewGuid().ToString(),
                PerformedAt = DateTime.UtcNow.AddDays(-2)
            },
            new LabResult
            {
                TenantId = 1, PatientId = patientId.ToString(), TestCode = "GLU", TestName = "Glucose",
                Value = "120", ValueNumeric = 120m, Status = LabResultStatus.Verified,
                Flag = "H", Source = "MANUAL",
                LabOrderId = Guid.NewGuid().ToString(), EncounterId = Guid.NewGuid().ToString(),
                PerformedAt = DateTime.UtcNow.AddDays(-1)
            });
        await db.SaveChangesAsync();

        var handler = new GetLabResultHistoryTrendQueryHandler(db);
        var result = await handler.Handle(
            new GetLabResultHistoryTrendQuery(patientId, "GLU", null, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Points.Should().HaveCount(2);
        result.Value.Points[0].ValueNumeric.Should().Be(100m);
        result.Value.Points[1].ValueNumeric.Should().Be(120m);
    }
}
