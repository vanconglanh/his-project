import apiClient from "./client";
import type { ApiResponse, VitalSignsRequest, VitalSignsResponse } from "./types";

export async function createVitalSign(encounterId: string, body: VitalSignsRequest) {
  const res = await apiClient.post<ApiResponse<VitalSignsResponse>>(
    `/encounters/${encounterId}/vital-signs`,
    body
  );
  return res.data.data;
}

export async function listVitalSigns(encounterId: string) {
  const res = await apiClient.get<ApiResponse<VitalSignsResponse[]>>(
    `/encounters/${encounterId}/vital-signs`
  );
  return res.data.data;
}

export async function getLatestVitalSign(encounterId: string) {
  const res = await apiClient.get<ApiResponse<VitalSignsResponse>>(
    `/encounters/${encounterId}/vital-signs/latest`
  );
  return res.data.data;
}

export async function updateVitalSign(id: string, body: VitalSignsRequest) {
  await apiClient.put(`/vital-signs/${id}`, body);
}

export async function deleteVitalSign(id: string) {
  await apiClient.delete(`/vital-signs/${id}`);
}

export async function getVitalSignHistory(
  patientId: string,
  params?: { date_from?: string; date_to?: string; metric?: string }
) {
  const res = await apiClient.get<ApiResponse<VitalSignsResponse[]>>(
    `/patients/${patientId}/vital-signs/history`,
    { params }
  );
  return res.data.data;
}
