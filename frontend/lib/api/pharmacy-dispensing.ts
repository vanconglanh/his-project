import apiClient from "./client";
import type { ApiResponse, ApiMeta } from "./types";

// ─── Types ────────────────────────────────────────────────────────────────────

export interface DispenseQueueItem {
  prescription_id: string;
  prescription_code: string;
  patient_id: string;
  patient_name: string;
  doctor_name: string;
  signed_at: string;
  items_count: number;
  total_amount: number;
  is_bhyt: boolean;
}

export interface DispenseRequest {
  warehouse_id: string;
  note?: string;
  items: Array<{
    prescription_item_id: string;
    batch_picks: Array<{
      batch_no: string;
      quantity: number;
    }>;
  }>;
}

export interface DispenseRecordResponse {
  id: string;
  tenant_id: string;
  prescription_id: string;
  warehouse_id: string;
  dispensed_at: string;
  dispensed_by: string;
  dispensed_by_name: string;
  status: "DISPENSED" | "REJECTED" | "RETURNED" | "PARTIAL";
  note?: string;
  items: Array<{
    id: string;
    prescription_item_id: string;
    drug_id: string;
    drug_name: string;
    batch_no: string;
    expiry_date: string;
    quantity: number;
    unit_cost: number;
    line_amount: number;
  }>;
  total_amount: number;
}

// ─── API calls ────────────────────────────────────────────────────────────────

export async function getDispenseQueue(params?: {
  warehouse_id?: string;
  q?: string;
  page?: number;
  page_size?: number;
}): Promise<{ data: DispenseQueueItem[]; meta: ApiMeta }> {
  const { data } = await apiClient.get<{ data: DispenseQueueItem[]; meta: ApiMeta }>(
    "/pharmacy/dispense/queue",
    { params }
  );
  return data;
}

export async function dispensePrescription(
  prescriptionId: string,
  body: DispenseRequest
): Promise<DispenseRecordResponse> {
  const { data } = await apiClient.post<ApiResponse<DispenseRecordResponse>>(
    `/pharmacy/dispense/${prescriptionId}`,
    body
  );
  return data.data;
}

export async function rejectDispense(id: string, reason: string): Promise<DispenseRecordResponse> {
  const { data } = await apiClient.post<ApiResponse<DispenseRecordResponse>>(
    `/pharmacy/dispense/${id}/reject`,
    { reason }
  );
  return data.data;
}

export async function returnDispense(
  id: string,
  body: { reason: string; items: Array<{ dispense_item_id: string; quantity: number }> }
): Promise<DispenseRecordResponse> {
  const { data } = await apiClient.post<ApiResponse<DispenseRecordResponse>>(
    `/pharmacy/dispense/${id}/return`,
    body
  );
  return data.data;
}

export async function listDispenseHistory(params?: {
  patient_id?: string;
  from_date?: string;
  to_date?: string;
  status?: string;
  page?: number;
  page_size?: number;
}): Promise<{ data: DispenseRecordResponse[]; meta: ApiMeta }> {
  const { data } = await apiClient.get<{ data: DispenseRecordResponse[]; meta: ApiMeta }>(
    "/pharmacy/dispense/history",
    { params }
  );
  return data;
}

export async function printDispenseReceipt(id: string): Promise<void> {
  const url = `${apiClient.defaults.baseURL}/pharmacy/dispense/${id}/receipt-pdf`;
  window.open(url, "_blank");
}
