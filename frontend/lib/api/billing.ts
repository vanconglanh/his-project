import apiClient from "./client";
import type { ApiMeta } from "./types";

// ─── Types ────────────────────────────────────────────────────────────────────

export type BillingStatus =
  | "DRAFT"
  | "FINALIZED"
  | "PARTIAL_PAID"
  | "PAID"
  | "VOID";

export type BillingItemType =
  | "SERVICE"
  | "DRUG"
  | "PROCEDURE"
  | "LAB"
  | "RAD"
  | "PACKAGE"
  | "OTHER";

export type PayerType = "SELF" | "BHYT" | "MIXED";

export interface BillingItem {
  id: string;
  type: BillingItemType;
  ref_id: string | null;
  code: string;
  name: string;
  quantity: number;
  unit_price: number;
  vat_rate: 0 | 5 | 8 | 10;
  discount_percent: number;
  line_total: number;
  bhyt_applicable: boolean;
  bhyt_amount: number;
}

export interface BillingResponse {
  id: string;
  tenant_id: string;
  encounter_id: string | null;
  patient_id: string;
  patient_summary: {
    full_name: string;
    dob: string;
    gender: string;
    phone: string;
    bhyt_card_no_masked: string | null;
  };
  bill_no: string;
  items: BillingItem[];
  subtotal: number;
  vat_total: number;
  discount_amount: number;
  bhyt_amount: number;
  patient_payable: number;
  paid_amount: number;
  balance: number;
  status: BillingStatus;
  payment_due_date: string | null;
  payer: PayerType;
  note: string | null;
  created_at: string;
  created_by: string;
  finalized_at: string | null;
  void_reason: string | null;
}

export interface BillingListParams {
  status?: BillingStatus;
  patient_id?: string;
  encounter_id?: string;
  from_date?: string;
  to_date?: string;
  payer?: PayerType;
  page?: number;
  page_size?: number;
}

export interface BillingListResponse {
  data: BillingResponse[];
  meta: ApiMeta;
}

export interface BillingItemUpsert {
  type: BillingItemType;
  ref_id?: string | null;
  code?: string;
  name: string;
  quantity: number;
  unit_price: number;
  vat_rate?: number;
  discount_percent?: number;
  bhyt_applicable?: boolean;
}

export interface PendingEncounter {
  encounter_id: string;
  patient_code: string;
  patient_name: string;
  doctor_name: string;
  has_lab: boolean;
  has_rad: boolean;
  has_drug: boolean;
  estimated_total: number;
  created_at: string;
}

export interface PendingEncounterListParams {
  branch_id?: string;
  date?: string;
}

export interface PendingEncounterListResponse {
  data: PendingEncounter[];
  meta: ApiMeta;
}

// ─── API ──────────────────────────────────────────────────────────────────────

export async function listBillings(params?: BillingListParams): Promise<BillingListResponse> {
  const { data } = await apiClient.get<BillingListResponse>("/billings", { params });
  return data;
}

export async function getBilling(id: string): Promise<BillingResponse> {
  const { data } = await apiClient.get<{ data: BillingResponse }>(`/billings/${id}`);
  return data.data;
}

export async function createBilling(body: {
  encounter_id: string;
  include_dispensing?: boolean;
  payer?: PayerType;
  note?: string;
}): Promise<BillingResponse> {
  const { data } = await apiClient.post<{ data: BillingResponse }>("/billings", body);
  return data.data;
}

export async function updateBilling(id: string, body: {
  note?: string;
  discount_amount?: number;
  payment_due_date?: string;
}): Promise<BillingResponse> {
  const { data } = await apiClient.put<{ data: BillingResponse }>(`/billings/${id}`, body);
  return data.data;
}

export async function addBillingItem(id: string, item: BillingItemUpsert): Promise<BillingResponse> {
  const { data } = await apiClient.post<{ data: BillingResponse }>(`/billings/${id}/items`, item);
  return data.data;
}

export async function removeBillingItem(itemId: string): Promise<void> {
  await apiClient.delete(`/billings/items/${itemId}`);
}

export async function finalizeBilling(id: string): Promise<BillingResponse> {
  const { data } = await apiClient.post<{ data: BillingResponse }>(`/billings/${id}/finalize`);
  return data.data;
}

export async function voidBilling(id: string, reason: string): Promise<BillingResponse> {
  const { data } = await apiClient.post<{ data: BillingResponse }>(`/billings/${id}/void`, { reason });
  return data.data;
}

export async function applyBhyt(id: string, body: {
  bhyt_card_no: string;
  copay_rate: 80 | 95 | 100;
  right_route?: "DUNG_TUYEN" | "TRAI_TUYEN" | "CAP_CUU";
}): Promise<BillingResponse> {
  const { data } = await apiClient.post<{ data: BillingResponse }>(`/billings/${id}/apply-bhyt`, body);
  return data.data;
}

/** BUG-F01 — liệt kê lượt khám có dịch vụ nhưng CHƯA lập hoá đơn, phục vụ màn "Hàng chờ thu ngân". */
export async function listPendingEncounters(params?: PendingEncounterListParams): Promise<PendingEncounterListResponse> {
  const { data } = await apiClient.get<PendingEncounterListResponse>("/billings/pending-encounters", { params });
  return data;
}

export async function getBillingsByEncounter(encounterId: string): Promise<BillingResponse[]> {
  const { data } = await apiClient.get<{ data: BillingResponse[] }>(`/billings/encounter/${encounterId}`);
  return data.data;
}

/** FR-911 H-9 — QR VietQR động theo số tiền phải thu THẬT SỰ của hoá đơn (server tự tính). */
export interface DynamicBillingQrResponse {
  billing_id: string;
  amount: number;
  qr_payload: string;
  qr_payload_image_base64: string;
  qr_url: string | null;
  transaction_ref: string;
}

export async function generateDynamicBillingQr(id: string): Promise<DynamicBillingQrResponse> {
  const { data } = await apiClient.post<{ data: DynamicBillingQrResponse }>(`/billings/${id}/qr-dynamic`);
  return data.data;
}

export async function printBillingPdf(id: string): Promise<void> {
  // BUG FIX (QC print-button audit 2026-09-02): window.open() không gửi Bearer
  // token (API dùng JWT trong localStorage, không phải cookie) -> luôn 401.
  const url = `${apiClient.defaults.baseURL}/billings/${id}/pdf`;
  const { printPdfBlob } = await import("@/lib/utils/printPdfBlob");
  await printPdfBlob(url);
}
