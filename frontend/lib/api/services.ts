import apiClient from "./client";
import type { ApiMeta } from "./types";

// ─── Types ────────────────────────────────────────────────────────────────────

export type ServiceCategory =
  | "CONSULTATION"
  | "PROCEDURE"
  | "LAB"
  | "RAD"
  | "PHARMACY"
  | "OTHER";

export interface ServiceResponse {
  id: string;
  tenant_id: string;
  code: string;
  name: string;
  category: ServiceCategory;
  price: number;
  vat_rate: 0 | 5 | 8 | 10;
  bhyt_code: string | null;
  bhyt_max_amount: number | null;
  is_active: boolean;
  created_at: string;
  updated_at: string;
}

export interface ServiceUpsertRequest {
  code: string;
  name: string;
  category: ServiceCategory;
  price: number;
  vat_rate: 0 | 5 | 8 | 10;
  bhyt_code?: string | null;
  bhyt_max_amount?: number | null;
  is_active?: boolean;
}

export interface ServicePackageItem {
  service_id: string;
  service_name?: string;
  unit_price?: number;
  quantity: number;
}

export interface ServicePackageResponse {
  id: string;
  tenant_id: string;
  code: string;
  name: string;
  services: ServicePackageItem[];
  total_price: number;
  discount_percent: number;
  final_price: number;
  valid_from: string | null;
  valid_to: string | null;
  is_active: boolean;
}

export interface ServicePackageUpsertRequest {
  code: string;
  name: string;
  services: Array<{ service_id: string; quantity: number }>;
  discount_percent?: number;
  valid_from?: string | null;
  valid_to?: string | null;
  is_active?: boolean;
}

export interface ServiceListParams {
  q?: string;
  category?: ServiceCategory;
  is_active?: boolean;
  page?: number;
  page_size?: number;
}

export interface ServiceListResponse {
  data: ServiceResponse[];
  meta: ApiMeta;
}

// ─── API ──────────────────────────────────────────────────────────────────────

export async function listServices(params?: ServiceListParams): Promise<ServiceListResponse> {
  const { data } = await apiClient.get<ServiceListResponse>("/services", { params });
  return data;
}

export async function getService(id: string): Promise<ServiceResponse> {
  const { data } = await apiClient.get<{ data: ServiceResponse }>(`/services/${id}`);
  return data.data;
}

export async function createService(body: ServiceUpsertRequest): Promise<ServiceResponse> {
  const { data } = await apiClient.post<{ data: ServiceResponse }>("/services", body);
  return data.data;
}

export async function updateService(id: string, body: ServiceUpsertRequest): Promise<ServiceResponse> {
  const { data } = await apiClient.put<{ data: ServiceResponse }>(`/services/${id}`, body);
  return data.data;
}

export async function deleteService(id: string): Promise<void> {
  await apiClient.delete(`/services/${id}`);
}

export async function searchServices(q: string): Promise<ServiceResponse[]> {
  const { data } = await apiClient.get<ServiceListResponse>("/services/search", { params: { q } });
  return data.data;
}

export async function importServices(file: File): Promise<{ total: number; inserted: number; updated: number; errors: unknown[] }> {
  const form = new FormData();
  form.append("file", file);
  const { data } = await apiClient.post<{ data: { total: number; inserted: number; updated: number; errors: unknown[] } }>("/services/import", form, {
    headers: { "Content-Type": "multipart/form-data" },
  });
  return data.data;
}

// Service Packages

export async function listServicePackages(params?: { q?: string; is_active?: boolean }): Promise<{ data: ServicePackageResponse[]; meta: ApiMeta }> {
  const { data } = await apiClient.get<{ data: ServicePackageResponse[]; meta: ApiMeta }>("/service-packages", { params });
  return data;
}

export async function createServicePackage(body: ServicePackageUpsertRequest): Promise<ServicePackageResponse> {
  const { data } = await apiClient.post<{ data: ServicePackageResponse }>("/service-packages", body);
  return data.data;
}

export async function updateServicePackage(id: string, body: ServicePackageUpsertRequest): Promise<ServicePackageResponse> {
  const { data } = await apiClient.put<{ data: ServicePackageResponse }>(`/service-packages/${id}`, body);
  return data.data;
}

export async function deleteServicePackage(id: string): Promise<void> {
  await apiClient.delete(`/service-packages/${id}`);
}
