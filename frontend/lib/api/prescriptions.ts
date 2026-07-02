import apiClient from "./client";
import type { ApiResponse, ApiMeta } from "./types";

// ─── Types ────────────────────────────────────────────────────────────────────

export type PrescriptionStatus =
  | "DRAFT"
  | "SIGNED"
  | "SUBMITTED_DTQG"
  | "DISPENSED"
  | "PARTIAL_DISPENSED"
  | "CANCELLED";

export type DtqgStatus = "NONE" | "PENDING" | "SUBMITTED" | "ACCEPTED" | "REJECTED";

export type RouteEnum = "ORAL" | "IV" | "IM" | "SC" | "TOP" | "INH" | "OPH" | "OTIC" | "NAS" | "REC" | "OTHER";

export type DdiSeverity = "MINOR" | "MODERATE" | "MAJOR" | "CONTRAINDICATED";

export interface DdiWarning {
  drug1_id: string;
  drug1_name: string;
  drug2_id: string;
  drug2_name: string;
  severity: DdiSeverity;
  description: string;
  evidence_level: "A" | "B" | "C";
}

export interface PrescriptionItemRequest {
  drug_id: string;
  dosage: string;
  frequency: string;
  route: RouteEnum;
  duration_days: number;
  quantity: number;
  instructions?: string;
}

export interface PrescriptionItemResponse extends PrescriptionItemRequest {
  id: string;
  drug_name: string;
  strength: string;
  unit: string;
  batch_dispensed?: Array<{ batch_no: string; quantity: number }>;
}

export interface PrescriptionResponse {
  id: string;
  tenant_id: string;
  encounter_id: string;
  patient_id: string;
  patient_summary: {
    full_name: string;
    gender: string;
    dob: string;
    bhyt_no?: string;
  };
  doctor_id: string;
  doctor_name: string;
  status: PrescriptionStatus;
  prescribed_at: string;
  signed_at?: string | null;
  signed_by?: string | null;
  dtqg_code?: string | null;
  dtqg_status: DtqgStatus;
  items: PrescriptionItemResponse[];
  ddi_warnings: DdiWarning[];
  total_amount: number;
  note?: string;
  created_at: string;
  updated_at: string;
}

export interface PrescriptionDiagnosis {
  icd10_code: string;
  icd10_name: string;
  is_primary: boolean;
}

export interface PrescriptionCreateRequest {
  encounter_id?: string;
  patient_id: string;
  doctor_id?: string;
  diagnoses?: PrescriptionDiagnosis[];
  note?: string;
  items?: PrescriptionItemRequest[];
}

export interface PrescriptionListParams {
  page?: number;
  page_size?: number;
  status?: PrescriptionStatus;
  patient_id?: string;
  encounter_id?: string;
  doctor_id?: string;
  from_date?: string;
  to_date?: string;
  q?: string;
}

export interface PrescriptionListResponse {
  data: PrescriptionResponse[];
  meta: ApiMeta;
}

// ─── API calls ────────────────────────────────────────────────────────────────

export async function listPrescriptions(params?: PrescriptionListParams): Promise<PrescriptionListResponse> {
  const { data } = await apiClient.get<PrescriptionListResponse>("/prescriptions", { params });
  return data;
}

export async function getPrescription(id: string): Promise<PrescriptionResponse> {
  const { data } = await apiClient.get<ApiResponse<PrescriptionResponse>>(`/prescriptions/${id}`);
  return data.data;
}

export async function createPrescription(body: PrescriptionCreateRequest): Promise<PrescriptionResponse> {
  const { data } = await apiClient.post<ApiResponse<PrescriptionResponse>>("/prescriptions", body);
  return data.data;
}

export async function updatePrescription(id: string, body: { note?: string }): Promise<PrescriptionResponse> {
  const { data } = await apiClient.put<ApiResponse<PrescriptionResponse>>(`/prescriptions/${id}`, body);
  return data.data;
}

export async function deletePrescription(id: string): Promise<void> {
  await apiClient.delete(`/prescriptions/${id}`);
}

export async function addPrescriptionItems(
  id: string,
  items: PrescriptionItemRequest[]
): Promise<PrescriptionItemResponse[]> {
  const { data } = await apiClient.post<ApiResponse<PrescriptionItemResponse[]>>(
    `/prescriptions/${id}/items`,
    { items }
  );
  return data.data;
}

export async function removePrescriptionItem(id: string, itemId: string): Promise<void> {
  await apiClient.delete(`/prescriptions/${id}/items/${itemId}`);
}

export async function signPrescription(
  id: string,
  body: { signature_data: string; certificate_thumbprint: string; signing_time?: string }
): Promise<PrescriptionResponse> {
  const { data } = await apiClient.post<ApiResponse<PrescriptionResponse>>(`/prescriptions/${id}/sign`, body);
  return data.data;
}

export async function cancelPrescription(id: string, reason: string): Promise<PrescriptionResponse> {
  const { data } = await apiClient.post<ApiResponse<PrescriptionResponse>>(`/prescriptions/${id}/cancel`, { reason });
  return data.data;
}

export async function getDdiCheck(id: string): Promise<{ prescription_id: string; warnings: DdiWarning[]; has_contraindicated: boolean }> {
  const { data } = await apiClient.get<ApiResponse<{ prescription_id: string; warnings: DdiWarning[]; has_contraindicated: boolean }>>(`/prescriptions/${id}/ddi-check`);
  return data.data;
}

export async function getPrescriptionQrUrl(id: string): Promise<string> {
  return `${apiClient.defaults.baseURL}/prescriptions/${id}/qr`;
}

export async function printPrescriptionPdf(id: string): Promise<void> {
  const url = `${apiClient.defaults.baseURL}/prescriptions/${id}/pdf`;
  window.open(url, "_blank");
}
