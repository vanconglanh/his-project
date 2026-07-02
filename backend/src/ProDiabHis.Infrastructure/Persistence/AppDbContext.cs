using Microsoft.EntityFrameworkCore;
using ProDiabHis.Application.Auth;
using ProDiabHis.Application.Common;
using ProDiabHis.Domain.Common;
using ProDiabHis.Domain.Entities;
using ProDiabHis.Domain.Entities.Bhyt;
using ProDiabHis.Domain.Entities.Pharmacy;
using BillingEntity = ProDiabHis.Domain.Entities.Billing;

namespace ProDiabHis.Infrastructure.Persistence;

public class AppDbContext : DbContext, IApplicationDbContext
{
    private readonly ITenantProvider _tenantProvider;

    public AppDbContext(DbContextOptions<AppDbContext> options, ITenantProvider tenantProvider)
        : base(options)
    {
        _tenantProvider = tenantProvider;
    }

    // Auth / Security
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    // Patient
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Allergy> Allergies => Set<Allergy>();
    public DbSet<Insurance> Insurances => Set<Insurance>();
    public DbSet<EmergencyContact> EmergencyContacts => Set<EmergencyContact>();
    public DbSet<Consent> Consents => Set<Consent>();

    // Encounter
    public DbSet<Encounter> Encounters => Set<Encounter>();
    public DbSet<EncounterDiagnosis> EncounterDiagnoses => Set<EncounterDiagnosis>();
    public DbSet<VitalSigns> VitalSigns => Set<VitalSigns>();
    public DbSet<EmrContent> EmrContents => Set<EmrContent>();
    public DbSet<EmrVersion> EmrVersions => Set<EmrVersion>();
    public DbSet<EmrSignature> EmrSignatures => Set<EmrSignature>();
    public DbSet<EmrTemplate> EmrTemplates => Set<EmrTemplate>();

    // Lab / Rad / CLS
    public DbSet<LabOrder> LabOrders => Set<LabOrder>();
    public DbSet<RadOrder> RadOrders => Set<RadOrder>();
    public DbSet<ClsUpload> ClsUploads => Set<ClsUpload>();
    public DbSet<LabResult> LabResults => Set<LabResult>();
    public DbSet<LabPartner> LabPartners => Set<LabPartner>();

    // Pharmacy
    public DbSet<Drug> Drugs => Set<Drug>();
    public DbSet<Stock> Stocks => Set<Stock>();
    public DbSet<Prescription> Prescriptions => Set<Prescription>();
    public DbSet<PrescriptionItem> PrescriptionItems => Set<PrescriptionItem>();
    public DbSet<Dispense> Dispenses => Set<Dispense>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<Grn> Grns => Set<Grn>();

    // Billing / Cashier
    public DbSet<BillingService> BillingServices => Set<BillingService>();
    public DbSet<ServicePackage> ServicePackages => Set<ServicePackage>();
    public DbSet<BillingEntity> Billings => Set<BillingEntity>();
    public DbSet<BillingItem> BillingItems => Set<BillingItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<QrCode> QrCodes => Set<QrCode>();
    public DbSet<EInvoice> EInvoices => Set<EInvoice>();
    public DbSet<CashierShift> CashierShifts => Set<CashierShift>();

    // Notifications
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<WebPushSubscription> WebPushSubscriptions => Set<WebPushSubscription>();
    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();
    public DbSet<VapidKey> VapidKeys => Set<VapidKey>();

    // Portal
    public DbSet<PortalAccount> PortalAccounts => Set<PortalAccount>();
    public DbSet<PortalOtpLog> PortalOtpLogs => Set<PortalOtpLog>();
    public DbSet<PortalSession> PortalSessions => Set<PortalSession>();

    // Diabetes (Clinical)
    public DbSet<DiabetesAssessment> DiabetesAssessments => Set<DiabetesAssessment>();
    public DbSet<DiabetesTemplate> DiabetesTemplates => Set<DiabetesTemplate>();

    // API Partners
    public DbSet<ApiPartner> ApiPartners => Set<ApiPartner>();

