import apiClient from "./client";
import type { ApiResponse, ApiMeta } from "./types";

// ─── Types ────────────────────────────────────────────────────────────────────

export type DrugForm =
  | "TABLET"
  | "CAPSULE"
  | "SYRUP"
  | "INJ"
  | "CREAM"
  | "OINTMENT"
  | "DROP"
  | "INHALER"
  | "POWDER"
  | "SUPPOSITORY"
  | "OTHER";

export interface DrugMasterRequest {
  code: string;
  name_vi: string;
  name_en?: string;
  generic_name?: string;
  atc_code?: string;
  strength?: string;
  unit: string;
  form: DrugForm;
  manufacturer?: string;
  country?: string;
  price?: number;
  category_id?: string | null;
  requires_prescription?: boolean;
  is_psychotropic?: boolean;
  is_narcotic?: boolean;
  dtqg_drug_code?: string;
  status?: "ACTIVE" | "INACTIVE";
}

export interface DrugMasterResponse extends DrugMasterRequest {
  id: string;
  tenant_id: string;
  interactions_count?: number;
  created_at: string;
  updated_at: string;
}

export interface DrugCategory {
  id: string;
  code: string;
  name: string;
  parent_id?: string | null;
}

export interface DdiRule {
  id: string;
  drug1_id: string;
  drug1_name: string;
  drug2_id: string;
  drug2_name: string;
  severity: "MINOR" | "MODERATE" | "MAJOR" | "CONTRAINDICATED";
  description: string;
  evidence_level: "A" | "B" | "C";
}

export interface DrugListParams {
  page?: number;
  page_size?: number;
  q?: string;
  status?: "ACTIVE" | "INACTIVE";
  requires_prescription?: boolean;
  atc_code?: string;
  category_id?: string;
}

export interface DrugImportResult {
  total_rows: number;
  inserted: number;
  updated: number;
  failed: number;
  errors: Array<{ row: number; message: string }>;
}

// ─── API calls ────────────────────────────────────────────────────────────────

export async function listDrugs(
  params?: DrugListParams
): Promise<{ data: DrugMasterResponse[]; meta: ApiMeta }> {
  const { data } = await apiClient.get<{ data: DrugMasterResponse[]; meta: ApiMeta }>("/drugs", { params });
  return data;
}

export async function searchDrugs(q: string, limit = 20): Promise<DrugMasterResponse[]> {
  const { data } = await apiClient.get<ApiResponse<DrugMasterResponse[]>>("/drugs/search", {
    params: { q, limit },
  });
  return data.data;
}

export async function getDrug(id: string): Promise<DrugMasterResponse> {
  const { data } = await apiClient.get<ApiResponse<DrugMasterResponse>>(`/drugs/${id}`);
  return data.data;
}

export async function createDrug(body: DrugMasterRequest): Promise<DrugMasterResponse> {
  const { data } = await apiClient.post<ApiResponse<DrugMasterResponse>>("/drugs", body);
  return data.data;
}

export async function updateDrug(id: string, body: DrugMasterRequest): Promise<DrugMasterResponse> {
  const { data } = await apiClient.put<ApiResponse<DrugMasterResponse>>(`/drugs/${id}`, body);
  return data.data;
}

export async function deleteDrug(id: string): Promise<void> {
  await apiClient.delete(`/drugs/${id}`);
}

export async function importDrugsExcel(file: File, mode: "INSERT" | "UPSERT" = "UPSERT"): Promise<DrugImportResult> {
  const form = new FormData();
  form.append("file", file);
  form.append("mode", mode);
  const { data } = await apiClient.post<ApiResponse<DrugImportResult>>("/drugs/import", form, {
    headers: { "Content-Type": "multipart/form-data" },
  });
  return data.data;
}

export async function getEquivalentDrugs(id: string): Promise<DrugMasterResponse[]> {
  const { data } = await apiClient.get<ApiResponse<DrugMasterResponse[]>>(`/drugs/${id}/equivalents`);
  return data.data;
}

export async function getDrugInteractions(id: string): Promise<DdiRule[]> {
  const { data } = await apiClient.get<ApiResponse<DdiRule[]>>(`/drugs/${id}/interactions`);
  return data.data;
}

export async function listDrugCategories(): Promise<DrugCategory[]> {
  const { data } = await apiClient.get<ApiResponse<DrugCategory[]>>("/drugs/categories");
  return data.data;
}

export async function createDrugCategory(body: {
  code: string;
  name: string;
  parent_id?: string | null;
}): Promise<DrugCategory> {
  const { data } = await apiClient.post<ApiResponse<DrugCategory>>("/drugs/categories", body);
  return data.data;
}

export async function syncCucQld(mode: "FULL" | "INCREMENTAL" = "INCREMENTAL", since?: string): Promise<{ job_id: string }> {
  const { data } = await apiClient.post<ApiResponse<{ job_id: string }>>("/drugs/sync-cuc-qld", { mode, since });
  return data.data;
}
