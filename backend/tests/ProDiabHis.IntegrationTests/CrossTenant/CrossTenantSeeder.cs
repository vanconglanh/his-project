using Microsoft.EntityFrameworkCore;
using ProDiabHis.Application.Common;
using ProDiabHis.Domain.Entities;
using ProDiabHis.Domain.Entities.Pharmacy;
using ProDiabHis.Infrastructure.Persistence;

namespace ProDiabHis.IntegrationTests.CrossTenant;

/// <summary>
/// Bo dinh danh + du lieu 2 tenant dung cho toan bo test cross-tenant.
/// Tenant A = 1, Tenant B = 2. Moi tenant co day du: tenant row + user + patient +
/// encounter + billing + prescription + lab result + drug + branch.
/// GUID/ID cua tung ban ghi duoc ghi cung (deterministic) de test goi GET /{id} chinh xac.
/// </summary>
public static class CrossTenantIds
{
    public const int TenantA = 1;
    public const int TenantB = 2;

    // ── Tenant A ──────────────────────────────────────────────────────────
    public static readonly Guid UserA      = Guid.Parse("aaaa0001-0000-0000-0000-000000000001");
    public static readonly Guid PatientA   = Guid.Parse("aaaa0002-0000-0000-0000-000000000001");
    public static readonly Guid EncounterA = Guid.Parse("aaaa0003-0000-0000-0000-000000000001");
    public static readonly Guid BillingA   = Guid.Parse("aaaa0004-0000-0000-0000-000000000001");
    public static readonly Guid PrescA     = Guid.Parse("aaaa0005-0000-0000-0000-000000000001");
    public static readonly Guid LabA       = Guid.Parse("aaaa0006-0000-0000-0000-000000000001");
    public static readonly Guid DrugA      = Guid.Parse("aaaa0007-0000-0000-0000-000000000001");
    public const int BranchA = 1;

    // ── Tenant B (muc tieu ma tenant A KHONG duoc thay) ───────────────────
    public static readonly Guid UserB      = Guid.Parse("bbbb0001-0000-0000-0000-000000000002");
    public static readonly Guid PatientB   = Guid.Parse("bbbb0002-0000-0000-0000-000000000002");
    public static readonly Guid EncounterB = Guid.Parse("bbbb0003-0000-0000-0000-000000000002");
    public static readonly Guid BillingB   = Guid.Parse("bbbb0004-0000-0000-0000-000000000002");
    public static readonly Guid PrescB     = Guid.Parse("bbbb0005-0000-0000-0000-000000000002");
    public static readonly Guid LabB       = Guid.Parse("bbbb0006-0000-0000-0000-000000000002");
    public static readonly Guid DrugB      = Guid.Parse("bbbb0007-0000-0000-0000-000000000002");
    public const int BranchB = 2;

    // Chuoi ky tu dai dien tenant B — dung de assert body list KHONG chua.
    public const string PatientBCode   = "CTBPAT-TENANT-B-0002";
    public const string DrugBCode      = "CTBDRUG-TENANT-B-0002";
    public const string BranchBCode    = "CTB-BRANCH-B-0002";
}

/// <summary>ITenantProvider co dinh — dung xac minh EF Global Query Filter theo tung tenant.</summary>
public sealed class FixedTenantProvider : ITenantProvider
{
    public FixedTenantProvider(int tenantId) => TenantId = tenantId;
    public int TenantId { get; private set; }
    public void SetTenantId(int tenantId) => TenantId = tenantId;
}

/// <summary>BranchProvider bo qua filter chi nhanh — chi kiem tra tenant isolation.</summary>
public sealed class IgnoreBranchProvider : IBranchProvider
{
    public int BranchId => 0;
    public bool IgnoreBranchFilter => true;
    public IReadOnlyList<int> AllowedBranchIds => Array.Empty<int>();
    public void SetContext(int branchId, bool ignoreFilter, IReadOnlyList<int> allowedBranchIds) { }
}

public static class CrossTenantSeeder
{
    private static bool _seeded;
    private static readonly object _lock = new();

