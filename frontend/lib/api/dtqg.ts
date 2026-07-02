import apiClient from "./client";
import type { ApiResponse, ApiMeta } from "./types";

// ─── Types ────────────────────────────────────────────────────────────────────

export interface DtqgSubmissionResponse {
  id: string;
  prescription_id: string;
  ma_don_thuoc?: string;
  qr_payload?: string;
  qr_image_url?: string;
  status: "PENDING" | "SUBMITTED" | "ACCEPTED" | "REJECTED";
  error_code?: string | null;
  error_message?: string | null;
  submitted_at?: string | null;
  accepted_at?: string | null;
  retry_count: number;
  last_retry_at?: string | null;
}

export interface DtqgCredentialsRequest {
  cskcb_id: string;
  partner_code: string;
  token: string;
}

export interface DtqgCredentialsResponse {
  id: string;
  tenant_id: string;
  cskcb_id: string;
  partner_code: string;
  token_masked: string;
  is_active: boolean;
  last_tested_at?: string | null;
  last_test_ok?: boolean;
}

export interface DtqgSubmissionListParams {
  status?: "PENDING" | "SUBMITTED" | "ACCEPTED" | "REJECTED";
  from_date?: string;
  to_date?: string;
  page?: number;
  page_size?: number;
}

// ─── API calls ────────────────────────────────────────────────────────────────

export async function submitToDtqg(prescriptionId: string): Promise<DtqgSubmissionResponse> {
  const { data } = await apiClient.post<ApiResponse<DtqgSubmissionResponse>>(
    `/prescriptions/${prescriptionId}/dtqg/submit`
  );
  return data.data;
}

export async function getDtqgStatus(prescriptionId: string): Promise<DtqgSubmissionResponse> {
  const { data } = await apiClient.get<ApiResponse<DtqgSubmissionResponse>>(
    `/prescriptions/${prescriptionId}/dtqg/status`
  );
  return data.data;
}

export async function retryDtqgSubmission(prescriptionId: string): Promise<DtqgSubmissionResponse> {
  const { data } = await apiClient.post<ApiResponse<DtqgSubmissionResponse>>(
    `/prescriptions/${prescriptionId}/dtqg/retry`
  );
  return data.data;
}

export async function listDtqgSubmissions(
  params?: DtqgSubmissionListParams
): Promise<{ data: DtqgSubmissionResponse[]; meta: ApiMeta }> {
  const { data } = await apiClient.get<{ data: DtqgSubmissionResponse[]; meta: ApiMeta }>(
    "/dtqg/submissions",
    { params }
  );
  return data;
}

export async function cancelOnPortal(submissionId: string, reason: string): Promise<DtqgSubmissionResponse> {
  const { data } = await apiClient.post<ApiResponse<DtqgSubmissionResponse>>(
    `/dtqg/submissions/${submissionId}/cancel-on-portal`,
    { reason }
  );
  return data.data;
}

export async function getDtqgCredentials(): Promise<DtqgCredentialsResponse> {
  const { data } = await apiClient.get<ApiResponse<DtqgCredentialsResponse>>("/dtqg/credentials");
  return data.data;
}

export async function upsertDtqgCredentials(body: DtqgCredentialsRequest): Promise<DtqgCredentialsResponse> {
  const { data } = await apiClient.put<ApiResponse<DtqgCredentialsResponse>>("/dtqg/credentials", body);
  return data.data;
}

export async function testDtqgConnection(): Promise<{ ok: boolean; latency_ms: number; portal_response: string }> {
  const { data } = await apiClient.post<ApiResponse<{ ok: boolean; latency_ms: number; portal_response: string }>>(
    "/dtqg/credentials/test"
  );
  return data.data;
}
