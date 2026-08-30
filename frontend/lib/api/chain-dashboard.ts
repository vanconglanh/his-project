import apiClient from "./client";

// ---- Types ----
// Casing xac nhan tu backend/src/ProDiabHis.Application/Dashboard/ChainDashboardDtos.cs
// (JsonNamingPolicy.SnakeCaseLower toan cuc -> record property PascalCase -> wire snake_case)

export interface BranchRankingRow {
  branch_id: number;
  branch_name: string;
  revenue: number;
  encounter_count: number;
  revenue_per_encounter: number;
  new_patient_count: number;
  cancel_rate: number;
  pct_change_revenue: number | null;
}

export interface BranchScopeMeta {
  included_branch_count: number;
  total_branch_count: number;
  included_branch_names: string[];
}

export interface BranchRankingResponse {
  data: BranchRankingRow[];
  meta: BranchScopeMeta;
}

export interface DoctorKpiRow {
  doctor_id: string;
  doctor_name: string;
  revenue: number;
  encounter_count: number;
  revenue_per_encounter: number;
}

export interface BranchDetailResponse {
  branch_id: number;
  branch_name: string;
  doctors: DoctorKpiRow[];
}

export interface DateRangeParams {
  from?: string;
  to?: string;
}

// ---- API functions ----

export async function getBranchRanking(params: DateRangeParams): Promise<BranchRankingResponse> {
  const { data } = await apiClient.get<BranchRankingResponse>("/dashboard/branch-ranking", {
    params,
  });
  return data;
}

export async function getBranchDetail(
  branchId: number,
  params: DateRangeParams
): Promise<BranchDetailResponse> {
  const { data } = await apiClient.get<{ data: BranchDetailResponse }>(
    `/dashboard/branch/${branchId}/detail`,
    { params }
  );
  return data.data;
}
