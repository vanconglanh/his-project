import apiClient from "./client";
import type { ApiMeta } from "./types";

// ─── Types ────────────────────────────────────────────────────────────────────

export type ReconcileParseStatus = "PENDING" | "PARSING" | "PARSED" | "FAILED";
export type ReconcileItemStatus = "APPROVED" | "REJECTED" | "ADJUSTED" | "DISPUTED" | "ACCEPTED";

export interface ReconcileUploadResponse {
  id: string;
  tenant_id: string;
  export_id: string;
  file_path: string;
  uploaded_at: string;
  parsed_at: string | null;
  parse_status: ReconcileParseStatus;
  parse_error: string | null;
  created_at: string;
  created_by: string;
}

export interface ReconcileItemResponse {
  id: string;
  export_id: string;
  export_item_id: string;
  table_no: number;
  ma_lien_ket: string;
  request_amount: number;
  approved_amount: number;
  rejected_amount: number;
  rejection_code: string | null;
  rejection_reason: string | null;
  status: ReconcileItemStatus;
  dispute_reason: string | null;
  dispute_evidence_path: string | null;
  updated_at: string;
}

export interface ReconcileSummaryByTable {
  table_no: number;
  requested: number;
  approved: number;
  rejected: number;
}

export interface ReconcileTopRejection {
  code: string;
  reason: string;
  count: number;
  amount: number;
}

export interface ReconcileSummary {
  export_id: string;
  period_month: string;
  total_items: number;
  approved_items: number;
  rejected_items: number;
  adjusted_items: number;
  disputed_items: number;
  total_requested_amount: number;
  total_approved_amount: number;
  total_rejected_amount: number;
  by_table: ReconcileSummaryByTable[];
  top_rejection_reasons: ReconcileTopRejection[];
}

export type ReconcileStatusFilter = "ALL" | "APPROVED" | "REJECTED" | "ADJUSTED" | "DISPUTED";

export interface ReconcileListParams {
  status_filter?: ReconcileStatusFilter;
  page?: number;
  page_size?: number;
}

// ─── API calls ────────────────────────────────────────────────────────────────

export async function importReconcileFile(exportId: string, file: File): Promise<ReconcileUploadResponse> {
  const form = new FormData();
  form.append("export_id", exportId);
  form.append("file", file);
  const res = await apiClient.post<{ data: ReconcileUploadResponse }>("/bhyt/reconcile/import", form, {
    headers: { "Content-Type": "multipart/form-data" },
  });
  return res.data.data;
}

export async function listReconcileItems(
  exportId: string,
  params?: ReconcileListParams
): Promise<{ data: ReconcileItemResponse[]; meta: ApiMeta }> {
  const res = await apiClient.get<{ data: ReconcileItemResponse[]; meta: ApiMeta }>(
    `/bhyt/reconcile/${exportId}`,
    { params }
  );
  return res.data;
}

export async function disputeReconcileItem(
  itemId: string,
  body: { reason: string; evidence_file_path?: string }
): Promise<ReconcileItemResponse> {
  const res = await apiClient.post<{ data: ReconcileItemResponse }>(`/bhyt/reconcile/${itemId}/dispute`, body);
  return res.data.data;
}

export async function acceptReconcileItem(
  itemId: string,
  body?: { note?: string }
): Promise<ReconcileItemResponse> {
  const res = await apiClient.post<{ data: ReconcileItemResponse }>(`/bhyt/reconcile/${itemId}/accept`, body ?? {});
  return res.data.data;
}

export async function getReconcileSummary(exportId: string): Promise<ReconcileSummary> {
  const res = await apiClient.get<{ data: ReconcileSummary }>(`/bhyt/reconcile/${exportId}/summary`);
  return res.data.data;
}
