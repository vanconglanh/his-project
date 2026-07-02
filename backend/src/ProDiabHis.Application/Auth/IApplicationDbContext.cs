using Microsoft.EntityFrameworkCore;
using ProDiabHis.Domain.Entities;
using ProDiabHis.Domain.Entities.Bhyt;
using ProDiabHis.Domain.Entities.Pharmacy;

namespace ProDiabHis.Application.Auth;

/// <summary>Interface EF DbContext de tach biet Application khoi Infrastructure</summary>
public interface IApplicationDbContext
{
    // Auth / Security
    DbSet<User> Users { get; }
    DbSet<Role> Roles { get; }
    DbSet<Permission> Permissions { get; }
    DbSet<RolePermission> RolePermissions { get; }
    DbSet<Tenant> Tenants { get; }
    DbSet<UserRole> UserRoles { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<AuditLog> AuditLogs { get; }

    // Patient
    DbSet<Patient> Patients { get; }
    DbSet<Allergy> Allergies { get; }
    DbSet<Insurance> Insurances { get; }
    DbSet<EmergencyContact> EmergencyContacts { get; }
    DbSet<Consent> Consents { get; }

    // Encounter
    DbSet<Encounter> Encounters { get; }
    DbSet<EncounterDiagnosis> EncounterDiagnoses { get; }
    DbSet<Domain.Entities.VitalSigns> VitalSigns { get; }
    DbSet<EmrContent> EmrContents { get; }
    DbSet<EmrVersion> EmrVersions { get; }
    DbSet<EmrSignature> EmrSignatures { get; }
    DbSet<EmrTemplate> EmrTemplates { get; }

    // Lab / Rad / CLS
    DbSet<LabOrder> LabOrders { get; }
    DbSet<RadOrder> RadOrders { get; }
    DbSet<ClsUpload> ClsUploads { get; }
    DbSet<LabResult> LabResults { get; }
    DbSet<LabPartner> LabPartners { get; }

    // Pharmacy
    DbSet<Drug> Drugs { get; }
    DbSet<Stock> Stocks { get; }
    DbSet<Prescription> Prescriptions { get; }
    DbSet<PrescriptionItem> PrescriptionItems { get; }
    DbSet<Dispense> Dispenses { get; }
    DbSet<Supplier> Suppliers { get; }
    DbSet<PurchaseOrder> PurchaseOrders { get; }
    DbSet<Grn> Grns { get; }

    // Billing / Cashier
    DbSet<BillingService> BillingServices { get; }
    DbSet<ServicePackage> ServicePackages { get; }
    DbSet<Domain.Entities.Billing> Billings { get; }
    DbSet<BillingItem> BillingItems { get; }
    DbSet<Payment> Payments { get; }
    DbSet<QrCode> QrCodes { get; }
    DbSet<EInvoice> EInvoices { get; }
    DbSet<CashierShift> CashierShifts { get; }

    // Notifications
    DbSet<Notification> Notifications { get; }
    DbSet<WebPushSubscription> WebPushSubscriptions { get; }
    DbSet<NotificationPreference> NotificationPreferences { get; }
    DbSet<VapidKey> VapidKeys { get; }

    // Portal
    DbSet<PortalAccount> PortalAccounts { get; }
    DbSet<PortalOtpLog> PortalOtpLogs { get; }
    DbSet<PortalSession> PortalSessions { get; }

    // Diabetes (Clinical)
    DbSet<DiabetesAssessment> DiabetesAssessments { get; }
    DbSet<DiabetesTemplate> DiabetesTemplates { get; }

    // API Partners
    DbSet<ApiPartner> ApiPartners { get; }

    // BHYT
    DbSet<BhytExport> BhytExports { get; }
    DbSet<BhytExportItem> BhytExportItems { get; }
    DbSet<BhytReconcileUpload> BhytReconcileUploads { get; }
    DbSet<BhytReconcileItem> BhytReconcileItems { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
