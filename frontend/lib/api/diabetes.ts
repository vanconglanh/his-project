import apiClient from "./client";
import type {
  ApiResponse,
  DiabetesAssessmentRequest,
  DiabetesAssessmentResponse,
} from "./types";

export async function createDiabetesAssessment(encounterId: string, body: DiabetesAssessmentRequest) {
  const res = await apiClient.post<ApiResponse<DiabetesAssessmentResponse>>(
    `/encounters/${encounterId}/diabetes-assessment`,
    body
  );
  return res.data.data;
}

export async function getDiabetesAssessment(encounterId: string) {
  const res = await apiClient.get<ApiResponse<DiabetesAssessmentResponse>>(
    `/encounters/${encounterId}/diabetes-assessment`
  );
  return res.data.data;
}

export async function updateDiabetesAssessment(encounterId: string, body: DiabetesAssessmentRequest) {
  await apiClient.put(`/encounters/${encounterId}/diabetes-assessment`, body);
}

export async function getDiabetesHistory(
  patientId: string,
  params?: { date_from?: string; date_to?: string }
) {
  const res = await apiClient.get<ApiResponse<DiabetesAssessmentResponse[]>>(
    `/patients/${patientId}/diabetes-assessments/history`,
    { params }
  );
  return res.data.data;
}