    /// <summary>DbContext scoped theo 1 tenant cu the (de test query filter).</summary>
    public static AppDbContext ContextForTenant(string connectionString, int tenantId)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySql(connectionString, ServerVersion.AutoDetect(connectionString))
            .Options;
        return new AppDbContext(options, new FixedTenantProvider(tenantId), new IgnoreBranchProvider());
    }

    /// <summary>
    /// Seed 2 tenant (idempotent — chi chay 1 lan cho ca test run). Dung DbContext KHONG loc tenant
    /// (fixture.NewDbContext -> NoopTenantProvider TenantId=0) nen ghi duoc ca 2 tenant.
    /// </summary>
    public static void EnsureSeeded(Func<AppDbContext> newDbContext)
    {
        if (_seeded) return;
        lock (_lock)
        {
            if (_seeded) return;
            using var db = newDbContext();

            PatchSchema(db);

            SeedTenant(db, CrossTenantIds.TenantA);
            SeedTenant(db, CrossTenantIds.TenantB);

            SeedBranch(db, CrossTenantIds.TenantA, CrossTenantIds.BranchA, "CTA-BRANCH-A-0001");
            SeedBranch(db, CrossTenantIds.TenantB, CrossTenantIds.BranchB, CrossTenantIds.BranchBCode);

            SeedUser(db, CrossTenantIds.TenantA, CrossTenantIds.UserA, "userA@cta.test");
            SeedUser(db, CrossTenantIds.TenantB, CrossTenantIds.UserB, "userB@ctb.test");

            SeedPatient(db, CrossTenantIds.TenantA, CrossTenantIds.PatientA, "CTAPAT-TENANT-A-0001", "Benh nhan Tenant A");
            SeedPatient(db, CrossTenantIds.TenantB, CrossTenantIds.PatientB, CrossTenantIds.PatientBCode, "Benh nhan Tenant B");

            SeedEncounter(db, CrossTenantIds.TenantA, CrossTenantIds.EncounterA, CrossTenantIds.PatientA);
            SeedEncounter(db, CrossTenantIds.TenantB, CrossTenantIds.EncounterB, CrossTenantIds.PatientB);

            SeedBilling(db, CrossTenantIds.TenantA, CrossTenantIds.BillingA, CrossTenantIds.PatientA, CrossTenantIds.EncounterA);
            SeedBilling(db, CrossTenantIds.TenantB, CrossTenantIds.BillingB, CrossTenantIds.PatientB, CrossTenantIds.EncounterB);

            SeedPrescription(db, CrossTenantIds.TenantA, CrossTenantIds.PrescA, CrossTenantIds.PatientA, CrossTenantIds.EncounterA, CrossTenantIds.UserA);
            SeedPrescription(db, CrossTenantIds.TenantB, CrossTenantIds.PrescB, CrossTenantIds.PatientB, CrossTenantIds.EncounterB, CrossTenantIds.UserB);

            SeedLabResult(db, CrossTenantIds.TenantA, CrossTenantIds.LabA, CrossTenantIds.PatientA, CrossTenantIds.EncounterA);
            SeedLabResult(db, CrossTenantIds.TenantB, CrossTenantIds.LabB, CrossTenantIds.PatientB, CrossTenantIds.EncounterB);

            SeedDrug(db, CrossTenantIds.TenantA, CrossTenantIds.DrugA, "CTADRUG-TENANT-A-0001", "Thuoc Tenant A");
            SeedDrug(db, CrossTenantIds.TenantB, CrossTenantIds.DrugB, CrossTenantIds.DrugBCode, "Thuoc Tenant B");

            db.SaveChanges();
            _seeded = true;
        }
    }

    /// <summary>
    /// DrugHandlers doc read-model rong hon entity EF (name_en, form, price, category_id,
    /// requires_prescription, is_psychotropic, is_narcotic, dtqg_drug_code, status, manufacturer,
    /// country) — cac cot nay do migration that tao nhung EnsureCreated (theo entity Drug) KHONG
    /// tao. Bo sung o DAY (khong dung TestSchemaSupplement.cs vi file do bi khoa cho task khac).
    /// Idempotent: bo qua neu cot da ton tai.
    /// </summary>
    private static void PatchSchema(AppDbContext db)
    {
        string[] alters =
        {
            // diab_his_pha_prescription_items: config Ignore khong ap, nhung PrescriptionHandlers
            // (list + detail) doc `i.deleted_at IS NULL` -> can cot nay (EnsureCreated tao thieu
            // vi PrescriptionItem soft-delete map khong ra cot tren DB test).
            "ALTER TABLE `diab_his_pha_prescription_items` ADD COLUMN `deleted_at` DATETIME NULL",
            "ALTER TABLE `diab_his_pha_drugs` ADD COLUMN `form` VARCHAR(50) NULL",
            "ALTER TABLE `diab_his_pha_drugs` ADD COLUMN `manufacturer` VARCHAR(255) NULL",
            "ALTER TABLE `diab_his_pha_drugs` ADD COLUMN `country` VARCHAR(100) NULL",
            "ALTER TABLE `diab_his_pha_drugs` ADD COLUMN `price` DECIMAL(15,2) NULL",
            "ALTER TABLE `diab_his_pha_drugs` ADD COLUMN `category_id` VARCHAR(36) NULL",
            "ALTER TABLE `diab_his_pha_drugs` ADD COLUMN `requires_prescription` TINYINT(1) NOT NULL DEFAULT 1",
            "ALTER TABLE `diab_his_pha_drugs` ADD COLUMN `is_psychotropic` TINYINT(1) NOT NULL DEFAULT 0",
            "ALTER TABLE `diab_his_pha_drugs` ADD COLUMN `is_narcotic` TINYINT(1) NOT NULL DEFAULT 0",
            "ALTER TABLE `diab_his_pha_drugs` ADD COLUMN `dtqg_drug_code` VARCHAR(50) NULL",
            "ALTER TABLE `diab_his_pha_drugs` ADD COLUMN `status` VARCHAR(20) NOT NULL DEFAULT 'ACTIVE'",
        };
        foreach (var sql in alters)
        {
            try { db.Database.ExecuteSqlRaw(sql); }
            catch { /* cot da ton tai -> bo qua */ }
        }
    }

    // Tenant + Branch co PK INT identity -> EF khong ton trong Id gan tay khi Add.
    // Chen thang bang raw SQL voi id tuong minh de dam bao tenant_id/branch_id = 1/2 on dinh
    // (JWT tenant_id + TenantId cua business rows deu dua vao 1/2). Idempotent qua INSERT IGNORE.
    private static void SeedTenant(AppDbContext db, int id)
    {
        db.Database.ExecuteSqlRaw(
            "INSERT IGNORE INTO diab_his_sys_tenants (id, code, name, subdomain, created_at, updated_at) " +
            "VALUES ({0}, {1}, {2}, {3}, NOW(), NOW())",
            id, $"CT-TENANT-{id}", $"Cross Tenant {id}", $"ct{id}");
    }

    private static void SeedBranch(AppDbContext db, int tenantId, int id, string code)
    {
        db.Database.ExecuteSqlRaw(
            "INSERT IGNORE INTO diab_his_sys_branches (id, tenant_id, code, name, created_at, updated_at) " +
            "VALUES ({0}, {1}, {2}, {3}, NOW(), NOW())",
            id, tenantId, code, $"Chi nhanh {code}");
    }

    private static void SeedUser(AppDbContext db, int tenantId, Guid id, string email)
    {
        if (db.Users.IgnoreQueryFilters().Any(u => u.Id == id)) return;
        db.Users.Add(new User
        {
            Id = id,
            TenantId = tenantId,
            BranchId = tenantId == CrossTenantIds.TenantA ? CrossTenantIds.BranchA : CrossTenantIds.BranchB,
            Email = email,
            PasswordHash = "x",
            FullName = $"User {email}",
            Status = UserStatus.Active,
            IsActive = true
        });
    }

    private static void SeedPatient(AppDbContext db, int tenantId, Guid id, string code, string name)
    {
        if (db.Patients.IgnoreQueryFilters().Any(p => p.Id == id)) return;
        db.Patients.Add(new Patient
        {
            Id = id,
            TenantId = tenantId,
            Code = code,
            FullName = name,
            Gender = Gender.Male,
            Status = PatientStatus.Active,
            Nationality = "VN",
            PatientType = "SERVICE"
        });
    }

    private static void SeedEncounter(AppDbContext db, int tenantId, Guid id, Guid patientId)
    {
        if (db.Encounters.IgnoreQueryFilters().Any(e => e.Id == id)) return;
        db.Encounters.Add(new Encounter
        {
            Id = id,
            TenantId = tenantId,
            BranchId = tenantId,
            PatientId = patientId.ToString(),
            EncounterType = EncounterTypes.FirstVisit,
            Status = EncounterStatus.Waiting
        });
    }

    private static void SeedBilling(AppDbContext db, int tenantId, Guid id, Guid patientId, Guid encounterId)
    {
        if (db.Billings.IgnoreQueryFilters().Any(b => b.Id == id)) return;
        db.Billings.Add(new Billing
        {
            Id = id,
            TenantId = tenantId,
            BranchId = tenantId,
            PatientId = patientId,
            EncounterId = encounterId,
            BillNo = $"CT-BILL-{tenantId}",
            Payer = "SELF",
            Status = BillingStatus.Draft
        });
    }

    private static void SeedPrescription(AppDbContext db, int tenantId, Guid id, Guid patientId, Guid encounterId, Guid doctorId)
    {
        if (db.Prescriptions.IgnoreQueryFilters().Any(p => p.Id == id)) return;
        db.Prescriptions.Add(new Prescription
        {
            Id = id,
            TenantId = tenantId,
            BranchId = tenantId,
            EncounterId = encounterId,
            PatientId = patientId,
            DoctorId = doctorId,
            PrescriptionNo = $"CT-RX-{tenantId}",
            Status = PrescriptionStatus.Draft
        });
    }

    private static void SeedLabResult(AppDbContext db, int tenantId, Guid id, Guid patientId, Guid encounterId)
    {
        if (db.LabResults.IgnoreQueryFilters().Any(l => l.Id == id)) return;
        var orderId = Guid.NewGuid().ToString();
        db.LabResults.Add(new LabResult
        {
            Id = id,
            TenantId = tenantId,
            BranchId = tenantId,
            LabOrderId = orderId,
            OrderId = orderId,
            PatientId = patientId.ToString(),
            EncounterId = encounterId.ToString(),
            TestCode = "GLU",
            TestName = "Glucose",
            Value = "5.5",
            Flag = "NORMAL",
            Status = "VERIFIED",
            Source = "MANUAL"
        });
    }

    private static void SeedDrug(AppDbContext db, int tenantId, Guid id, string code, string name)
    {
        if (db.Drugs.IgnoreQueryFilters().Any(d => d.Id == id)) return;
        db.Drugs.Add(new Drug
        {
            Id = id,
            TenantId = tenantId,
            Code = code,
            Name = name,
            Unit = "Vien",
            IsActive = true
        });
    }
}
