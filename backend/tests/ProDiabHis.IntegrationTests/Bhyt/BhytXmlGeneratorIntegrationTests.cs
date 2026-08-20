using System.Data;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using MySqlConnector;
using ProDiabHis.Application.Bhyt;
using ProDiabHis.Application.Common;
using ProDiabHis.Domain.Entities;
using ProDiabHis.Domain.Entities.Pharmacy;
using ProDiabHis.Infrastructure.Bhyt;
using ProDiabHis.Infrastructure.Security;
using Xunit;
using BillingEntity = ProDiabHis.Domain.Entities.Billing;

namespace ProDiabHis.IntegrationTests.Bhyt;

/// <summary>
/// Chay that pipeline BhytXmlGeneratorImpl.GenerateAsync tren MySQL that (Testcontainers):
/// seed tenant + benh nhan + the BHYT + encounter + chan doan (1 PRIMARY + 2 SECONDARY)
/// + billing + don thuoc, roi assert du lieu Bang 1 (MA_BENH_CHINH / MA_BENH_KT) dung QD 3176.
///
/// LUU Y: GenerateAsync hien tai KHONG tu serialize ra file XML (chua co XDocument/XmlWriter
/// nao trong repo cho output BHYT) — no tra ve BhytExportItemData voi RowDataJson la JSON
/// cua tung dong Bang 1-5. Test nay vi vay assert tren JSON cua Bang 1, la du lieu goc se
/// duoc dung de sinh XML. BhytXsdValidatorImpl hien la placeholder (khong doc file XML that,
/// chi log OK khi thay file .xsd ton tai) nen KHONG the dung de validate schema that trong test
/// nay — khong bit XSD validation da chay that.
/// </summary>
[Collection("MySql")]
public class BhytXmlGeneratorIntegrationTests : IClassFixture<MySqlTestFixture>
{
    private readonly MySqlTestFixture _fixture;

    // Key test rieng, KHONG phai key that, chi dung trong pham vi test nay.
    private const string TestMasterKeyBase64 = "MTIzNDU2Nzg5MDEyMzQ1Njc4OTAxMjM0NTY3ODkwMTI=";

    public BhytXmlGeneratorIntegrationTests(MySqlTestFixture fixture)
    {
        _fixture = fixture;
    }

    private sealed class TestDapperConnectionFactory : IDapperConnectionFactory
    {
        private readonly string _cs;
        public TestDapperConnectionFactory(string cs) => _cs = cs;
        public IDbConnection CreateConnection() => new MySqlConnection(_cs);
    }

    private sealed class NoopAuditService : IAuditService
    {
        public Task LogAsync(string action, string? resourceType, string? resourceId,
            object? details = null, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task LogAsync(string action, string? resourceType, string? resourceId,
            AuditSeverity severity, bool crossTenantAttempt = false, string? requestId = null,
            object? details = null, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    /// <summary>IFileStorage khong lam gi - test nay chi goi ValidateXmlContent (khong qua DownloadAsync).</summary>
    private sealed class NoopFileStorage : IFileStorage
    {
        public Task<string> UploadAsync(string bucket, string objectKey, Stream stream, string contentType,
            CancellationToken cancellationToken = default) => Task.FromResult(objectKey);
        public Task<string> GetSignedUrlAsync(string bucket, string objectKey, int ttlSeconds = 900,
            CancellationToken cancellationToken = default) => Task.FromResult("");
        public Task DeleteAsync(string bucket, string objectKey, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
        public Task EnsureBucketExistsAsync(string bucket, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
        public Task<Stream> DownloadAsync(string bucket, string objectKey, CancellationToken cancellationToken = default)
            => Task.FromResult<Stream>(new MemoryStream());
    }

    [DockerAvailableFact]
    public async Task GenerateAsync_EncounterWithPrimaryAndSecondaryDiagnoses_ProducesCorrectMaBenh()
    {
        // ── Arrange: IEncryptionService that (AES-256-GCM) voi key test rieng ──
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Encryption:MasterKey"] = TestMasterKeyBase64
            })
            .Build();
        IEncryptionService encryption = new AesGcmEncryptor(config);

        var db = _fixture.DbContext;

        // ── Seed tenant ──
        var tenant = new Tenant
        {
            Code = "BHYTIT01",
            Name = "Phong kham test BHYT XML",
            Subdomain = $"bhyt-it-{Guid.NewGuid():N}"[..30]
        };
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();

        // ── Seed benh nhan ──
        var patient = new Patient
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Code = "BN-IT-001",
            FullName = "Nguyen Van Test",
            Gender = "MALE",
            DateOfBirth = new DateOnly(1980, 1, 15),
            Status = PatientStatus.Active
        };
        db.Patients.Add(patient);

        // ── Seed the BHYT (so the ma hoa AES-256-GCM) ──
        var insurance = new Insurance
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            PatientId = patient.Id.ToString(),
            Type = "BHYT",
            CardNoEnc = encryption.Encrypt("DN4079912345678"),
            CardNoMasked = "DN40****678",
            ValidFrom = new DateOnly(2026, 1, 1),
            ValidTo = new DateOnly(2026, 12, 31),
            HospitalCode = "04104",
            CoveragePercent = 80
        };
        db.Insurances.Add(insurance);

        // ── Seed encounter trong ky 2026-05 ──
        var encounter = new Encounter
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            PatientId = patient.Id.ToString(),
            Status = EncounterStatus.Done,
            StartedAt = new DateTime(2026, 5, 10, 8, 0, 0, DateTimeKind.Utc),
            FinishedAt = new DateTime(2026, 5, 10, 9, 0, 0, DateTimeKind.Utc)
        };
        db.Encounters.Add(encounter);

