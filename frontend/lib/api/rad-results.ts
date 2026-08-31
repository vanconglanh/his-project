import apiClient from "./client";
import type { ApiResponse } from "./types";

// ─── Types ────────────────────────────────────────────────────────────────────

export type RadResultStatus = "DRAFT" | "VERIFIED" | "AMENDED";

export interface RadResultResponse {
  id: string;
  rad_order_id: string;
  patient_id: string;
  encounter_id: string;
  modality: string;
  findings: string;
  impression: string | null;
  conclusion: string;
  recommendations: string | null;
  performed_at: string;
  performed_by: string;
  status: RadResultStatus;
  verified_at: string | null;
  verified_by: string | null;
  dicom_count: number;
  signed_pdf_url: string | null;
  created_at: string;
  /** URL ky (signed) toi file nguon da dung de doc OCR ra ket qua nay — null neu nhap tay. */
  source_file_url?: string | null;
}

export interface RadResultCreateRequest {
  rad_order_id: string;
  findings: string;
  impression?: string | null;
  conclusion: string;
  recommendations?: string | null;
  performed_at: string;
}

export interface RadResultUpdateRequest {
  findings?: string;
  impression?: string | null;
  conclusion?: string;
  recommendations?: string | null;
  amend_reason?: string | null;
}

export interface RadResultListParams {
  patient_id?: string;
  encounter_id?: string;
  rad_order_id?: string;
  status?: RadResultStatus;
  page?: number;
  page_size?: number;
}

export interface DicomUploadResult {
  uploaded_count: number;
  total_size_bytes: number;
}

// ─── OCR đọc phiếu kết quả CĐHA (X-quang/Siêu âm/CT) ─────────────────────────────
// Trích 2 đoạn văn bản chính (Mô tả / Kết luận) từ file PDF/ảnh, cho sửa tay rồi lưu.
// LƯU Ý: response backend dùng snake_case (đã verify E2E) — KHÔNG dùng camelCase.
export interface RadOcrExtractResult {
  findings: string | null;
  impression: string | null;
  conclusion: string | null;
  recommendations: string | null;
  has_any_extracted: boolean;
  raw_text: string;
  /** GAP-8: dinh danh file nguon da upload de OCR — thread lai khi confirm de backend luu source_file_id. */
  source_file_id?: string | null;
}

export interface RadOcrConfirmRequest {
  rad_order_id: string;
  findings: string;
  impression?: string | null;
  conclusion: string;
  recommendations?: string | null;
  performed_at?: string;
  /** GAP-8: thread lai source_file_id tu buoc extract. */
  source_file_id?: string | null;
  /** GAP-2: text OCR GOC (truoc khi sua tay) — backend luu de doi chieu/diff. */
  ocr_raw_text?: string | null;
}

export interface RadOcrConfirmResult {
  id: string;
  status: RadResultStatus;
}

// ─── API Functions ────────────────────────────────────────────────────────────

export async function listRadResults(params?: RadResultListParams): Promise<RadResultResponse[]> {
  const res = await apiClient.get<ApiResponse<RadResultResponse[]>>("/rad-results", { params });
  return res.data.data;
}

export async function createRadResult(body: RadResultCreateRequest): Promise<RadResultResponse> {
  const res = await apiClient.post<ApiResponse<RadResultResponse>>("/rad-results", body);
  return res.data.data;
}

export async function updateRadResult(id: string, body: RadResultUpdateRequest): Promise<void> {
  await apiClient.put(`/rad-results/${id}`, body);
}

export async function verifyRadResult(id: string): Promise<{ signed_pdf_url: string }> {
  const res = await apiClient.post<ApiResponse<{ signed_pdf_url: string }>>(`/rad-results/${id}/verify`);
  return res.data.data;
}

export async function uploadDicomFiles(id: string, files: File[]): Promise<DicomUploadResult> {
  const fd = new FormData();
  files.forEach((f) => fd.append("files", f));
  const res = await apiClient.post<ApiResponse<DicomUploadResult>>(
    `/rad-results/${id}/dicom-upload`,
    fd,
    { headers: { "Content-Type": "multipart/form-data" } }
  );
  return res.data.data;
}

export function getRadResultPdfUrl(id: string): string {
  return `${apiClient.defaults.baseURL}/rad-results/${id}/pdf`;
}

export async function ocrExtractRadResult(file: File): Promise<RadOcrExtractResult> {
  const fd = new FormData();
  fd.append("file", file);
  const res = await apiClient.post<ApiResponse<RadOcrExtractResult>>("/rad-results/ocr-extract", fd, {
    headers: { "Content-Type": "multipart/form-data" },
  });
  return res.data.data;
}

export async function ocrConfirmRadResult(body: RadOcrConfirmRequest): Promise<RadOcrConfirmResult> {
  const res = await apiClient.post<ApiResponse<RadOcrConfirmResult>>("/rad-results/ocr-confirm", body);
  return res.data.data;
}
