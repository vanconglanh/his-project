import apiClient from "./client";
import type {
  ApiResponse,
  TreatmentSuggestionRequest,
  TreatmentSuggestionResponse,
  UpdateAiSuggestionStatusRequest,
} from "./types";

export async function generateTreatmentSuggestion(patientId: string, body?: TreatmentSuggestionRequest) {
  const res = await apiClient.post<ApiResponse<TreatmentSuggestionResponse>>(
    `/patients/${patientId}/ai/treatment-suggestion`,
    body ?? {}
  );
  return res.data.data;
}

export async function updateAiSuggestionStatus(logId: string, body: UpdateAiSuggestionStatusRequest) {
  await apiClient.patch(`/ai/suggestions/${logId}`, body);
}
