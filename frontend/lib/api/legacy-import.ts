import apiClient from "./client";
import type { ApiResponse, ApiMeta } from "./types";

// ─── Types ────────────────────────────────────────────────────────────────────
// Nhap lieu hang loat ho so giay cu dang anh scan: admin upload ZIP -> backend OCR
// nen -> admin review tung anh (sua text OCR, match benh nhan) -> confirm luu thanh
// tai lieu dinh kem ho so benh nhan (doc_type='HO_SO_CU_SCAN' trong cls-uploads).

export type LegacyImportBatchStatus = "pending" | "processing" | "done" | "failed";

export type LegacyImportItemStatus =
  | "pending_match"
  | "pending_review"
  | "confirmed"
  | "rejected"
  | "failed";

export type LegacyImportMatchMethod = "filename_auto" | "manual" | null;

export interface LegacyImportBatch {
  id: string;
  zip_file_name: string;
  total_items: number;
  processed_items: number;
  status: LegacyImportBatchStatus;
  created_at: string;
}

export interface LegacyImportItem {
  id: string;
  batch_id: string;
  original_filename: string;
  image_url: string | null;
  ocr_text: string | null;
  ocr_confidence: number | null;
  matched_patient_id: string | null;
  matched_patient_name: string | null;
  match_method: LegacyImportMatchMethod;
  status: LegacyImportItemStatus;
  item_error: string | null;
  confirmed_at: string | null;
}

export interface LegacyImportBatchListResponse {
  data: LegacyImportBatch[];
  meta: ApiMeta;
}

export interface LegacyImportItemListResponse {
  data: LegacyImportItem[];
  meta: ApiMeta;
}

export interface LegacyImportItemListParams {
  status?: LegacyImportItemStatus;
  page?: number;
  page_size?: number;
}

// ─── API Functions ────────────────────────────────────────────────────────────

export async function uploadLegacyImportBatch(file: File): Promise<LegacyImportBatch> {
  const form = new FormData();
  form.append("file", file);

  const { data } = await apiClient.post<ApiResponse<LegacyImportBatch>>(
    "/legacy-imports",
    form,
    { headers: { "Content-Type": "multipart/form-data" } }
  );
  return data.data;
}

export async function listLegacyImportBatches(params?: {
  page?: number;
  page_size?: number;
}): Promise<LegacyImportBatchListResponse> {
  const { data } = await apiClient.get<LegacyImportBatchListResponse>("/legacy-imports", {
    params,
  });
  return data;
}

export async function getLegacyImportBatch(id: string): Promise<LegacyImportBatch> {
  const { data } = await apiClient.get<ApiResponse<LegacyImportBatch>>(
    `/legacy-imports/${id}`
  );
  return data.data;
}

export async function listLegacyImportItems(
  batchId: string,
  params?: LegacyImportItemListParams
): Promise<LegacyImportItemListResponse> {
  const { data } = await apiClient.get<LegacyImportItemListResponse>(
    `/legacy-imports/${batchId}/items`,
    { params }
  );
  return data;
}

export async function matchLegacyImportItem(
  itemId: string,
  patientId: string
): Promise<LegacyImportItem> {
  const { data } = await apiClient.put<ApiResponse<LegacyImportItem>>(
    `/legacy-imports/items/${itemId}/match`,
    { patient_id: patientId }
  );
  return data.data;
}

export async function confirmLegacyImportItem(
  itemId: string,
  body: { ocr_text?: string; patient_id?: string }
): Promise<LegacyImportItem> {
  const { data } = await apiClient.post<ApiResponse<LegacyImportItem>>(
    `/legacy-imports/items/${itemId}/confirm`,
    body
  );
  return data.data;
}

export async function rejectLegacyImportItem(itemId: string): Promise<LegacyImportItem> {
  const { data } = await apiClient.post<ApiResponse<LegacyImportItem>>(
    `/legacy-imports/items/${itemId}/reject`
  );
  return data.data;
}
