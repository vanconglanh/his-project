namespace ProDiabHis.Application.Dashboard;

/// <summary>1 dong xep hang chi nhanh (US-6.1). Doanh thu = SUM(bil_billing.patient_payable) tinh theo
/// noi cung cap dich vu (billing.branch_id) - dung BR-86.</summary>
public record BranchRankingRow(
    int BranchId,
    string BranchName,
    decimal Revenue,
    int EncounterCount,
    decimal RevenuePerEncounter,
    int NewPatientCount,
    decimal CancelRate,
    decimal? PctChangeRevenue);

/// <summary>BR-92: metadata pham vi du lieu - "Du lieu: 3/12 chi nhanh".</summary>
public record BranchScopeMeta(
    int IncludedBranchCount,
    int TotalBranchCount,
    IReadOnlyList<string> IncludedBranchNames);

public record BranchRankingResponse(
    IReadOnlyList<BranchRankingRow> Items,
    BranchScopeMeta Meta);

public record DoctorKpiRow(
    Guid DoctorId,
    string DoctorName,
    decimal Revenue,
    int EncounterCount,
    decimal RevenuePerEncounter);

public record BranchDetailResponse(
    int BranchId,
    string BranchName,
    IReadOnlyList<DoctorKpiRow> Doctors);
