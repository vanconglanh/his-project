using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.EMR;
using ProDiabHis.Domain.Entities;
using Xunit;

namespace ProDiabHis.UnitTests.EMR;

/// <summary>Unit tests cho EMR EF Core handlers</summary>
public class EmrHandlersTests
{
    private readonly FakeTenantProvider _tenant = new(1);
    private readonly ICurrentUser _user;
    private readonly IAuditService _audit;

    public EmrHandlersTests()
    {
        _user = Substitute.For<ICurrentUser>();
        _user.UserId.Returns(Guid.NewGuid());
        _audit = Substitute.For<IAuditService>();
    }

    // ─── Get EMR null khi chưa có ───
    [Fact]
    public async Task GetEmr_NoDraft_ReturnsNull()
    {
        using var db = TestDbContextFactory.Create(tenantId: 1);
        var handler = new GetEmrQueryHandler(db);
        var result = await handler.Handle(new GetEmrQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    // ─── Save Draft tạo mới ───
    [Fact]
    public async Task SaveEmrDraft_NewEncounter_CreatesDraft()
    {
        using var db = TestDbContextFactory.Create(tenantId: 1);
        var encounterId = Guid.NewGuid();
        db.Encounters.Add(new Encounter
        {
            Id = encounterId, TenantId = 1,
            PatientId = Guid.NewGuid().ToString(), Status = "IN_PROGRESS"
        });
        await db.SaveChangesAsync();

        var handler = new SaveEmrDraftCommandHandler(db, _tenant, _user, _audit);
        var req = new EmrSaveRequest(
            ContentJson: new { chief_complaint = "Đau bụng", note = "Khám tổng quát" },
            ContentHtml: "<p>Đau bụng</p>",
            TemplateId: null);

        var result = await handler.Handle(new SaveEmrDraftCommand(encounterId, req), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.EncounterId.Should().Be(encounterId);
        result.Value.Version.Should().Be(1);

        var emrCount = await db.EmrContents.CountAsync();
        emrCount.Should().Be(1);

        var versionCount = await db.EmrVersions.CountAsync();
        versionCount.Should().Be(1);
    }

    // ─── Save Draft tăng version ───
    [Fact]
    public async Task SaveEmrDraft_ExistingDraft_IncrementsVersion()
    {
        using var db = TestDbContextFactory.Create(tenantId: 1);
        var encounterId = Guid.NewGuid();
        var encIdStr    = encounterId.ToString();
        db.Encounters.Add(new Encounter
        {
            Id = encounterId, TenantId = 1,
            PatientId = Guid.NewGuid().ToString(), Status = "IN_PROGRESS"
        });
        db.EmrContents.Add(new EmrContent
        {
            TenantId = 1, EncounterId = encIdStr,
            ContentJson = "{}", Version = 2
        });
        await db.SaveChangesAsync();

        var handler = new SaveEmrDraftCommandHandler(db, _tenant, _user, _audit);
        var req = new EmrSaveRequest(
            ContentJson: new { note = "Updated" }, ContentHtml: null, TemplateId: null);

        var result = await handler.Handle(new SaveEmrDraftCommand(encounterId, req), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Version.Should().Be(3);
    }

    // ─── Save Draft lỗi nếu encounter không tồn tại ───
    [Fact]
    public async Task SaveEmrDraft_EncounterNotFound_ReturnsFailure()
    {
        using var db = TestDbContextFactory.Create(tenantId: 1);
        var handler = new SaveEmrDraftCommandHandler(db, _tenant, _user, _audit);
        var req = new EmrSaveRequest(new { }, null, null);

        var result = await handler.Handle(new SaveEmrDraftCommand(Guid.NewGuid(), req), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("ENCOUNTER_NOT_FOUND");
    }

    // ─── Save Draft lỗi nếu EMR đã ký ───
    [Fact]
    public async Task SaveEmrDraft_AlreadySigned_ReturnsFailure()
    {
        using var db = TestDbContextFactory.Create(tenantId: 1);
        var encounterId = Guid.NewGuid();
        var encIdStr    = encounterId.ToString();
        db.Encounters.Add(new Encounter
        {
            Id = encounterId, TenantId = 1,
            PatientId = Guid.NewGuid().ToString(), Status = "COMPLETED"
        });
        db.EmrContents.Add(new EmrContent
        {
            TenantId = 1, EncounterId = encIdStr,
            ContentJson = "{}", Version = 1,
            SignedAt = DateTime.UtcNow, SignedBy = Guid.NewGuid().ToString()
        });
        await db.SaveChangesAsync();

        var handler = new SaveEmrDraftCommandHandler(db, _tenant, _user, _audit);
        var req = new EmrSaveRequest(new { }, null, null);

        var result = await handler.Handle(new SaveEmrDraftCommand(encounterId, req), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("EMR_ALREADY_SIGNED");
    }

    // ─── Template CRUD ───
    [Fact]
    public async Task CreateEmrTemplate_HappyPath_CreatesTemplate()
    {
        using var db = TestDbContextFactory.Create(tenantId: 1);
        var handler = new CreateEmrTemplateCommandHandler(db, _tenant, _user);
        var req = new EmrTemplateRequest(
            Name: "Mẫu khám nội tổng quát",
            ContentJson: new { sections = new[] { "Anamnesis", "Physical Exam" } },
            Speciality: "INTERNAL");

        var result = await handler.Handle(new CreateEmrTemplateCommand(req), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Mẫu khám nội tổng quát");
        result.Value.IsSystem.Should().BeFalse();
        result.Value.TenantId.Should().Be(1);

        var count = await db.EmrTemplates.CountAsync();
        count.Should().Be(1);
    }

    [Fact]
    public async Task DeleteEmrTemplate_SystemTemplate_ReturnsFailure()
    {
        using var db = TestDbContextFactory.Create(tenantId: 1);
        var tplId = Guid.NewGuid();
        db.EmrTemplates.Add(new EmrTemplate
        {
            Id = tplId, TenantId = null, Name = "Mẫu hệ thống",
            ContentJson = "{}", Speciality = "GENERAL", IsSystem = true
        });
        await db.SaveChangesAsync();

        var handler = new DeleteEmrTemplateCommandHandler(db, _user);
        var result = await handler.Handle(new DeleteEmrTemplateCommand(tplId), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("TEMPLATE_SYSTEM");
    }
}
