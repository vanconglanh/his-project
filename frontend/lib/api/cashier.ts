import apiClient from "./client";
import type { ApiMeta } from "./types";

// ─── Types ────────────────────────────────────────────────────────────────────

export type ShiftStatus = "OPEN" | "CLOSED";

export interface ShiftSummary {
  total_cash: number;
  total_card: number;
  total_transfer: number;
  total_qr: number;
  total_other: number;
  total_refund: number;
  total_void: number;
  count_transactions: number;
  gross_collected: number;
  net_collected: number;
  breakdown_by_method: Array<{ method: string; amount: number; count: number }>;
}

export interface CashierClosingResponse {
  id: string;
  tenant_id: string;
  cashier_user_id: string;
  cashier_name: string;
  shift_date: string;
  shift_start: string;
  shift_end: string | null;
  summary: ShiftSummary;
  opening_balance: number;
  closing_balance: number | null;
  expected_cash: number | null;
  actual_cash: number | null;
  difference: number | null;
  note: string | null;
  status: ShiftStatus;
  closed_by: string | null;
  created_at: string;
}

export interface DebtResponse {
  patient_id: string;
  patient_code: string;
  patient_name: string;
  phone: string | null;
  total_billed: number;
  total_paid: number;
  balance: number;
  unpaid_bills_count: number;
  last_payment_at: string | null;
  oldest_unpaid_at: string | null;
  days_overdue: number | null;
}

export interface ClosingHistoryParams {
  cashier_user_id?: string;
  from_date?: string;
  to_date?: string;
  status?: ShiftStatus;
  page?: number;
  page_size?: number;
}

export interface DebtListParams {
  q?: string;
  min_balance?: number;
  older_than_days?: number;
  page?: number;
  page_size?: number;
}

// ─── API ──────────────────────────────────────────────────────────────────────

export async function getTodayClosing(params?: {
  cashier_user_id?: string;
  date?: string;
}): Promise<CashierClosingResponse> {
  const { data } = await apiClient.get<{ data: CashierClosingResponse }>("/cashier/closing/today", { params });
  return data.data;
}

export async function openShift(body?: {
  opening_balance?: number;
  note?: string;
}): Promise<CashierClosingResponse> {
  const { data } = await apiClient.post<{ data: CashierClosingResponse }>("/cashier/closing/open", body ?? {});
  return data.data;
}

export async function closeShift(body: {
  shift_id?: string | null;
  actual_cash: number;
  note?: string;
  accept_difference?: boolean;
}): Promise<CashierClosingResponse> {
  const { data } = await apiClient.post<{ data: CashierClosingResponse }>("/cashier/closing/close", body);
  return data.data;
}

export async function listClosingHistory(params?: ClosingHistoryParams): Promise<{ data: CashierClosingResponse[]; meta: ApiMeta }> {
  const { data } = await apiClient.get<{ data: CashierClosingResponse[]; meta: ApiMeta }>("/cashier/closing/history", { params });
  return data;
}

export async function listDebts(params?: DebtListParams): Promise<{
  data: DebtResponse[];
  meta: ApiMeta;
  summary: { total_patients: number; total_debt: number };
}> {
  const { data } = await apiClient.get<{
    data: DebtResponse[];
    meta: ApiMeta;
    summary: { total_patients: number; total_debt: number };
  }>("/cashier/debts", { params });
  return data;
}

export function printShiftPdf(shiftId: string): void {
  const url = `${apiClient.defaults.baseURL}/cashier/closing/${shiftId}/pdf`;
  window.open(url, "_blank");
}

export async function printReceiptPdf(paymentId: string): Promise<void> {
  const url = `${apiClient.defaults.baseURL}/cashier/receipts/${paymentId}/print`;
  const { printPdfBlob } = await import("@/lib/utils/printPdfBlob");
  await printPdfBlob(url);
}
