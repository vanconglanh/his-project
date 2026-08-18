import apiClient from "./client";
import type { ApiResponse } from "./types";

// ─── Đợt chỉ định CLS (G01/G02) ───────────────────────────────────────────────
// Endpoint backend: /encounters/{id}/cls-rounds, /cls-rounds/{id}[/submit|/pay|/waive|/cancel]

export type ClsRoundStatus =
  | "OPEN"
  | "SUBMITTED"
  | "IN_PROGRESS"
  | "COMPLETED"
  | "CANCELLED";

export type ClsRoundPaymentStatus = "UNPAID" | "PAID" | "WAIVED";

export interface ClsRoundOrderItem {
  id: string;
  kind: "LAB" | "RAD";
  code: string;
  name: string;
  status: string;
  unit_price: number;
}

export interface ClsRoundProgress {
  total: number;
  done: number;
  pending: number;
}

export interface ClsRound {
  id: string;
  encounter_id: string;
  round_no: number;
  status: ClsRoundStatus;
  payment_status: ClsRoundPaymentStatus;
  total_amount: number;
  billing_id?: string | null;
  paid_at?: string | null;
  waived_reason?: string | null;
  note?: string | null;
  created_at: string;
  lab_orders: ClsRoundOrderItem[];
  rad_orders: ClsRoundOrderItem[];
  progress: ClsRoundProgress;
}

export interface ClsRoundListMeta {
  total: number;
  unpaid_rounds: number;
  unpaid_amount: number;
}

export interface ClsRoundListResult {
  rounds: ClsRound[];
  meta: ClsRoundListMeta;
}

export interface ClsRoundLabItemRequest {
  test_code: string;
  test_name?: string;
  sample_type?: string | null;
  priority?: string;
  note?: string;
}

export interface ClsRoundRadItemRequest {
  modality: string;
  body_part?: string;
  contrast: boolean;
  procedure_code: string;
  procedure_name?: string;
  priority?: string;
  note?: string;
}

export interface CreateClsRoundRequest {
  note?: string;
  lab_tests?: ClsRoundLabItemRequest[];
  rad_orders?: ClsRoundRadItemRequest[];
}

export async function listClsRounds(
  encounterId: string,
  status?: string
): Promise<ClsRoundListResult> {
  const res = await apiClient.get<{ data: ClsRound[]; meta: ClsRoundListMeta }>(
    `/encounters/${encounterId}/cls-rounds`,
    { params: status ? { status } : undefined }
  );
  return {
    rounds: res.data.data ?? [],
    meta: res.data.meta ?? { total: 0, unpaid_rounds: 0, unpaid_amount: 0 },
  };
}

export async function createClsRound(encounterId: string, body: CreateClsRoundRequest) {
  const res = await apiClient.post<ApiResponse<ClsRound>>(
    `/encounters/${encounterId}/cls-rounds`,
    body
  );
  return res.data.data;
}

export async function getClsRound(roundId: string) {
  const res = await apiClient.get<ApiResponse<ClsRound>>(`/cls-rounds/${roundId}`);
  return res.data.data;
}

export async function submitClsRound(roundId: string) {
  const res = await apiClient.post<ApiResponse<ClsRound>>(`/cls-rounds/${roundId}/submit`);
  return res.data.data;
}

export async function payClsRound(
  roundId: string,
  body?: { billing_id?: string; method?: string; amount?: number; note?: string }
) {
  const res = await apiClient.post<ApiResponse<ClsRound>>(`/cls-rounds/${roundId}/pay`, body ?? {});
  return res.data.data;
}

export async function waiveClsRound(roundId: string, reason: string) {
  const res = await apiClient.post<ApiResponse<ClsRound>>(`/cls-rounds/${roundId}/waive`, { reason });
  return res.data.data;
}

export async function cancelClsRound(roundId: string, reason?: string) {
  const res = await apiClient.post<ApiResponse<ClsRound>>(`/cls-rounds/${roundId}/cancel`, { reason });
  return res.data.data;
}
