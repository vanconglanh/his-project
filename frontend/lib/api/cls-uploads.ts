import apiClient from "./client";
import type { ApiResponse, ApiMeta, ClsUploadResponse } from "./types";

export interface ClsUploadListParams {
  page?: number;
  page_size?: number;
  doc_type?: string;
}

export interface ClsUploadListResponse {
  data: ClsUploadResponse[];
  meta: ApiMeta;
}

export async function listClsUploads(
  patientId: string,
  params?: ClsUploadListParams
): Promise<ClsUploadListResponse> {
  const { data } = await apiClient.get<ClsUploadListResponse>(
    `/patients/${patientId}/cls-uploads`,
    { params }
  );
  return data;
}

export async function uploadCls(
  patientId: string,
  file: File,
  docType: string,
  encounterId?: string,
  note?: string
): Promise<ClsUploadResponse> {
  const form = new FormData();
  form.append("file", file);
  form.append("doc_type", docType);
  if (encounterId) form.append("encounter_id", encounterId);
  if (note) form.append("note", note);

  const { data } = await apiClient.post<ApiResponse<ClsUploadResponse>>(
    `/patients/${patientId}/cls-uploads`,
    form,
    { headers: { "Content-Type": "multipart/form-data" } }
  );
  return data.data;
}

export async function getClsUpload(patientId: string, id: string): Promise<ClsUploadResponse> {
  const { data } = await apiClient.get<ApiResponse<ClsUploadResponse>>(
    `/patients/${patientId}/cls-uploads/${id}`
  );
  return data.data;
}

export async function deleteClsUpload(patientId: string, id: string): Promise<void> {
  await apiClient.delete(`/patients/${patientId}/cls-uploads/${id}`);
}

// BM-26 (Lark Bug-Feedback): "Không mở được Tệp tin đã upload" - signed_url (MinIO presigned
// URL) tro toi endpoint NOI BO cua docker network (vd "minio:9000"), trinh duyet nguoi dung
// khong resolve duoc hostname nay (chi backend, dang o trong cung docker network, moi goi
// duoc). Fix: fetch noi dung file qua chinh API backend (da public qua nginx, dung JWT auth
// co san cua apiClient) roi tao Blob URL cuc bo de <img>/lightbox/link "Mo file" dung, thay vi
// tro thang toi signed_url tra ve tu BE.
export async function getClsUploadContentBlobUrl(patientId: string, id: string): Promise<string> {
  const { data } = await apiClient.get(`/patients/${patientId}/cls-uploads/${id}/content`, {
    responseType: "blob",
  });
  return URL.createObjectURL(data as Blob);
}

export async function listEncounterClsUploads(encounterId: string): Promise<ClsUploadResponse[]> {
  const { data } = await apiClient.get<ApiResponse<ClsUploadResponse[]>>(
    `/encounters/${encounterId}/cls-uploads`
  );
  return data.data;
}
