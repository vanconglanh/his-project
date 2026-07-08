import apiClient from "./client";
import type {
  ApiResponse,
  CarePathwayTargetDto,
  DeteriorationFlagsResponse,
  DiabetesTrajectoryResponse,
  RiskListParams,
  RiskListResponse,
} from "./types";

export async function getDiabetesTrajectory(
  patientId: string,
  params?: { from?: string; to?: string }
) {
  const res = await apiClient.get<ApiResponse<DiabetesTrajectoryResponse>>(
    `/patients/${patientId}/diabetes/trajectory`,
    { params }
  );
  return res.data.data;
}

export async function getDeteriorationFlags(patientId: string) {
  const res = await apiClient.get<ApiResponse<DeteriorationFlagsResponse>>(
    `/patients/${patientId}/diabetes/deterioration-flags`
  );
  return res.data.data;
}

export async function getRiskList(params?: RiskListParams) {
  const res = await apiClient.get<RiskListResponse>("/diabetes/risk-list", { params });
  return res.data;
}

export async function getCarePathwayTargets(code = "DM_T2_5481") {
  const res = await apiClient.get<ApiResponse<CarePathwayTargetDto[]>>("/care-pathway/targets", {
    params: { code },
  });
  return res.data.data;
}
