import apiClient from "./client";
import type { ApiResponse, ApiMeta } from "./types";

/**
 * Chi nhánh (Branch) — quan ly da chi nhanh trong 1 tenant.
 * Backend da san sang (BranchScopeMiddleware doc header X-Branch-Id).
 * GIA DINH shape response — backend chua co OpenAPI spec chi tiet cho module nay,
 * field duoc suy ra tu quy uoc chung cua du an (id number, snake_case, is_default/is_active).
 */
export interface BranchResponse {
  id: number;
  code: string;
  name: string;
  address?: string | null;
  phone?: string | null;
  is_default: boolean;
  is_active: boolean;
  created_at?: string;
  updated_at?: string;
}

export interface BranchRequest {
  code: string;
  name: string;
  address?: string;
  phone?: string;
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
