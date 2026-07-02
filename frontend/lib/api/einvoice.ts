import apiClient from "./client";
import type { ApiMeta } from "./types";

// ─── Types ────────────────────────────────────────────────────────────────────

export type EInvoiceStatus = "DRAFT" | "ISSUED" | "CANCELLED" | "REPLACED";
export type EInvoiceProvider = "MISA" | "VNPT" | "EFY";

export interface EInvoiceResponse {
  id: string;
  tenant_id: string;
  billing_id: string;
  provider: EInvoiceProvider;
  invoice_no: string;
  invoice_series: string | null;
  cqt_code: string;
  issue_date: string;
  total_amount: number;
  vat_amount: number;
  status: EInvoiceStatus;
  pdf_url: string | null;
  xml_url: string | null;
  signed_at: string | null;
  cancel_reason: string | null;
  cancelled_at: string | null;
  created_at: string;
  created_by: string;
}

export interface EInvoiceListParams {
  status?: EInvoiceStatus;
  provider?: EInvoiceProvider;
  billing_id?: string;
  from_date?: string;
  to_date?: string;
  page?: number;
  page_size?: number;
}

export interface EInvoiceListResponse {
  data: EInvoiceResponse[];
  meta: ApiMeta;
}

// ─── API ──────────────────────────────────────────────────────────────────────

export async function listEInvoices(params?: EInvoiceListParams): Promise<EInvoiceListResponse> {
  const { data } = await apiClient.get<EInvoiceListResponse>("/einvoices", { params });
  return data;
}

export async function getEInvoice(id: string): Promise<EInvoiceResponse> {
  const { data } = await apiClient.get<{ data: EInvoiceResponse }>(`/einvoices/${id}`);
  return data.data;
}

export async function issueEInvoice(body: {
  billing_id: string;
  provider: EInvoiceProvider;
  buyer?: {
    name?: string;
    tax_code?: string | null;
    address?: string | null;
    email?: string | null;
    phone?: string | null;
  };
  send_email?: boolean;
}): Promise<EInvoiceResponse> {
  const { data } = await apiClient.post<{ data: EInvoiceResponse }>("/einvoices/issue", body);
  return data.data;
}

export async function cancelEInvoice(id: string, reason: string): Promise<void> {
  await apiClient.post(`/einvoices/${id}/cancel`, { reason });
}

export function downloadEInvoiceXml(id: string): void {
  const url = `${apiClient.defaults.baseURL}/einvoices/${id}/xml-download`;
  window.open(url, "_blank");
}
