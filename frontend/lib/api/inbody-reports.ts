import apiClient from "./client";
import type { ApiResponse, ApiMeta } from "./types";

// ─── Types ────────────────────────────────────────────────────────────────────
// Xem PRD docs/prd/inbody-ocr-20260830.md muc 5 — bang mapping label -> indicator_type.
// MVP CHI doc text layer PDF (UglyToad.PdfPig), KHONG OCR anh.

export type InBodyExtractionStatus = "pending" | "success" | "partial" | "failed";

export type InBodyIndicatorType =
  | "WEIGHT_KG"
  | "BMI"
  | "SMM"
  | "BODY_FAT_MASS"
  | "PBF"
  | "VISCERAL_FAT"
  | "TBW"
  | "BMR"
  | "INBODY_SCORE";

export interface InBodyFieldDto {
  indicator_type: InBodyIndicatorType;
  value: number | null;
  unit: string | null;
  extracted: boolean;
}

export interface InBodyReportResponse {
  id: string;
  patient_id: string;
  encounter_id: string | null;
  extraction_status: InBodyExtractionStatus;
  file_url: string | null;
  fields: InBodyFieldDto[];
  confirmed_by: string | null;
  confirmed_at: string | null;
  created_at: string;
}

export interface InBodyReportListResponse {
  data: InBodyReportResponse[];
  meta: ApiMeta;
}

export interface ConfirmInBodyFieldItem {
  indicator_type: InBodyIndicatorType;
  value: number | null;
  unit: string | null;
  include: boolean;
}

// ─── API Functions ────────────────────────────────────────────────────────────

export async function uploadInBodyReport(
  patientId: string,
  file: File,
  encounterId?: string
): Promise<InBodyReportResponse> {
  const form = new FormData();
  form.append("file", file);
  if (encounterId) form.append("encounter_id", encounterId);

  const { data } = await apiClient.post<ApiResponse<InBodyReportResponse>>(
    `/patients/${patientId}/inbody-reports`,
    form,
    { headers: { "Content-Type": "multipart/form-data" } }
  );
  return data.data;
}

export async function listInBodyReports(
  patientId: string,
  params?: { page?: number; page_size?: number }
): Promise<InBodyReportListResponse> {
  const { data } = await apiClient.get<InBodyReportListResponse>(
    `/patients/${patientId}/inbody-reports`,
    { params }
  );
  return data;
}

export async function confirmInBodyReport(
  id: string,
  body: { encounter_id?: string; fields: ConfirmInBodyFieldItem[] }
): Promise<InBodyReportResponse> {
  const { data } = await apiClient.post<ApiResponse<InBodyReportResponse>>(
    `/inbody-reports/${id}/confirm`,
    body
  );
  return data.data;
}
