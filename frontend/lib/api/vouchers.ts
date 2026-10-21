import apiClient from "./client";
import type { ApiResponse, ApiMeta } from "./types";

export type VoucherAppliesTo = "EXAM" | "LAB" | "IMAGING" | "DRUG" | "ALL";

export const VOUCHER_APPLIES_TO_LABEL: Record<VoucherAppliesTo, string> = {
  EXAM: "Phí khám",
  LAB: "Xét nghiệm",
  IMAGING: "Chẩn đoán hình ảnh",
  DRUG: "Thuốc",
  ALL: "Tất cả",
};

/** Khớp VoucherResponse (backend, JSON snake_case). */
export interface VoucherResponse {
  id: string;
  code: string;
  description: string | null;
  discount_percent: number;
  applies_to: VoucherAppliesTo;
  valid_from: string | null;
  valid_to: string | null;
  max_uses: number | null;
  used_count?: number;
  is_active: boolean;
}

export interface VoucherBody {
  description?: string | null;
  discount_percent: number;
  applies_to: VoucherAppliesTo;
  valid_from: string | null;
  valid_to: string | null;
  max_uses: number | null;
  is_active: boolean;
}

export async function listVouchers(params?: {
  is_active?: boolean;
  page?: number;
  page_size?: number;
}): Promise<{ data: VoucherResponse[]; meta: ApiMeta }> {
  const { data } = await apiClient.get<{ data: VoucherResponse[]; meta: ApiMeta }>("/vouchers", { params });
  return data;
}

export async function getVoucher(id: string): Promise<VoucherResponse> {
  const { data } = await apiClient.get<ApiResponse<VoucherResponse>>(`/vouchers/${id}`);
  return data.data;
}

export async function createVoucher(body: VoucherBody & { code: string }): Promise<VoucherResponse> {
  const { data } = await apiClient.post<ApiResponse<VoucherResponse>>("/vouchers", body);
  return data.data;
}

export async function updateVoucher(id: string, body: VoucherBody): Promise<VoucherResponse> {
  const { data } = await apiClient.put<ApiResponse<VoucherResponse>>(`/vouchers/${id}`, body);
  return data.data;
}

export async function deleteVoucher(id: string): Promise<void> {
  await apiClient.delete(`/vouchers/${id}`);
}