        // ── Chan doan: 1 PRIMARY + 2 SECONDARY ──
        db.EncounterDiagnoses.Add(new EncounterDiagnosis
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            EncounterId = encounter.Id.ToString(),
            Icd10Code = "E11.9",
            Name = "Dai thao duong type 2",
            Type = DiagnosisType.Primary
        });
        db.EncounterDiagnoses.Add(new EncounterDiagnosis
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            EncounterId = encounter.Id.ToString(),
            Icd10Code = "I10",
            Name = "Tang huyet ap vo can",
            Type = DiagnosisType.Secondary
        });
        db.EncounterDiagnoses.Add(new EncounterDiagnosis
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            EncounterId = encounter.Id.ToString(),
            Icd10Code = "E78.5",
            Name = "Roi loan chuyen hoa lipid",
            Type = DiagnosisType.Secondary
        });

        // ── Billing + billing item ──
        var billing = new BillingEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            PatientId = patient.Id,
            EncounterId = encounter.Id,
            Payer = "BHYT",
            Subtotal = 200000m,
            PatientPayable = 40000m,
            BhytAmount = 160000m,
            Balance = 0m,
            Status = BillingStatus.Finalized
        };
        db.Billings.Add(billing);
        db.BillingItems.Add(new BillingItem
        {
            Id = Guid.NewGuid(),
            BillingId = billing.Id,
            TenantId = tenant.Id,
            ItemType = "DRUG",
            Name = "Metformin 500mg",
            Quantity = 30,
            UnitPrice = 5000m,
            LineTotal = 150000m,
            BhytApplicable = true,
            BhytAmount = 120000m
        });
        db.BillingItems.Add(new BillingItem
        {
            Id = Guid.NewGuid(),
            BillingId = billing.Id,
            TenantId = tenant.Id,
            ItemType = "SERVICE",
            Code = "KB01",
            Name = "Kham benh",
            Quantity = 1,
            UnitPrice = 50000m,
            LineTotal = 50000m,
            BhytApplicable = true,
            BhytAmount = 40000m
        });

        // ── Don thuoc BHYT ──
        var drug = new Drug
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Code = "DR001",
            Name = "Metformin 500mg",
            Unit = "vien",
            SellPrice = 5000m
        };
        db.Drugs.Add(drug);

        var prescription = new Prescription
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            EncounterId = encounter.Id,
            PatientId = patient.Id,
            DoctorId = Guid.NewGuid(),
            Status = PrescriptionStatus.Signed,
            SignedAt = new DateTime(2026, 5, 10, 8, 30, 0, DateTimeKind.Utc)
        };
        db.Prescriptions.Add(prescription);
        db.PrescriptionItems.Add(new PrescriptionItem
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            PrescriptionId = prescription.Id,
            DrugId = drug.Id,
            DrugName = drug.Name,
            Quantity = 30,
            Unit = "vien",
            Dosage = "1 vien x 2 lan/ngay",
            Route = "uong",
            UnitPrice = 5000m,
            LineTotal = 150000m,
            BhytApplicable = true
        });

        await db.SaveChangesAsync();

        // ── Act: chay that pipeline sinh du lieu Bang 1-5 tren MySQL container ──
        var generator = new BhytXmlGeneratorImpl(
            new TestDapperConnectionFactory(_fixture.ConnectionString),
            NullLogger<BhytXmlGeneratorImpl>.Instance,
            encryption,
            new NoopAuditService());

        var result = await generator.GenerateAsync(
            exportId: 1, tenantId: tenant.Id, periodMonth: "2026-05",
            scopeFilterJson: null, ct: CancellationToken.None);

        // ── Assert ──
        result.Success.Should().BeTrue(result.ErrorMessage);
        result.EncounterCount.Should().Be(1);

        var table1Item = result.Items.Should().ContainSingle(i => i.TableNo == 1).Subject;
        var table1Json = JsonDocument.Parse(table1Item.RowDataJson).RootElement;

        table1Json.GetProperty("MA_BENH_CHINH").GetString().Should().Be("E11.9");
        var maBenhKhac = table1Json.GetProperty("MA_BENH_KT").GetString();
        maBenhKhac.Should().NotBeNullOrEmpty();
        maBenhKhac.Should().Contain("I10");
        maBenhKhac.Should().Contain("E78.5");
        maBenhKhac.Should().NotContain("E11.9");

        // Table 2 (thuoc BHYT) phai co dong thuoc vua ke, dung ma_lien_ket voi Bang 1
        var table2Item = result.Items.Should().ContainSingle(i => i.TableNo == 2).Subject;
        table2Item.MaLienKet.Should().Be(table1Item.MaLienKet);
        var table2Json = JsonDocument.Parse(table2Item.RowDataJson).RootElement;
        table2Json.GetProperty("TenThuoc").GetString().Should().Be("Metformin 500mg");

        // ── Act 2: serialize XML that (khong con la JSON per-row) va validate voi XSD that ──
        var serializer = new ProDiabHis.Infrastructure.Bhyt.BhytXmlSerializerImpl();
        var xml = serializer.Serialize(exportId: 1, tenantCode: tenant.Code, periodMonth: "2026-05", result.Items);

        xml.Should().Contain("<GIAMDINHHS").And.Contain("<Bang1>").And.Contain("<Bang2>");

        var validator = new ProDiabHis.Infrastructure.Bhyt.BhytXsdValidatorImpl(
            NullLogger<ProDiabHis.Infrastructure.Bhyt.BhytXsdValidatorImpl>.Instance,
            new TestDapperConnectionFactory(_fixture.ConnectionString),
            new NoopFileStorage());

        var xsdResult = validator.ValidateXmlContent(xml);
        xsdResult.Valid.Should().BeTrue(string.Join("; ", xsdResult.Errors.Select(e => $"{e.Field}: {e.Message}")));
    }

    [DockerAvailableFact]
    public async Task GenerateAsync_NoEncountersInPeriod_ReturnsFailureNoEncounters()
    {
        // Edge case: ky khong co encounter nao -> tra ve that bai co ma loi ro rang,
        // KHONG duoc nem exception hay tra ve thanh cong voi danh sach rong.
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Encryption:MasterKey"] = TestMasterKeyBase64
            })
            .Build();
        IEncryptionService encryption = new AesGcmEncryptor(config);

        var db = _fixture.DbContext;
        var tenant = new Tenant
        {
            Code = "BHYTIT02",
            Name = "Phong kham test BHYT XML rong",
            Subdomain = $"bhyt-it-empty-{Guid.NewGuid():N}"[..30]
        };
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();

        var generator = new BhytXmlGeneratorImpl(
            new TestDapperConnectionFactory(_fixture.ConnectionString),
            NullLogger<BhytXmlGeneratorImpl>.Instance,
            encryption,
            new NoopAuditService());

        var result = await generator.GenerateAsync(
            exportId: 2, tenantId: tenant.Id, periodMonth: "2099-01",
            scopeFilterJson: null, ct: CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("BHYT_EXPORT_NO_ENCOUNTERS");
        result.Items.Should().BeEmpty();
    }
}
