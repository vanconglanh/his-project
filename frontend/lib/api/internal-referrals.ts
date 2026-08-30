import apiClient from "./client";
import type { ApiResponse } from "./types";

/**
 * Chuyển cơ sở nội bộ (Internal referral) — BR-29.
 * Casing xac nhan tu backend/src/ProDiabHis.Application/Branches/InternalReferralDtos.cs
 * (InternalReferralDto/CreateInternalReferralRequest/UpdateInternalReferralStatusRequest).
 */
export type InternalReferralStatus = "SENT" | "ACCEPTED" | "COMPLETED" | "CANCELLED";

export interface InternalReferralResponse {
  id: number;
  tenant_id: number;
  patient_id: string;
  patient_name?: string | null;
  source_branch_id: number;
  source_branch_name?: string | null;
  target_branch_id: number;
  target_branch_name?: string | null;
  encounter_id?: string | null;
  referring_doctor_id?: string | null;
  reason?: string | null;
  status: InternalReferralStatus;
  note?: string | null;
  created_at: string;
  updated_at: string;
}

export interface CreateInternalReferralRequest {
  patient_id: string;
  target_branch_id: number;
  encounter_id?: string;
  reason?: string;
  note?: string;
}

export interface UpdateInternalReferralStatusRequest {
  status: "ACCEPTED" | "COMPLETED" | "CANCELLED";
  note?: string;
}

export async function createInternalReferral(
  body: CreateInternalReferralRequest
): Promise<InternalReferralResponse> {
  const { data } = await apiClient.post<ApiResponse<InternalReferralResponse>>(
    "/internal-referrals",
    body
  );
  return data.data;
}

export async function listIncomingInternalReferrals(
  status?: string
): Promise<InternalReferralResponse[]> {
  const { data } = await apiClient.get<ApiResponse<InternalReferralResponse[]>>(
    "/internal-referrals/incoming",
    { params: status ? { status } : undefined }
  );
  return data.data;
}

export async function updateInternalReferralStatus(
  id: number | string,
  body: UpdateInternalReferralStatusRequest
): Promise<InternalReferralResponse> {
  const { data } = await apiClient.patch<ApiResponse<InternalReferralResponse>>(
    `/internal-referrals/${id}/status`,
    body
  );
  return data.data;
}
