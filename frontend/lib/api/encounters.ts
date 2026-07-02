import apiClient from "./client";
import type {
  ApiResponse,
  ApiMeta,
  EncounterResponse,
  EncounterDetailResponse,
  EncounterCreateRequest,
  EncounterUpdateRequest,
  DiagnosisRequest,
  DiagnosisResponse,
  TimelineEvent,
  Over12hAlert,
} from "./types";

export interface ListEncountersParams {
  patient_id?: string;
  doctor_id?: string;
  room_id?: string;
  status?: string;
  encounter_type?: string;
  date_from?: string;
  date_to?: string;
  page?: number;
  page_size?: number;
}

export async function listEncounters(params?: ListEncountersParams) {
  const res = await apiClient.get<{ data: EncounterResponse[]; meta: ApiMeta }>(
    "/encounters",
    { params }
  );
  return res.data;
}

export async function createEncounter(body: EncounterCreateRequest) {
  const res = await apiClient.post<ApiResponse<EncounterResponse>>("/encounters", body);
  return res.data.data;
}

export async function getEncounter(id: string) {
  const res = await apiClient.get<ApiResponse<EncounterDetailResponse>>(`/encounters/${id}`);
  return res.data.data;
}

export async function updateEncounter(id: string, body: EncounterUpdateRequest) {
  const res = await apiClient.put<ApiResponse<EncounterResponse>>(`/encounters/${id}`, body);
  return res.data.data;
}

export async function startEncounter(id: string) {
  const res = await apiClient.post<ApiResponse<EncounterResponse>>(`/encounters/${id}/start`);
  return res.data.data;
}

export async function closeEncounter(id: string) {
  const res = await apiClient.post(`/encounters/${id}/close`);
  return res.data;
}

export async function addDiagnosis(encounterId: string, body: DiagnosisRequest) {
  const res = await apiClient.post<ApiResponse<DiagnosisResponse>>(
    `/encounters/${encounterId}/diagnoses`,
    body
  );
  return res.data.data;
}

export async function deleteDiagnosis(encounterId: string, diagnosisId: string) {
  await apiClient.delete(`/encounters/${encounterId}/diagnoses/${diagnosisId}`);
}

export async function updateChiefComplaint(encounterId: string, chief_complaint: string) {
  await apiClient.put(`/encounters/${encounterId}/chief-complaint`, { chief_complaint });
}

export async function getEncounterTimeline(encounterId: string) {
  const res = await apiClient.get<ApiResponse<TimelineEvent[]>>(`/encounters/${encounterId}/timeline`);
  return res.data.data;
}

export async function listOver12hAlerts() {
  const res = await apiClient.get<ApiResponse<Over12hAlert[]>>("/encounters/alerts/over-12h");
  return res.data.data;
}
