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
    string? LogoUrl);

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

