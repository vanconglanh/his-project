using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.LabPartners;
using ProDiabHis.Domain.Entities;
using Xunit;

namespace ProDiabHis.UnitTests.LabResults;

/// <summary>Unit tests cho LabPartner EF Core handlers</summary>
public class LabPartnerHandlersTests
{
    private readonly FakeTenantProvider _tenant = new(1);
    private readonly ICurrentUser _user;
    private readonly IEncryptionService _enc;

    public LabPartnerHandlersTests()
    {
        _user = Substitute.For<ICurrentUser>();
        _user.UserId.Returns(Guid.NewGuid());
        _enc = Substitute.For<IEncryptionService>();
        _enc.Encrypt(Arg.Any<string>()).Returns(ci => "ENC:" + ci.Arg<string>());
        _enc.Decrypt(Arg.Any<string>()).Returns(ci => ci.Arg<string>().Replace("ENC:", ""));
    }

    // ─── List ───
    [Fact]
    public async Task ListLabPartners_TenantFilter_OnlyReturnsTenantData()
    {
        using var db = TestDbContextFactory.Create(tenantId: 1);
        db.LabPartners.Add(new LabPartner
        {
            TenantId = 1, Code = "LP001", Name = "Diag Lab A",
            EndpointUrl = "https://diag-a.com", Transport = "REST", Status = "ACTIVE"
        });
        db.LabPartners.Add(new LabPartner
        {
            TenantId = 2, Code = "LP002", Name = "Diag Lab B",
            EndpointUrl = "https://diag-b.com", Transport = "REST", Status = "ACTIVE"
        });
        await db.SaveChangesAsync();

        var handler = new ListLabPartnersQueryHandler(db);
        var result = await handler.Handle(new ListLabPartnersQuery(null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Should().HaveCount(1);
        result.Value[0].Code.Should().Be("LP001");
    }

    // ─── Create ───
    [Fact]
    public async Task CreateLabPartner_HappyPath_CreatesWithEncryptedKey()
    {
        using var db = TestDbContextFactory.Create(tenantId: 1);
        var handler = new CreateLabPartnerCommandHandler(db, _tenant, _user, _enc);

        var req = new LabPartnerCreateRequest(
            Code: "LP003", Name: "Test Lab",
            EndpointUrl: "https://test-lab.com", AuthType: "API_KEY",
            ApiKey: "secret-key", BearerToken: null,
            Transport: "REST", SupportedTests: new List<string> { "GLU", "HBA1C" },
            ContactEmail: "lab@test.com", ContactPhone: null);

        var result = await handler.Handle(new CreateLabPartnerCommand(req), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Code.Should().Be("LP003");
        result.Value.ApiKeyMasked.Should().NotBeNullOrEmpty();
        result.Value.SupportedTests.Should().Contain("GLU");

        var entity = await db.LabPartners.AsNoTracking().FirstAsync(e => e.Code == "LP003");
        entity.ApiKeyEncrypted.Should().NotBeNull();
        entity.TenantId.Should().Be(1);
    }

    // ─── Get not found ───
    [Fact]
    public async Task GetLabPartner_NotFound_ReturnsFailure()
    {
        using var db = TestDbContextFactory.Create(tenantId: 1);
        var handler = new GetLabPartnerQueryHandler(db);

        var result = await handler.Handle(new GetLabPartnerQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("LAB_PARTNER_NOT_FOUND");
    }

    // ─── Delete ───
    [Fact]
    public async Task DeleteLabPartner_SoftDelete_SetsDeletedAt()
    {
        using var db = TestDbContextFactory.Create(tenantId: 1);
        var partnerId = Guid.NewGuid();
        db.LabPartners.Add(new LabPartner
        {
            Id = partnerId, TenantId = 1, Code = "LP_DEL",
            Name = "Delete Me", EndpointUrl = "https://del.com",
            Transport = "REST", Status = "INACTIVE"
        });
        await db.SaveChangesAsync();

        var handler = new DeleteLabPartnerCommandHandler(db, _user);
        var result = await handler.Handle(new DeleteLabPartnerCommand(partnerId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        // Should be soft-deleted (not visible via global filter)
        var visible = await db.LabPartners.AsNoTracking().AnyAsync(e => e.Id == partnerId);
        visible.Should().BeFalse(); // deleted_at set, filtered out
    }

    // ─── Update not found ───
    [Fact]
    public async Task UpdateLabPartner_NotFound_ReturnsFailure()
    {
        using var db = TestDbContextFactory.Create(tenantId: 1);
        var handler = new UpdateLabPartnerCommandHandler(db, _user);

        var result = await handler.Handle(
            new UpdateLabPartnerCommand(Guid.NewGuid(),
                new LabPartnerUpdateRequest("New Name", null, null, null, null, null, null)),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("LAB_PARTNER_NOT_FOUND");
    }
}
