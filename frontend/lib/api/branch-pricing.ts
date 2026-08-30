import apiClient from "./client";
import type { ApiMeta } from "./types";

/**
 * Override giá + ẩn/hiện (is_active) theo chi nhánh/nhóm chi nhánh cho
 * DỊCH VỤ (/service-price-overrides) và THUỐC (/drug-price-overrides).
 * Contract khoá bởi Leader — KHÔNG tự đổi shape.
 */

export type PriceOverrideScope = "BRANCH" | "GROUP";

export interface PriceOverrideListParams {
  branch_id?: number;
  group_id?: number;
  scope?: PriceOverrideScope;
  page?: number;
  page_size?: number;
}

export interface PriceOverrideUpdateRequest {
  price: number;
  is_active: boolean;
  effective_from: string; // YYYY-MM-DD
  effective_to?: string | null; // YYYY-MM-DD
  note?: string;
}

// ─── Dịch vụ ────────────────────────────────────────────────────────────────

export interface ServicePriceOverrideResponse {
  id: string;
  tenant_id: string;
  service_id: string;
  service_name: string;
  scope: PriceOverrideScope;
  branch_id: number | null;
  group_id: number | null;
  price: number;
  is_active: boolean;
  effective_from: string;
  effective_to: string | null;
  note: string | null;
  created_at: string;
  created_by: string | number | null;
}

export interface ServicePriceOverrideCreateRequest {
  service_id: string;
  scope: PriceOverrideScope;
  branch_id?: number;
  group_id?: number;
  price: number;
  is_active: boolean;
  effective_from: string;
  effective_to?: string | null;
  note?: string;
}

export interface ServicePriceOverrideListParams extends PriceOverrideListParams {
  service_id?: string;
}

export async function listServicePriceOverrides(
  params?: ServicePriceOverrideListParams
): Promise<{ data: ServicePriceOverrideResponse[]; meta: ApiMeta }> {
  const { data } = await apiClient.get<{ data: ServicePriceOverrideResponse[]; meta: ApiMeta }>(
    "/service-price-overrides",
    { params }
  );
  return data;
}

export async function getServicePriceOverride(id: string): Promise<ServicePriceOverrideResponse> {
  const { data } = await apiClient.get<{ data: ServicePriceOverrideResponse }>(
    `/service-price-overrides/${id}`
  );
  return data.data;
}

export async function createServicePriceOverride(
  body: ServicePriceOverrideCreateRequest
): Promise<ServicePriceOverrideResponse> {
  const { data } = await apiClient.post<{ data: ServicePriceOverrideResponse }>(
    "/service-price-overrides",
    body
  );
  return data.data;
}

export async function updateServicePriceOverride(
  id: string,
  body: PriceOverrideUpdateRequest
): Promise<ServicePriceOverrideResponse> {
  const { data } = await apiClient.put<{ data: ServicePriceOverrideResponse }>(
    `/service-price-overrides/${id}`,
    body
  );
  return data.data;
}

export async function deleteServicePriceOverride(id: string): Promise<void> {
  await apiClient.delete(`/service-price-overrides/${id}`);
}

// ─── Thuốc ──────────────────────────────────────────────────────────────────

export interface DrugPriceOverrideResponse {
  id: string;
  tenant_id: string;
  drug_id: string;
  drug_name: string;
  scope: PriceOverrideScope;
  branch_id: number | null;
  group_id: number | null;
  price: number;
  is_active: boolean;
  effective_from: string;
  effective_to: string | null;
  note: string | null;
  created_at: string;
  created_by: string | number | null;
}

export interface DrugPriceOverrideCreateRequest {
  drug_id: string;
  scope: PriceOverrideScope;
  branch_id?: number;
  group_id?: number;
  price: number;
  is_active: boolean;
  effective_from: string;
  effective_to?: string | null;
  note?: string;
}

export interface DrugPriceOverrideListParams extends PriceOverrideListParams {
  drug_id?: string;
}

export async function listDrugPriceOverrides(
  params?: DrugPriceOverrideListParams
): Promise<{ data: DrugPriceOverrideResponse[]; meta: ApiMeta }> {
  const { data } = await apiClient.get<{ data: DrugPriceOverrideResponse[]; meta: ApiMeta }>(
    "/drug-price-overrides",
    { params }
  );
  return data;
}

export async function getDrugPriceOverride(id: string): Promise<DrugPriceOverrideResponse> {
  const { data } = await apiClient.get<{ data: DrugPriceOverrideResponse }>(
    `/drug-price-overrides/${id}`
  );
  return data.data;
}

export async function createDrugPriceOverride(
  body: DrugPriceOverrideCreateRequest
): Promise<DrugPriceOverrideResponse> {
  const { data } = await apiClient.post<{ data: DrugPriceOverrideResponse }>(
    "/drug-price-overrides",
    body
  );
  return data.data;
}

export async function updateDrugPriceOverride(
  id: string,
  body: PriceOverrideUpdateRequest
): Promise<DrugPriceOverrideResponse> {
  const { data } = await apiClient.put<{ data: DrugPriceOverrideResponse }>(
    `/drug-price-overrides/${id}`,
    body
  );
  return data.data;
}

export async function deleteDrugPriceOverride(id: string): Promise<void> {
  await apiClient.delete(`/drug-price-overrides/${id}`);
}
