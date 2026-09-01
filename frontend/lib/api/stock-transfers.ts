import apiClient from "./client";
import type { ApiMeta } from "./types";

/**
 * Điều chuyển kho nội bộ giữa chi nhánh (E/Đợt3, mục 4.2 BRD, BR-51..BR-63).
 * Khớp DTO backend: ProDiabHis.Application.Pharmacy.StockTransfers.StockTransferDtos.cs
 */

export const STOCK_TRANSFER_STATUSES = [
  "DRAFT",
  "PENDING_APPROVAL",
  "APPROVED",
  "REJECTED",
  "IN_TRANSIT",
  "RECEIVED",
  "PARTIALLY_RECEIVED",
  "CLOSED",
  "CANCELLED",
] as const;

export type StockTransferStatus = (typeof STOCK_TRANSFER_STATUSES)[number];

export interface StockTransferItemRequest {
  drug_id: string;
  lot_no?: string | null;
  expiry_date?: string | null;
  qty_requested: number;
  unit_cost: number;
  note?: string | null;
}

export interface CreateStockTransferRequest {
  from_branch_id: number;
  to_branch_id: number;
  reason?: string;
  items: StockTransferItemRequest[];
}

export interface RejectStockTransferRequest {
  reason: string;
}

export interface ReceiveItemRequest {
  item_id: string;
  qty_received: number;
}

export interface ReceiveStockTransferRequest {
  items: ReceiveItemRequest[];
  note?: string;
}

export interface ApproveStockTransferRequest {
  override_expiry_guard?: boolean;
}

export interface StockTransferItemResponse {
  id: string;
  drug_id: string;
  drug_name: string | null;
  lot_no: string | null;
  expiry_date: string | null;
  qty_requested: number;
  qty_shipped: number;
  qty_received: number;
  unit_cost: number;
  note: string | null;
}

export interface StockTransferResponse {
  id: string;
  tenant_id: number;
  transfer_no: string;
  from_branch_id: number;
  from_branch_name: string | null;
  to_branch_id: number;
  to_branch_name: string | null;
  status: StockTransferStatus;
  total_value: number;
  requires_approval: boolean;
  reason: string | null;
  requested_by: string | null;
  requested_at: string | null;
  approved_by: string | null;
  approved_at: string | null;
  rejected_reason: string | null;
  shipped_by: string | null;
  shipped_at: string | null;
  received_by: string | null;
  received_at: string | null;
  items: StockTransferItemResponse[];
  created_at: string;
}

export interface StockTransferListParams {
  status?: StockTransferStatus;
  branch_id?: number;
  page?: number;
  page_size?: number;
}

// ─── API ──────────────────────────────────────────────────────────────────────

export async function listStockTransfers(
  params?: StockTransferListParams
): Promise<{ data: StockTransferResponse[]; meta: ApiMeta }> {
  const { data } = await apiClient.get<{ data: StockTransferResponse[]; meta: ApiMeta }>(
    "/stock-transfers",
    { params }
  );
  return data;
}

export async function getStockTransfer(id: string): Promise<StockTransferResponse> {
  const { data } = await apiClient.get<{ data: StockTransferResponse }>(`/stock-transfers/${id}`);
  return data.data;
}

export async function createStockTransfer(
  body: CreateStockTransferRequest
): Promise<StockTransferResponse> {
  const { data } = await apiClient.post<{ data: StockTransferResponse }>("/stock-transfers", body);
  return data.data;
}

export async function submitStockTransfer(id: string): Promise<StockTransferResponse> {
  const { data } = await apiClient.post<{ data: StockTransferResponse }>(`/stock-transfers/${id}/submit`);
  return data.data;
}

export async function approveStockTransfer(
  id: string,
  body?: ApproveStockTransferRequest
): Promise<StockTransferResponse> {
  const { data } = await apiClient.post<{ data: StockTransferResponse }>(
    `/stock-transfers/${id}/approve`,
    body ?? {}
  );
  return data.data;
}

export async function rejectStockTransfer(
  id: string,
  body: RejectStockTransferRequest
): Promise<StockTransferResponse> {
  const { data } = await apiClient.post<{ data: StockTransferResponse }>(
    `/stock-transfers/${id}/reject`,
    body
  );
  return data.data;
}

export async function shipStockTransfer(id: string): Promise<StockTransferResponse> {
  const { data } = await apiClient.post<{ data: StockTransferResponse }>(`/stock-transfers/${id}/ship`);
  return data.data;
}

export async function receiveStockTransfer(
  id: string,
  body?: ReceiveStockTransferRequest
): Promise<StockTransferResponse> {
  const { data } = await apiClient.post<{ data: StockTransferResponse }>(
    `/stock-transfers/${id}/receive`,
    body ?? { items: [] }
  );
  return data.data;
}

export async function partialReceiveStockTransfer(
  id: string,
  body: ReceiveStockTransferRequest
): Promise<StockTransferResponse> {
  const { data } = await apiClient.post<{ data: StockTransferResponse }>(
    `/stock-transfers/${id}/partial-receive`,
    body
  );
  return data.data;
}

export async function closeStockTransfer(id: string): Promise<StockTransferResponse> {
  const { data } = await apiClient.post<{ data: StockTransferResponse }>(`/stock-transfers/${id}/close`);
  return data.data;
}

export async function cancelStockTransfer(id: string): Promise<StockTransferResponse> {
  const { data } = await apiClient.post<{ data: StockTransferResponse }>(`/stock-transfers/${id}/cancel`);
  return data.data;
}
