using System.Text.Json.Serialization;

namespace ProDiabHis.Application.Reports;

// --------------- Shared --------------- //

public record ChartDataPoint(string Label, decimal Value, string? Color = null);

// --------------- Revenue --------------- //

public record RevenueReportResponse(
    string Period,
    DateOnly From,
    DateOnly To,
    decimal TotalRevenue,
    int TotalInvoices,
    decimal TotalRefunds,
    decimal NetRevenue,
    IReadOnlyList<ChartDataPoint> Series);

// --------------- Doctor KPI --------------- //

public record DoctorKpiResponse(
    Guid DoctorId,
    string DoctorName,
    long TotalEncounters,
    decimal TotalRevenue,
    decimal AvgRevenuePerEncounter,
    long PrescriptionCount);

// --------------- Top Drugs --------------- //

public record TopDrugResponse(
    Guid DrugId,
    string DrugName,
    string? ActiveIngredient,
    int QuantityDispensed,
    decimal TotalRevenue,
    int PrescriptionCount);

// --------------- Diabetes Cohort --------------- //

public record HbA1cBucket(string Label, int PatientCount, decimal Percentage);

public record DiabetesCohortResponse(
    string PeriodKey,
    int TotalPatients,
    decimal AvgHbA1c,
    decimal PctControlled,   // HbA1c < 7
    decimal PctUncontrolled, // HbA1c >= 7
    IReadOnlyList<HbA1cBucket> Buckets);

// Detailed cohort: theo loai DM, phan bo HbA1c, bien chung.
// PascalCase — controller project sang snake_case khop FE interface DiabetesCohort.
public record DiabetesCohortByType(int T1, int T2, int Gdm);

public record DiabetesHba1cDistribution(int Lt7, int Between7And8, int Between8And9, int Gt9);

public record DiabetesComplications(
    int Retinopathy,
    int Neuropathy,
    int Nephropathy,
    int Cad,
    int Pad);

public record DiabetesCohortDetailedResponse(
    DateOnly AsOf,
    int TotalPatients,
    DiabetesCohortByType ByType,
    DiabetesHba1cDistribution Hba1cDistribution,
    DiabetesComplications Complications);

// --------------- Dashboard --------------- //

public record DashboardOverviewResponse(
    int TodayEncounters,
    int WaitingPatients,
    decimal TodayRevenue,
    int LowStockAlerts,
    int NearExpiryAlerts,
    int BhytPendingCount,
    int DtqgFailedCount);

public record AlertResponse(
    Guid Id,
    string Type,
    string Severity,
    string Message,
    DateTimeOffset CreatedAt,
    object? Payload);

// --------------- Export --------------- //

public enum ExportFormat { Excel, Pdf }

public record ExportReportRequest(
    string ReportType,
    DateOnly From,
    DateOnly To,
    ExportFormat Format,
    Guid? ClinicId);

// --------------- Generic breakdown --------------- //

public record BreakdownItem(string Key, string Label, decimal Amount, int Count, decimal Percentage);

public record BreakdownListResponse(IReadOnlyList<BreakdownItem> Data);

// --------------- Report Print A4 --------------- //

public enum ReportType
{
    Financial,
    Clinical,
    Pharmacy
}

/// <summary>Tham so noi bo phuc vu QuestPDF exporter — khong expose ra client.</summary>
public record ReportPdfRequest(
    int TenantId,
    ReportType ReportType,
    DateOnly FromDate,
    DateOnly ToDate,
    int? ClinicId,
    Guid? ExportedByUserId,
    string ReportCode,
    string? ExportedByFullName = null);

/// <summary>Thong tin letterhead lay tu diab_his_sys_tenants.</summary>
public record LetterheadDto(
    string ClinicName,
    string? CskcbCode,
    string? CompanyName,
    string? Address,
    string? Phone,
    string? Email,
    string? EmailSupport,
    string? LogoUrl,
    string? Slogan = null,
    string? Website = null);

/// <summary>Hang du lieu bao cao tai chinh (Financial).</summary>
public record FinancialRowDto(
    int Stt,
    string InvoiceNo,
    string PatientName,
    string ServiceName,
    decimal Amount);

/// <summary>Hang du lieu bao cao lam sang (Clinical).</summary>
public record ClinicalRowDto(
    int Stt,
    string PatientName,
    string DoctorName,
    string Icd10Code,
    DateOnly EncounterDate);

/// <summary>Hang du lieu bao cao ton kho duoc (Pharmacy).</summary>
public record PharmacyRowDto(
    string DrugCode,
    string DrugName,
    string? LotNumber,
    DateOnly? ExpiryDate,
    decimal StockQuantity,
    string Unit);

/// <summary>So lieu luot kham theo ky (DAY/WEEK/MONTH).</summary>
public record EncounterCountItem(string PeriodLabel, int Count);

/// <summary>Top chan doan ICD-10.</summary>
public record TopDiagnosisResponse(string Icd10Code, string? Icd10Name, int Count, decimal Percentage);

// --------------- F7: Financial breakdown (by-service / by-payment-method) --------------- //

/// <summary>Doanh thu theo dich vu (khong tinh thuoc — da co bao cao pharmacy/top-drugs rieng).</summary>
public record ServiceRevenueResponse(
    string? ServiceCode,
    string ServiceName,
    string ItemType,
    int Count,
    decimal TotalRevenue,
    decimal Percentage);

/// <summary>Breakdown theo phuong thuc thanh toan — shape khop FE BreakdownItem (label/value/count/percentage).</summary>
public record PaymentMethodBreakdownResponse(string Label, decimal Value, int Count, decimal Percentage);

// --------------- F7: Cashier daily summary --------------- //

public record CashierDailySummaryResponse(
    DateOnly Date,
    decimal TotalRevenue,
    int TotalInvoices,
    decimal TotalRefunds,
    IReadOnlyList<PaymentMethodBreakdownResponse> ByPaymentMethod,
    decimal OpeningBalance,
    decimal ClosingBalance);

// --------------- F7: Debts aging --------------- //

public record DebtDetailItem(
    string BillNo,
    string PatientName,
    decimal Balance,
    int DaysOverdue,
    DateOnly? DueDate);

public record DebtsAgingResponse(
    [property: JsonPropertyName("bucket_0_30")] decimal Bucket0To30,
    [property: JsonPropertyName("bucket_30_60")] decimal Bucket30To60,
    [property: JsonPropertyName("bucket_60_90")] decimal Bucket60To90,
    [property: JsonPropertyName("bucket_over_90")] decimal BucketOver90,
    decimal Total,
    IReadOnlyList<DebtDetailItem> Details);

// --------------- F7: BHYT summary --------------- //

public record BhytSummaryResponse(
    int TotalCards,
    decimal TotalAmountClaimed,
    decimal TotalAmountPaid,
    decimal TotalAmountRejected,
    decimal RejectionRate,
    int PendingCount);

// --------------- F7: Clinical visits (paged) --------------- //

public record ClinicalVisitItem(
    Guid EncounterId,
    string PatientName,
    string? DoctorName,
    string Status,
    string? PrimaryIcd10Code,
    string? PrimaryIcd10Name,
    DateTime? StartedAt);

// --------------- F7: Pharmacy inventory value --------------- //

public record InventoryCategoryItem(string Category, decimal Value, int SkuCount);

public record InventoryValueResponse(
    decimal TotalValue,
    int TotalSkus,
    IReadOnlyList<InventoryCategoryItem> ByCategory);

