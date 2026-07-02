import apiClient from "./client";
import type { ApiResponse, ApiMeta } from "./types";

export interface SupplierRequest {
  code: string;
  name: string;
  tax_code?: string;
  address?: string;
  phone?: string;
  email?: string;
  contact_person?: string;
  status?: "ACTIVE" | "INACTIVE";
}

export interface SupplierResponse extends SupplierRequest {
  id: string;
  tenant_id: string;
  created_at: string;
  updated_at: string;
}

export async function listSuppliers(params?: {
  q?: string;
  status?: string;
  page?: number;
  page_size?: number;
}): Promise<{ data: SupplierResponse[]; meta: ApiMeta }> {
  const { data } = await apiClient.get<{ data: SupplierResponse[]; meta: ApiMeta }>("/suppliers", { params });
  return data;
}

export async function getSupplier(id: string): Promise<SupplierResponse> {
  const { data } = await apiClient.get<ApiResponse<SupplierResponse>>(`/suppliers/${id}`);
  return data.data;
}

export async function createSupplier(body: SupplierRequest): Promise<SupplierResponse> {
  const { data } = await apiClient.post<ApiResponse<SupplierResponse>>("/suppliers", body);
  return data.data;
}

export async function updateSupplier(id: string, body: SupplierRequest): Promise<SupplierResponse> {
  const { data } = await apiClient.put<ApiResponse<SupplierResponse>>(`/suppliers/${id}`, body);
  return data.data;
}

export async function deleteSupplier(id: string): Promise<void> {
  await apiClient.delete(`/suppliers/${id}`);
}
