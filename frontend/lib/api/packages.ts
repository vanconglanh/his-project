import apiClient from "./client";
import type { ApiResponse } from "./types";

/** Số dư định mức 1 dịch vụ/loại trong gói (FR-1205). */
export interface PackageBalanceSummary {
  item_type: string;
  item_code: string;
  item_name: string;
  unit: string;
  total_quantity: number;
  used_quantity: number;
  remaining_quantity: number;
  /** Chuỗi hiển thị sẵn từ BE, dạng "còn X/Y" */
  display: string;
  /** BE đánh dấu định mức sắp hết (remaining/total <= 15%) */
  is_low: boolean;
}

/** 1 gói dịch vụ (subscription) mà bệnh nhân đang sở hữu. */
export interface PackageSubscriptionSummary {
  id: string;
  subscription_no: string;
  package_name: string;
  status: string;
  payment_status: string;
  expiry_date: string;
  days_to_expiry: number;
  amount_due: number;
  balances: PackageBalanceSummary[];
}

/** Tóm tắt toàn bộ gói dịch vụ của 1 bệnh nhân — GET /patients/{id}/package-summary (FR-1205/FR-1206). */
export interface PackagePatientSummary {
  total_outstanding_debt: number;
  has_expiring_soon: boolean;
  subscriptions: PackageSubscriptionSummary[];
}

export async function getPatientPackageSummary(
  patientId: string
): Promise<PackagePatientSummary> {
  const { data } = await apiClient.get<ApiResponse<PackagePatientSummary>>(
    `/patients/${patientId}/package-summary`
  );
  return data.data;
}
