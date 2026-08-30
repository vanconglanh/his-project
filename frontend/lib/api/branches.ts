import apiClient from "./client";
import type { ApiResponse, ApiMeta } from "./types";

/**
 * Chi nhánh (Branch) — quan ly da chi nhanh trong 1 tenant.
 * Casing xac nhan tu backend/src/ProDiabHis.Application/Branches/BranchDtos.cs
 * (BranchDto/CreateBranchRequest/UpdateBranchRequest) — JsonNamingPolicy.SnakeCaseLower.
 */
export interface BranchResponse {
  id: number;
  code: string;
  name: string;
  cskcb_code?: string | null;
  address?: string | null;
  phone?: string | null;
  email?: string | null;
  working_hours?: string | null;
  timezone?: string;
  is_default: boolean;
  is_active: boolean;
  sort_order?: number;
  user_count?: number;
  status: string;
  hospital_rank?: string | null;
  kcb_tuyen?: string | null;
  bhyt_contract_code?: string | null;
  bhyt_contract_valid_from?: string | null;
  bhyt_contract_valid_to?: string | null;
  bhyt_enabled: boolean;
  dtqg_enabled: boolean;
  created_at?: string;
  updated_at?: string;
}

export interface BranchRequest {
  code: string;
  name: string;
  cskcb_code?: string;
  address?: string;
  phone?: string;
  email?: string;
  working_hours?: string;
  timezone?: string;
  hospital_rank?: string;
  kcb_tuyen?: string;
  bhyt_contract_code?: string;
  bhyt_contract_valid_from?: string;
  bhyt_contract_valid_to?: string;
  bhyt_enabled?: boolean;
  dtqg_enabled?: boolean;
}

export interface ListBranchesParams {
  is_active?: boolean;
  q?: string;
  page?: number;
  page_size?: number;
}

export async function listBranches(
  params?: ListBranchesParams
): Promise<{ data: BranchResponse[]; meta?: ApiMeta }> {
  const { data } = await apiClient.get<{ data: BranchResponse[]; meta?: ApiMeta }>(
    "/branches",
    { params }
  );
  return data;
}

export async function getBranch(id: number | string): Promise<BranchResponse> {
  const { data } = await apiClient.get<ApiResponse<BranchResponse>>(`/branches/${id}`);
  return data.data;
}

export async function createBranch(body: BranchRequest): Promise<BranchResponse> {
  const { data } = await apiClient.post<ApiResponse<BranchResponse>>("/branches", body);
  return data.data;
}

export async function updateBranch(
  id: number | string,
  body: BranchRequest
): Promise<BranchResponse> {
  const { data } = await apiClient.put<ApiResponse<BranchResponse>>(`/branches/${id}`, body);
  return data.data;
}

export async function deleteBranch(id: number | string): Promise<void> {
  await apiClient.delete(`/branches/${id}`);
}

export async function setDefaultBranch(id: number | string): Promise<BranchResponse> {
  const { data } = await apiClient.post<ApiResponse<BranchResponse>>(
    `/branches/${id}/set-default`
  );
  return data.data;
}

export async function setBranchStatus(
  id: number | string,
  is_active: boolean
): Promise<BranchResponse> {
  const { data } = await apiClient.post<ApiResponse<BranchResponse>>(
    `/branches/${id}/status`,
    { is_active }
  );
  return data.data;
}

export interface BranchUserRef {
  user_id: string;
  full_name?: string;
  email?: string;
}

export async function getBranchUsers(id: number | string): Promise<BranchUserRef[]> {
  const { data } = await apiClient.get<ApiResponse<BranchUserRef[]>>(`/branches/${id}/users`);
  return data.data;
}

export async function addBranchUsers(
  id: number | string,
  user_ids: string[]
): Promise<void> {
  await apiClient.post(`/branches/${id}/users`, { user_ids });
}

// ─── NV1: Tuân thủ BHYT/ĐTQG theo chi nhánh (BR-107) ───────────────────────────
// Casing xac nhan tu BranchBhytComplianceDto.

export interface BranchBhytComplianceResponse {
  branch_id: number;
  name: string;
  has_cskcb: boolean;
  bhyt_enabled: boolean;
  bhyt_contract_valid: boolean;
  dtqg_connected: boolean;
  dtqg_token_valid: boolean;
  last_bhyt_export_period: string | null;
}

export async function getBranchBhytCompliance(): Promise<BranchBhytComplianceResponse[]> {
  const { data } = await apiClient.get<ApiResponse<BranchBhytComplianceResponse[]>>(
    "/branches/bhyt-compliance"
  );
  return data.data;
}

// ─── NV2: Nhân bản chi nhánh + checklist go-live (BR-110/111/112) ──────────────
// Casing xac nhan tu CloneBranchRequest/ReadinessItemDto/BranchReadinessDto.

export interface CloneBranchRequest {
  source_branch_id: number;
  code: string;
  name: string;
  address?: string;
  phone?: string;
  email?: string;
  timezone?: string;
  group_id?: number;
}

export async function cloneBranch(
  sourceBranchId: number | string,
  body: CloneBranchRequest
): Promise<BranchResponse> {
  const { data } = await apiClient.post<ApiResponse<BranchResponse>>(
    `/branches/${sourceBranchId}/clone`,
    body
  );
  return data.data;
}

export interface BranchReadinessItem {
  key: string;
  label: string;
  passed: boolean;
  detail: string;
}

export interface BranchReadinessResponse {
  branch_id: number;
  all_passed: boolean;
  items: BranchReadinessItem[];
}

export async function getBranchReadiness(
  id: number | string
): Promise<BranchReadinessResponse> {
  const { data } = await apiClient.get<ApiResponse<BranchReadinessResponse>>(
    `/branches/${id}/readiness`
  );
  return data.data;
}

export async function activateBranch(id: number | string): Promise<BranchResponse> {
  const { data } = await apiClient.post<ApiResponse<BranchResponse>>(
    `/branches/${id}/activate`
  );
  return data.data;
}
