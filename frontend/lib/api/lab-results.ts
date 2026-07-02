import apiClient from "./client";
import type { ApiResponse, ApiMeta } from "./types";

// ─── Types ────────────────────────────────────────────────────────────────────

export type LabResultFlag = "NORMAL" | "H" | "L" | "HH" | "LL" | "CRITICAL";
export type LabResultStatus = "DRAFT" | "VERIFIED" | "AMENDED";
export type LabResultSource = "MANUAL" | "IMPORT" | "PARTNER";

export interface LabResultResponse {
  id: string;
  lab_order_id: string;
  lab_order_item_id: string;
  patient_id: string;
  encounter_id: string;
  test_code: string;
  test_name: string;
  value: string;
  value_numeric: number | null;
  unit: string;
  reference_range_low: number | null;
  reference_range_high: number | null;
  flag: LabResultFlag;
  method: string | null;
  performed_at: string;
  performed_by: string;
  status: LabResultStatus;
  verified_at: string | null;
  verified_by: string | null;
  note: string | null;
  source: LabResultSource;
  created_at: string;
  updated_at: string;
}

export interface LabResultCreateRequest {
  lab_order_item_id: string;
  value: string;
  value_numeric?: number | null;
  unit?: string | null;
  method?: string | null;
  performed_at: string;
  note?: string | null;
}

export interface LabResultUpdateRequest {
  value?: string;
  value_numeric?: number | null;
  unit?: string | null;
  method?: string | null;
  note?: string | null;
  amend_reason?: string;
}

export interface LabResultListParams {
  patient_id?: string;
  encounter_id?: string;
  lab_order_id?: string;
  status?: LabResultStatus;
  flag?: LabResultFlag;
  from_date?: string;
  to_date?: string;
  page?: number;
  page_size?: number;
}

export interface LabResultListResponse {
  data: LabResultResponse[];
  meta: ApiMeta;
}

export interface LabTrendPoint {
  performed_at: string;
  value_numeric: number;
  flag: LabResultFlag;
}

export interface LabTrendResponse {
  test_code: string;
  test_name: string;
  unit: string;
  reference_range_low: number | null;
  reference_range_high: number | null;
  points: LabTrendPoint[];
}

export interface LabImportResult {
  total_rows: number;
  success_count: number;
  failed_count: number;
  errors: { row: number; message: string }[];
}

export interface BatchVerifyResult {
  success_count: number;
  failed_count: number;
  errors: { id: string; code: string; message: string }[];
}

// ─── API Functions ────────────────────────────────────────────────────────────

export async function listLabResults(params?: LabResultListParams): Promise<LabResultListResponse> {
  const res = await apiClient.get<LabResultListResponse>("/lab-results", { params });
  return res.data;
}

export async function createLabResult(body: LabResultCreateRequest): Promise<LabResultResponse> {
  const res = await apiClient.post<ApiResponse<LabResultResponse>>("/lab-results", body);
  return res.data.data;
}

export async function updateLabResult(id: string, body: LabResultUpdateRequest): Promise<LabResultResponse> {
  const res = await apiClient.put<ApiResponse<LabResultResponse>>(`/lab-results/${id}`, body);
  return res.data.data;
}

export async function verifyLabResult(id: string): Promise<void> {
  await apiClient.post(`/lab-results/${id}/verify`);
}

export async function unverifyLabResult(id: string): Promise<void> {
  await apiClient.post(`/lab-results/${id}/unverify`);
}

export function getLabResultPdfUrl(id: string): string {
  return `${apiClient.defaults.baseURL}/lab-results/${id}/pdf`;
}

export async function importLabResults(
  file: File,
  format: "CSV" | "HL7_ORU",
  auto_verify = false
): Promise<LabImportResult> {
  const fd = new FormData();
  fd.append("file", file);
  fd.append("format", format);
  fd.append("auto_verify", String(auto_verify));
  const res = await apiClient.post<ApiResponse<LabImportResult>>("/lab-results/import", fd, {
    headers: { "Content-Type": "multipart/form-data" },
  });
  return res.data.data;
}

export async function listAbnormalLabResults(params?: {
  severity?: "ALL" | "CRITICAL_ONLY";
  from_date?: string;
  to_date?: string;
}): Promise<LabResultResponse[]> {
  const res = await apiClient.get<ApiResponse<LabResultResponse[]>>("/lab-results/abnormal", { params });
  return res.data.data;
}

export async function getLabResultTrend(params: {
  patient_id: string;
  test_code: string;
  from_date?: string;
  to_date?: string;
}): Promise<LabTrendResponse> {
  const res = await apiClient.get<ApiResponse<LabTrendResponse>>("/lab-results/history-trend", { params });
  return res.data.data;
}

export async function batchVerifyLabResults(result_ids: string[]): Promise<BatchVerifyResult> {
  const res = await apiClient.post<ApiResponse<BatchVerifyResult>>("/lab-results/batch-verify", { result_ids });
  return res.data.data;
}
