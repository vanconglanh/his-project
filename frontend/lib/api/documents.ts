import apiClient from "./client";
import type { ApiResponse } from "./types";
import type { InBodyReportResponse } from "./inbody-reports";
import type { LabOcrExtractResult } from "./lab-results";
import type { RadOcrExtractResult } from "./rad-results";

// ─── Types ────────────────────────────────────────────────────────────────────
// Xem PRD tinh nang "Upload tai lieu thong minh" — OCR + tu phan loai + route
// sang dung luong xac nhan da co (InBody / Ket qua xet nghiem / Ket qua CDHA / Ho so cu).

export type SmartDocumentType = "InBody" | "LabResult" | "RadResult" | "Legacy" | "Unknown";

export interface SmartDocumentCandidate {
  type: SmartDocumentType;
  score: number;
  evidence: string[];
}

export interface SmartDocumentClassification {
  type: SmartDocumentType;
  confidence: number;
  evidence: string[];
  candidates: SmartDocumentCandidate[];
}

// LabOcrExtractResponse tu backend co cung shape voi LabOcrExtractResult ma
// LabResultOcrPanel dang dung (xem lib/api/lab-results.ts).
export type LabOcrExtractResponse = LabOcrExtractResult;

// RadOcrExtractResponse tu backend co cung shape voi RadOcrExtractResult ma
// RadResultOcrPanel dang dung (xem lib/api/rad-results.ts).
export type RadOcrExtractResponse = RadOcrExtractResult;

export interface SmartUploadResponse {
  classification: SmartDocumentClassification;
  requires_encounter: boolean;
  raw_text_preview: string | null;
  in_body: InBodyReportResponse | null;
  lab_result: LabOcrExtractResponse | null;
  rad_result: RadOcrExtractResponse | null;
}

// ─── Batch: nhiều file cùng lúc HOẶC 1 file ZIP ─────────────────────────────────
// Mỗi file được OCR + phân loại ĐỘC LẬP, kết quả trả theo từng file (không gộp chung).

export interface SmartUploadItemResult {
  file_name: string;
  success: boolean;
  error_code: string | null;
  error_message: string | null;
  result: SmartUploadResponse | null;
}

export interface SmartUploadBatchResponse {
  items: SmartUploadItemResult[];
}

export interface SmartUploadParams {
  files: File[];
  patientId: string;
  encounterId?: string;
}

// ─── API Functions ────────────────────────────────────────────────────────────

export async function smartUploadDocument({
  files,
  patientId,
  encounterId,
}: SmartUploadParams): Promise<SmartUploadBatchResponse> {
  const fd = new FormData();
  // Nhiều file cùng field "files" (hoặc 1 file .zip) — backend tự giải nén nếu là ZIP.
  files.forEach((f) => fd.append("files", f));
  fd.append("patient_id", patientId);
  if (encounterId) fd.append("encounter_id", encounterId);

  const res = await apiClient.post<ApiResponse<SmartUploadBatchResponse>>(
    "/documents/smart-upload",
    fd,
    { headers: { "Content-Type": "multipart/form-data" } }
  );
  return res.data.data;
}