    // BHYT
    public DbSet<BhytExport> BhytExports => Set<BhytExport>();
    public DbSet<BhytExportItem> BhytExportItems => Set<BhytExportItem>();
    public DbSet<BhytReconcileUpload> BhytReconcileUploads => Set<BhytReconcileUpload>();
    public DbSet<BhytReconcileItem> BhytReconcileItems => Set<BhytReconcileItem>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);
        // Pomelo 8.0.x chua ho tro EF Core 8 primitive collection (ElementMappingConvention)
        // Disable convention nay de tranh NullReferenceException khi FinalizeModel
        configurationBuilder.Conventions.Remove<Microsoft.EntityFrameworkCore.Metadata.Conventions.ElementMappingConvention>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // ===== GLOBAL QUERY FILTERS =====

        // Auth / Security
        modelBuilder.Entity<User>()
            .HasQueryFilter(u => u.DeletedAt == null && u.TenantId == _tenantProvider.TenantId);

        modelBuilder.Entity<Role>()
            .HasQueryFilter(r => r.DeletedAt == null &&
                (r.TenantId == null || r.TenantId == _tenantProvider.TenantId));

        modelBuilder.Entity<Tenant>()
            .HasQueryFilter(t => t.DeletedAt == null);

        modelBuilder.Entity<AuditLog>()
            .HasQueryFilter(a => a.TenantId == _tenantProvider.TenantId);

        // Patient
        modelBuilder.Entity<Patient>()
            .HasQueryFilter(e => e.DeletedAt == null && e.TenantId == _tenantProvider.TenantId);

        modelBuilder.Entity<Allergy>()
            .HasQueryFilter(e => e.DeletedAt == null && e.TenantId == _tenantProvider.TenantId);

        modelBuilder.Entity<Insurance>()
            .HasQueryFilter(e => e.DeletedAt == null && e.TenantId == _tenantProvider.TenantId);

        modelBuilder.Entity<EmergencyContact>()
            .HasQueryFilter(e => e.DeletedAt == null && e.TenantId == _tenantProvider.TenantId);

        modelBuilder.Entity<Consent>()
            .HasQueryFilter(e => e.DeletedAt == null && e.TenantId == _tenantProvider.TenantId);

        // Encounter
        modelBuilder.Entity<Encounter>()
            .HasQueryFilter(e => e.DeletedAt == null && e.TenantId == _tenantProvider.TenantId);

        modelBuilder.Entity<EncounterDiagnosis>()
            .HasQueryFilter(e => e.DeletedAt == null && e.TenantId == _tenantProvider.TenantId);

        modelBuilder.Entity<VitalSigns>()
            .HasQueryFilter(e => e.DeletedAt == null && e.TenantId == _tenantProvider.TenantId);

        modelBuilder.Entity<EmrContent>()
            .HasQueryFilter(e => e.DeletedAt == null && e.TenantId == _tenantProvider.TenantId);

        modelBuilder.Entity<EmrVersion>()
            .HasQueryFilter(e => e.TenantId == _tenantProvider.TenantId);

        modelBuilder.Entity<EmrSignature>()
            .HasQueryFilter(e => e.TenantId == _tenantProvider.TenantId);

        modelBuilder.Entity<EmrTemplate>()
            .HasQueryFilter(e => e.DeletedAt == null
                && (e.TenantId == null || e.TenantId == _tenantProvider.TenantId));

        // Lab / Rad
        modelBuilder.Entity<LabOrder>()
            .HasQueryFilter(e => e.DeletedAt == null && e.TenantId == _tenantProvider.TenantId);

        modelBuilder.Entity<RadOrder>()
            .HasQueryFilter(e => e.DeletedAt == null && e.TenantId == _tenantProvider.TenantId);

        modelBuilder.Entity<ClsUpload>()
            .HasQueryFilter(e => e.DeletedAt == null && e.TenantId == _tenantProvider.TenantId);

        modelBuilder.Entity<LabResult>()
            .HasQueryFilter(e => e.DeletedAt == null && e.TenantId == _tenantProvider.TenantId);

        modelBuilder.Entity<LabPartner>()
            .HasQueryFilter(e => e.DeletedAt == null && e.TenantId == _tenantProvider.TenantId);

        // Pharmacy
        modelBuilder.Entity<Drug>()
            .HasQueryFilter(e => e.DeletedAt == null && e.TenantId == _tenantProvider.TenantId);

        modelBuilder.Entity<Stock>()
            .HasQueryFilter(e => e.TenantId == _tenantProvider.TenantId);

        modelBuilder.Entity<Prescription>()
            .HasQueryFilter(e => e.DeletedAt == null && e.TenantId == _tenantProvider.TenantId);

        modelBuilder.Entity<PrescriptionItem>()
            .HasQueryFilter(e => e.TenantId == _tenantProvider.TenantId);

        modelBuilder.Entity<Dispense>()
            .HasQueryFilter(e => e.TenantId == _tenantProvider.TenantId);

        modelBuilder.Entity<Supplier>()
            .HasQueryFilter(e => e.DeletedAt == null && e.TenantId == _tenantProvider.TenantId);

        modelBuilder.Entity<PurchaseOrder>()
            .HasQueryFilter(e => e.DeletedAt == null && e.TenantId == _tenantProvider.TenantId);

        modelBuilder.Entity<Grn>()
            .HasQueryFilter(e => e.DeletedAt == null && e.TenantId == _tenantProvider.TenantId);

        // Billing
        modelBuilder.Entity<BillingEntity>()
            .HasQueryFilter(e => e.DeletedAt == null && e.TenantId == _tenantProvider.TenantId);

        modelBuilder.Entity<BillingItem>()
            .HasQueryFilter(e => e.TenantId == _tenantProvider.TenantId);

        modelBuilder.Entity<BillingService>()
            .HasQueryFilter(e => e.DeletedAt == null && e.TenantId == _tenantProvider.TenantId);

        modelBuilder.Entity<ServicePackage>()
            .HasQueryFilter(e => e.DeletedAt == null && e.TenantId == _tenantProvider.TenantId);

        modelBuilder.Entity<Payment>()
            .HasQueryFilter(e => e.TenantId == _tenantProvider.TenantId);

        modelBuilder.Entity<CashierShift>()
            .HasQueryFilter(e => e.TenantId == _tenantProvider.TenantId);

        // Notifications
        modelBuilder.Entity<Notification>()
            .HasQueryFilter(e => e.TenantId == _tenantProvider.TenantId);

        modelBuilder.Entity<WebPushSubscription>()
            .HasQueryFilter(e => e.TenantId == _tenantProvider.TenantId);

        modelBuilder.Entity<NotificationPreference>()
            .HasQueryFilter(e => e.TenantId == _tenantProvider.TenantId);

        modelBuilder.Entity<VapidKey>()
            .HasQueryFilter(e => e.TenantId == _tenantProvider.TenantId);

        // Portal
        modelBuilder.Entity<PortalAccount>()
            .HasQueryFilter(e => e.DeletedAt == null && e.TenantId == _tenantProvider.TenantId);

        modelBuilder.Entity<PortalOtpLog>()
            .HasQueryFilter(e => e.TenantId == _tenantProvider.TenantId);

        modelBuilder.Entity<PortalSession>()
            .HasQueryFilter(e => e.TenantId == _tenantProvider.TenantId);

        // Diabetes
        modelBuilder.Entity<DiabetesAssessment>()
            .HasQueryFilter(e => e.DeletedAt == null && e.TenantId == _tenantProvider.TenantId);

        // ApiPartner
        modelBuilder.Entity<ApiPartner>()
            .HasQueryFilter(e => e.DeletedAt == null);

        // BHYT
        modelBuilder.Entity<BhytExport>()
            .HasQueryFilter(e => e.DeletedAt == null && e.TenantId == _tenantProvider.TenantId);

        modelBuilder.Entity<BhytReconcileUpload>()
            .HasQueryFilter(e => e.DeletedAt == null && e.TenantId == _tenantProvider.TenantId);

        modelBuilder.Entity<BhytReconcileItem>()
            .HasQueryFilter(e => e.TenantId == _tenantProvider.TenantId);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
                entry.Entity.CreatedAt = now;
            if (entry.State is EntityState.Added or EntityState.Modified)
                entry.Entity.UpdatedAt = now;
        }
        return base.SaveChangesAsync(cancellationToken);
    }
}
