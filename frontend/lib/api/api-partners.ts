import apiClient from "./client";
import type { ApiMeta } from "./types";

// ─── Types ────────────────────────────────────────────────────────────────────

export type ApiPartnerStatus = "ACTIVE" | "DISABLED" | "EXPIRED";

export interface ApiPartnerResponse {
  id: string;
  name: string;
  contact_email?: string;
  api_key_masked: string;
  scopes: string[];
  rate_limit_per_min: number;
  daily_quota: number;
  daily_used?: number;
  status: ApiPartnerStatus;
  expires_at?: string | null;
  ip_whitelist: string[];
  created_at: string;
}

export interface ApiPartnerCreatedResponse extends ApiPartnerResponse {
  api_key_plain: string;
}

export interface ApiPartnerCreateRequest {
  name: string;
  contact_email?: string;
  scopes: string[];
  rate_limit_per_min?: number;
  daily_quota?: number;
  expires_at?: string | null;
  ip_whitelist?: string[];
}

export interface ApiPartnerUpdateRequest {
  name?: string;
  contact_email?: string;
  scopes?: string[];
  rate_limit_per_min?: number;
  daily_quota?: number;
  status?: "ACTIVE" | "DISABLED";
  expires_at?: string | null;
  ip_whitelist?: string[];
}

export interface ApiPartnerUsageStats {
  total_requests: number;
  success_count: number;
  error_count: number;
  by_endpoint: { path: string; count: number }[];
  by_day: { date: string; count: number }[];
}

export interface ApiPartnerRequestLog {
  id: string;
  method: string;
  path: string;
  status_code: number;
  duration_ms: number;
  ip: string;
  called_at: string;
  error_code?: string | null;
}

export interface ListApiPartnersParams {
  q?: string;
  status?: ApiPartnerStatus;
}

// ─── API calls ────────────────────────────────────────────────────────────────

export async function listApiPartners(params?: ListApiPartnersParams) {
  const res = await apiClient.get<{ data: ApiPartnerResponse[]; meta: ApiMeta }>(
    "/api-partners",
    { params }
  );
  return res.data;
}

export async function getApiPartner(id: string) {
  const res = await apiClient.get<{ data: ApiPartnerResponse }>(`/api-partners/${id}`);
  return res.data.data;
}

export async function createApiPartner(body: ApiPartnerCreateRequest) {
  const res = await apiClient.post<{ data: ApiPartnerCreatedResponse }>("/api-partners", body);
  return res.data.data;
}

export async function updateApiPartner(id: string, body: ApiPartnerUpdateRequest) {
  const res = await apiClient.put<{ data: ApiPartnerResponse }>(`/api-partners/${id}`, body);
  return res.data.data;
}

export async function deleteApiPartner(id: string) {
  await apiClient.delete(`/api-partners/${id}`);
}

export async function regenerateApiKey(id: string) {
  const res = await apiClient.post<{ data: ApiPartnerCreatedResponse }>(
    `/api-partners/${id}/regenerate-key`
  );
  return res.data.data;
}

export async function testApiPartnerCall(id: string) {
  const res = await apiClient.post<{ data: unknown }>(`/api-partners/${id}/test-call`);
  return res.data;
}

export async function getApiPartnerUsageStats(
  id: string,
  params?: { from?: string; to?: string }
) {
  const res = await apiClient.get<{ data: ApiPartnerUsageStats }>(
    `/api-partners/${id}/usage-stats`,
    { params }
  );
  return res.data.data;
}

export async function getApiPartnerRequestLogs(
  id: string,
  params?: { page?: number; status_code?: number }
) {
  const res = await apiClient.get<{ data: ApiPartnerRequestLog[]; meta: ApiMeta }>(
    `/api-partners/${id}/request-logs`,
    { params }
  );
  return res.data;
}

export const ALL_SCOPES = [
  "public.patient.read",
  "public.patient.write",
  "public.appointment.read",
  "public.appointment.write",
  "public.catalog.read",
  "public.visit.lookup",
] as const;

export type ApiScope = (typeof ALL_SCOPES)[number];
