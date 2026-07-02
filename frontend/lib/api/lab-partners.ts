import apiClient from "./client";
import type { ApiResponse } from "./types";

// ─── Types ────────────────────────────────────────────────────────────────────

export type LabPartnerStatus = "ACTIVE" | "INACTIVE";
export type LabPartnerAuthType = "NONE" | "API_KEY" | "BEARER";
export type LabPartnerTransport = "REST" | "HL7_MLLP";

export interface LabPartner {
  id: string;
  tenant_id: string;
  code: string;
  name: string;
  endpoint_url: string;
  auth_type: LabPartnerAuthType;
  api_key_masked: string | null;
  transport: LabPartnerTransport;
  supported_tests: string[];
  status: LabPartnerStatus;
  contact_email: string | null;
  contact_phone: string | null;
  created_at: string;
  updated_at: string;
}

export interface LabPartnerCreateRequest {
  code: string;
  name: string;
  endpoint_url: string;
  auth_type: LabPartnerAuthType;
  api_key?: string | null;
  bearer_token?: string | null;
  transport: LabPartnerTransport;
  supported_tests?: string[];
  contact_email?: string | null;
  contact_phone?: string | null;
}

export interface LabPartnerUpdateRequest {
  name?: string;
  endpoint_url?: string;
  transport?: LabPartnerTransport;
  supported_tests?: string[];
  status?: LabPartnerStatus;
  contact_email?: string | null;
  contact_phone?: string | null;
}

export interface LabPartnerCredentialsRequest {
  auth_type: LabPartnerAuthType;
  api_key?: string;
  bearer_token?: string | null;
}

export interface ConnectionTestResult {
  ok: boolean;
  latency_ms: number;
  message: string;
}

export interface RotateCredentialsResult {
  api_key_masked: string;
  rotated_at: string;
}

// ─── API Functions ────────────────────────────────────────────────────────────

export async function listLabPartners(params?: { status?: LabPartnerStatus; q?: string }): Promise<LabPartner[]> {
  const res = await apiClient.get<ApiResponse<LabPartner[]>>("/lab-partners", { params });
  return res.data.data;
}

export async function getLabPartner(id: string): Promise<LabPartner> {
  const res = await apiClient.get<ApiResponse<LabPartner>>(`/lab-partners/${id}`);
  return res.data.data;
}

export async function createLabPartner(body: LabPartnerCreateRequest): Promise<LabPartner> {
  const res = await apiClient.post<ApiResponse<LabPartner>>("/lab-partners", body);
  return res.data.data;
}

export async function updateLabPartner(id: string, body: LabPartnerUpdateRequest): Promise<void> {
  await apiClient.put(`/lab-partners/${id}`, body);
}

export async function deleteLabPartner(id: string): Promise<void> {
  await apiClient.delete(`/lab-partners/${id}`);
}

export async function testLabPartnerConnection(id: string): Promise<ConnectionTestResult> {
  const res = await apiClient.post<ApiResponse<ConnectionTestResult>>(`/lab-partners/${id}/test-connection`);
  return res.data.data;
}

export async function updateLabPartnerCredentials(id: string, body: LabPartnerCredentialsRequest): Promise<void> {
  await apiClient.put(`/lab-partners/${id}/credentials`, body);
}

export async function rotateLabPartnerCredentials(id: string): Promise<RotateCredentialsResult> {
  const res = await apiClient.post<ApiResponse<RotateCredentialsResult>>(`/lab-partners/${id}/credentials/rotate`);
  return res.data.data;
}
