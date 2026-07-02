import apiClient from "./client";
import type { ApiMeta } from "./types";

// ─── Types ────────────────────────────────────────────────────────────────────

export type PaymentMethod =
  | "CASH"
  | "BANK_TRANSFER"
  | "VISA"
  | "MASTER"
  | "QR_VIETQR"
  | "QR_MOMO"
  | "QR_VNPAY"
  | "OTHER";

export type PaymentStatus =
  | "PENDING"
  | "COMPLETED"
  | "FAILED"
  | "REFUNDED"
  | "VOID";

export interface PaymentResponse {
  id: string;
  tenant_id: string;
  billing_id: string;
  amount: number;
  method: PaymentMethod;
  reference: string | null;
  status: PaymentStatus;
  provider: string | null;
  provider_txn_id: string | null;
  paid_at: string | null;
  paid_by: string | null;
  cashier_shift_id: string | null;
  note: string | null;
  refunded_amount: number;
  created_at: string;
}

export interface QrCodeResponse {
  id: string;
  billing_id: string;
  provider: "VIETQR" | "MOMO" | "VNPAY";
  qr_payload: string;
  qr_url: string;
  amount: number;
  expires_at: string;
  paid_at: string | null;
  status: "PENDING" | "PAID" | "EXPIRED" | "CANCELLED";
  transaction_ref: string;
}

export interface PaymentListParams {
  billing_id?: string;
  method?: PaymentMethod;
  status?: PaymentStatus;
  from_date?: string;
  to_date?: string;
  page?: number;
  page_size?: number;
}

export interface PaymentListResponse {
  data: PaymentResponse[];
  meta: ApiMeta;
}

// ─── API ──────────────────────────────────────────────────────────────────────

export async function listPayments(params?: PaymentListParams): Promise<PaymentListResponse> {
  const { data } = await apiClient.get<PaymentListResponse>("/payments", { params });
  return data;
}

export async function getPayment(id: string): Promise<PaymentResponse> {
  const { data } = await apiClient.get<{ data: PaymentResponse }>(`/payments/${id}`);
  return data.data;
}

export async function createPayment(body: {
  billing_id: string;
  amount: number;
  method: PaymentMethod;
  reference?: string;
  provider?: string;
  provider_txn_id?: string;
  note?: string;
}): Promise<PaymentResponse> {
  const { data } = await apiClient.post<{ data: PaymentResponse }>("/payments", body);
  return data.data;
}

export async function refundPayment(id: string, body: {
  amount: number;
  reason: string;
}): Promise<PaymentResponse> {
  const { data } = await apiClient.post<{ data: PaymentResponse }>(`/payments/${id}/refund`, body);
  return data.data;
}

export async function voidPayment(id: string, reason?: string): Promise<PaymentResponse> {
  const { data } = await apiClient.post<{ data: PaymentResponse }>(`/payments/${id}/void`, { reason });
  return data.data;
}

export async function generateQr(body: {
  billing_id: string;
  provider: "VIETQR" | "MOMO" | "VNPAY";
  amount: number;
  expires_in_seconds?: number;
}): Promise<QrCodeResponse> {
  const { data } = await apiClient.post<{ data: QrCodeResponse }>("/payments/qr/generate", body);
  return data.data;
}

export async function getQrStatus(qrId: string): Promise<QrCodeResponse> {
  const { data } = await apiClient.get<{ data: QrCodeResponse }>(`/payments/qr/${qrId}/status`);
  return data.data;
}

export async function chargeCard(body: {
  billing_id: string;
  amount: number;
  card_token: string;
  provider?: "VISA" | "MASTER";
  three_ds_nonce?: string;
}): Promise<PaymentResponse> {
  const { data } = await apiClient.post<{ data: PaymentResponse }>("/payments/card/charge", body);
  return data.data;
}
