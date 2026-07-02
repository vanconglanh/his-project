import apiClient from "./client";
import type { ApiResponse, FileUploadResponse } from "./types";

export type FileCategory = "AVATAR" | "CLS" | "CONSENT" | "EMR_ATTACHMENT";

export async function uploadFile(file: File, category?: FileCategory): Promise<FileUploadResponse> {
  const form = new FormData();
  form.append("file", file);
  if (category) form.append("category", category);

  const { data } = await apiClient.post<ApiResponse<FileUploadResponse>>(
    "/files/upload",
    form,
    { headers: { "Content-Type": "multipart/form-data" } }
  );
  return data.data;
}

export async function getSignedUrl(id: string): Promise<FileUploadResponse> {
  const { data } = await apiClient.get<ApiResponse<FileUploadResponse>>(
    `/files/${id}/signed-url`
  );
  return data.data;
}

export async function deleteFile(id: string): Promise<void> {
  await apiClient.delete(`/files/${id}`);
}
