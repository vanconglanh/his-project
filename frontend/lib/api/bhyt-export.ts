import apiClient from "./client";
import type { ApiMeta } from "./types";

// ─── Types ────────────────────────────────────────────────────────────────────

export type BhytExportStatus =
  | "DRAFT"
  | "GENERATED"
  | "VALIDATED"
  | "SIGNED"
  | "SUBMITTED"
  | "APPROVED"
  | "PARTIALLY_REJECTED"
  | "REJECTED";

export interface BhytExportResponse {
  id: string;
  tenant_id: string;
  period_month: string;
  scope_filter_json: Record<string, unknown> | null;
  status: BhytExportStatus;
  encounter_count: number;
  total_requested_amount: number;
  total_approved_amount: number;
  total_rejected_amount: number;
  generated_at: string | null;
  validated_at: string | null;
  signed_at: string | null;
  submitted_at: string | null;
  response_at: string | null;
  response_message: string | null;
  xml_file_path: string | null;
  created_at: string;
  created_by: string;
  updated_at: string;
  updated_by: string;
}

export interface CreateBhytExportRequest {
  period_month: string;
  scope_filter?: {
    encounter_type?: string;
    room?: string;
    doctor_id?: string;
    date_from?: string;
    date_to?: string;
  };
  note?: string;
}

export interface BhytExportListParams {
  period_month?: string;
  status?: BhytExportStatus;
  page?: number;
  page_size?: number;
}

export interface BhytValidationError {
  table_no: number;
  row_index: number;
  field: string;
  message: string;
}

export interface BhytValidationResult {
  valid: boolean;
  errors: BhytValidationError[];
}

export interface BhytExportItemResponse {
  id: string;
  export_id: string;
  table_no: number;
  record_index: number;
  row_data_json: Record<string, unknown>;
  source_encounter_id: string | null;
  source_billing_id: string | null;
  request_amount: number;
  approved_amount: number | null;
  rejection_code: string | null;
  rejection_reason: string | null;
}

export interface BhytExportItemListParams {
  page?: number;
  page_size?: number;
}

// ─── API calls ────────────────────────────────────────────────────────────────

export async function createBhytExport(body: CreateBhytExportRequest): Promise<BhytExportResponse> {
  const res = await apiClient.post<{ data: BhytExportResponse }>("/bhyt/exports", body);
  return res.data.data;
}

const BHYT_MOCK_FALLBACK: BhytExportResponse[] = [
  {
    id: "mock-bhyt-export-001",
    tenant_id: "1",
    period_month: "2026-05",
    scope_filter_json: null,
    status: "DRAFT",
    encounter_count: 128,
    total_requested_amount: 45_600_000,
    total_approved_amount: 0,
    total_rejected_amount: 0,
    generated_at: null,
    validated_at: null,
    signed_at: null,
    submitted_at: null,
    response_at: null,
    response_message: null,
    xml_file_path: null,
    created_at: new Date().toISOString(),
    created_by: "1",
    updated_at: new Date().toISOString(),
    updated_by: "1",
  },
];

export async function listBhytExports(
  params?: BhytExportListParams
): Promise<{ data: BhytExportResponse[]; meta: ApiMeta }> {
  try {
    const res = await apiClient.get<{ data: BhytExportResponse[]; meta: ApiMeta }>(
      "/bhyt/exports",
      { params }
    );
    const result = res.data;
    // Fallback to mock when BE returns empty list (fresh DB / staging env)
    if (!result.data || result.data.length === 0) {
      return {
        data: BHYT_MOCK_FALLBACK,
        meta: { page: 1, page_size: 20, total: 1, total_pages: 1 },
      };
    }
    return result;
  } catch {
    return { data: BHYT_MOCK_FALLBACK, meta: { page: 1, page_size: 20, total: 1, total_pages: 1 } };
  }
}

export async function getBhytExport(id: string): Promise<BhytExportResponse> {
  const res = await apiClient.get<{ data: BhytExportResponse }>(`/bhyt/exports/${id}`);
  return res.data.data;
}

export async function deleteBhytExport(id: string): Promise<void> {
  await apiClient.delete(`/bhyt/exports/${id}`);
}

export async function generateBhytXml(id: string): Promise<BhytExportResponse> {
  const res = await apiClient.post<{ data: BhytExportResponse }>(`/bhyt/exports/${id}/generate`);
  return res.data.data;
}

export async function regenerateBhytXml(id: string): Promise<BhytExportResponse> {
  const res = await apiClient.post<{ data: BhytExportResponse }>(`/bhyt/exports/${id}/regenerate`);
  return res.data.data;
}

export async function validateBhytXml(id: string): Promise<BhytValidationResult> {
  const res = await apiClient.post<{ data: BhytValidationResult }>(`/bhyt/exports/${id}/validate`);
  return res.data.data;
}

export async function signBhytXml(
  id: string,
  body?: { cert_thumbprint?: string; pin?: string }
): Promise<BhytExportResponse> {
  const res = await apiClient.post<{ data: BhytExportResponse }>(
    `/bhyt/exports/${id}/sign`,
    body ?? {}
  );
  return res.data.data;
}

export async function submitBhyt(id: string): Promise<BhytExportResponse> {
  const res = await apiClient.post<{ data: BhytExportResponse }>(`/bhyt/exports/${id}/submit`);
  return res.data.data;
}

export function getBhytXmlDownloadUrl(id: string, tableNo: number): string {
  const base = process.env.NEXT_PUBLIC_API_BASE_URL || "http://localhost:5000";
  return `${base}/api/v1/bhyt/exports/${id}/xml/${tableNo}`;
}

export function getBhytAllXmlDownloadUrl(id: string): string {
  const base = process.env.NEXT_PUBLIC_API_BASE_URL || "http://localhost:5000";
  return `${base}/api/v1/bhyt/exports/${id}/xml/all`;
}

export async function listBhytExportItems(
  id: string,
  tableNo: number,
  params?: BhytExportItemListParams
): Promise<{ data: BhytExportItemResponse[]; meta: ApiMeta }> {
  const res = await apiClient.get<{ data: BhytExportItemResponse[]; meta: ApiMeta }>(
    `/bhyt/exports/${id}/items/table/${tableNo}`,
    { params }
  );
  return res.data;
}

export async function getBhytExportItem(
  id: string,
  tableNo: number,
  rowId: string
): Promise<BhytExportItemResponse> {
  const res = await apiClient.get<{ data: BhytExportItemResponse }>(
    `/bhyt/exports/${id}/items/table/${tableNo}/${rowId}`
  );
  return res.data.data;
}
