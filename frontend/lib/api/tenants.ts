import apiClient from "./client";
import type {
  ApiResponse,
  ApiMeta,
  TenantResponse,
  CreateTenantRequest,
  UpdateTenantRequest,
  UpdateTenantProfileRequest,
} from "./types";

export interface ListTenantsParams {
  page?: number;
  page_size?: number;
  sort?: string;
  q?: string;
  status?: string;
}

export interface PagedTenants {
  data: TenantResponse[];
  meta: ApiMeta;
}

export async function listTenants(params?: ListTenantsParams): Promise<PagedTenants> {
  const { data } = await apiClient.get<PagedTenants>("/tenants", { params });
  return data;
}

export async function createTenant(payload: CreateTenantRequest): Promise<TenantResponse> {
  const { data } = await apiClient.post<ApiResponse<TenantResponse>>("/tenants", payload);
  return data.data;
}

export async function getTenant(id: number): Promise<TenantResponse> {
  const { data } = await apiClient.get<ApiResponse<TenantResponse>>(`/tenants/${id}`);
  return data.data;
}

export async function updateTenant(id: number, payload: UpdateTenantRequest): Promise<TenantResponse> {
  const { data } = await apiClient.put<ApiResponse<TenantResponse>>(`/tenants/${id}`, payload);
  return data.data;
}

export async function suspendTenant(id: number, reason?: string): Promise<TenantResponse> {
  const { data } = await apiClient.post<ApiResponse<TenantResponse>>(`/tenants/${id}/suspend`, { reason });
  return data.data;
}

export async function activateTenant(id: number): Promise<TenantResponse> {
  const { data } = await apiClient.post<ApiResponse<TenantResponse>>(`/tenants/${id}/activate`);
  return data.data;
}

export async function deleteTenant(id: number): Promise<void> {
  await apiClient.delete(`/tenants/${id}`);
}

export async function getMyTenant(): Promise<TenantResponse> {
  const { data } = await apiClient.get<ApiResponse<TenantResponse>>("/tenants/me");
  return data.data;
}

export async function updateMyTenant(payload: UpdateTenantProfileRequest): Promise<TenantResponse> {
  const { data } = await apiClient.put<ApiResponse<TenantResponse>>("/tenants/me", payload);
  return data.data;
}
