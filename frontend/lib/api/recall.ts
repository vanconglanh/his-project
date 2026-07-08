import apiClient from "./client";
import type {
  ApiResponse,
  NotifyRecallRequest,
  NotifyRecallResponse,
  RecallListParams,
  RecallListResponse,
  UpdateRecallStatusRequest,
} from "./types";

export async function listRecall(params?: RecallListParams) {
  const res = await apiClient.get<RecallListResponse>("/recall", { params });
  return res.data;
}

export async function updateRecallStatus(id: string, body: UpdateRecallStatusRequest) {
  await apiClient.patch(`/recall/${id}`, body);
}

export async function notifyRecall(id: string, body?: NotifyRecallRequest) {
  const res = await apiClient.post<ApiResponse<NotifyRecallResponse>>(`/recall/${id}/notify`, body ?? {});
  return res.data.data;
}
