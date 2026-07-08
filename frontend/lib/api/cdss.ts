import apiClient from "./client";
import type {
  ApiResponse,
  CdssCheckRequest,
  CdssCheckResponse,
  CdssOverrideRequest,
  CdssOverrideResponse,
} from "./types";

export async function checkCdss(body: CdssCheckRequest) {
  const res = await apiClient.post<ApiResponse<CdssCheckResponse>>("/cdss/check", body);
  return res.data.data;
}

export async function overrideCdss(body: CdssOverrideRequest) {
  const res = await apiClient.post<ApiResponse<CdssOverrideResponse>>("/cdss/override", body);
  return res.data.data;
}
