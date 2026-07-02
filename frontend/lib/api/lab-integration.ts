import apiClient from "./client";
import type { ApiResponse, ApiMeta } from "./types";

// ─── Types ────────────────────────────────────────────────────────────────────

export type OutboundStatus = "PENDING" | "SENT" | "ACKED" | "FAILED";
export type InboundStatus = "RECEIVED" | "PROCESSED" | "FAILED";

export interface LabOutbound {
  id: string;
  lab_order_id: string;
  lab_partner_id: string;
  partner_name: string;
  external_order_id: string | null;
  payload_json: Record<string, unknown> | null;
  status: OutboundStatus;
  retry_count: number;
  error_message: string | null;
  sent_at: string | null;
  acked_at: string | null;
  created_at: string;
}

export interface LabInbound {
  id: string;
  lab_partner_id: string;
  partner_name: string;
  external_result_id: string;
  outbound_id: string | null;
  payload_json: Record<string, unknown> | null;
  raw_hl7_message: string | null;
  status: InboundStatus;
  processed_at: string | null;
  received_at: string;
  processed_result_count: number;
  error_message: string | null;
}

export interface SendOutboundRequest {
  lab_partner_id: string;
  priority?: "NORMAL" | "URGENT";
  note?: string | null;
}

export interface OutboundListParams {
  status?: OutboundStatus;
  lab_partner_id?: string;
  from_date?: string;
  to_date?: string;
  page?: number;
  page_size?: number;
}

export interface InboundListParams {
  status?: InboundStatus;
  lab_partner_id?: string;
  from_date?: string;
  to_date?: string;
  page?: number;
  page_size?: number;
}

export interface IntegrationStats {
  from_date: string;
  to_date: string;
  outbound_total: number;
  outbound_failed: number;
  inbound_total: number;
  inbound_failed: number;
  by_partner: {
    lab_partner_id: string;
    partner_name: string;
    outbound_sent: number;
    inbound_received: number;
    avg_turnaround_minutes: number;
  }[];
}

export interface InboundRawPayload {
  payload_json: Record<string, unknown> | null;
  raw_hl7_message: string | null;
  headers: Record<string, unknown>;
}

export interface OutboundListResponse {
  data: LabOutbound[];
  meta?: ApiMeta;
}

export interface InboundListResponse {
  data: LabInbound[];
  meta?: ApiMeta;
}

// ─── API Functions ────────────────────────────────────────────────────────────

export async function sendLabOrder(lab_order_id: string, body: SendOutboundRequest): Promise<LabOutbound> {
  const res = await apiClient.post<ApiResponse<LabOutbound>>(
    `/lab-integration/outbound/send/${lab_order_id}`,
    body
  );
  return res.data.data;
}

export async function listOutbound(params?: OutboundListParams): Promise<OutboundListResponse> {
  const res = await apiClient.get<OutboundListResponse>("/lab-integration/outbound", { params });
  return res.data;
}

export async function retryOutbound(id: string): Promise<void> {
  await apiClient.post(`/lab-integration/outbound/${id}/retry`);
}

export async function listInbound(params?: InboundListParams): Promise<InboundListResponse> {
  const res = await apiClient.get<InboundListResponse>("/lab-integration/inbound", { params });
  return res.data;
}

export async function reprocessInbound(id: string): Promise<void> {
  await apiClient.post(`/lab-integration/inbound/${id}/reprocess`);
}

export async function getInboundRaw(id: string): Promise<InboundRawPayload> {
  const res = await apiClient.get<ApiResponse<InboundRawPayload>>(`/lab-integration/inbound/${id}/raw`);
  return res.data.data;
}

export async function getIntegrationStats(days = 7): Promise<IntegrationStats> {
  const res = await apiClient.get<ApiResponse<IntegrationStats>>("/lab-integration/stats", {
    params: { days },
  });
  return res.data.data;
}
