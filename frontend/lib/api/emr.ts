import apiClient from "./client";
import type {
  ApiResponse,
  EmrSaveRequest,
  EmrContentResponse,
  SignEmrRequest,
  EmrVersionMeta,
  EmrTemplateRequest,
  EmrTemplateResponse,
} from "./types";

export async function getEmr(encounterId: string) {
  const res = await apiClient.get<ApiResponse<EmrContentResponse>>(
    `/encounters/${encounterId}/emr`
  );
  return res.data.data;
}

export async function saveEmrDraft(encounterId: string, body: EmrSaveRequest) {
  const res = await apiClient.put<ApiResponse<EmrContentResponse>>(
    `/encounters/${encounterId}/emr`,
    body
  );
  return res.data.data;
}

export async function signEmr(encounterId: string, body: SignEmrRequest) {
  const res = await apiClient.post<ApiResponse<EmrContentResponse>>(
    `/encounters/${encounterId}/emr/sign`,
    body
  );
  return res.data.data;
}

export async function unsignEmr(encounterId: string, reason: string) {
  await apiClient.post(`/encounters/${encounterId}/emr/unsign`, { reason });
}

export async function getEmrVersions(encounterId: string) {
  const res = await apiClient.get<ApiResponse<EmrVersionMeta[]>>(
    `/encounters/${encounterId}/emr/versions`
  );
  return res.data.data;
}

export async function getEmrVersionDiff(
  encounterId: string,
  versionId: string,
  compareTo?: string
) {
  const res = await apiClient.get<ApiResponse<{ ops: unknown[] }>>(
    `/encounters/${encounterId}/emr/versions/${versionId}/diff`,
    { params: { compare_to: compareTo } }
  );
  return res.data.data;
}

export async function listEmrTemplates(params?: { speciality?: string; is_system?: boolean }) {
  const res = await apiClient.get<ApiResponse<EmrTemplateResponse[]>>("/emr-templates", { params });
  return res.data.data;
}

export async function createEmrTemplate(body: EmrTemplateRequest) {
  const res = await apiClient.post<ApiResponse<EmrTemplateResponse>>("/emr-templates", body);
  return res.data.data;
}

export async function updateEmrTemplate(id: string, body: EmrTemplateRequest) {
  await apiClient.put(`/emr-templates/${id}`, body);
}

export async function deleteEmrTemplate(id: string) {
  await apiClient.delete(`/emr-templates/${id}`);
}
