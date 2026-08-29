import apiClient from "./client";
import type { ApiResponse } from "./types";

// ─── Types ────────────────────────────────────────────────────────────────────

export type NotificationChannelType = "SMS" | "ZALO_ZNS";

export interface NotificationChannelResponse {
  id: string;
  tenant_id: number;
  branch_id: number | null;
  channel: NotificationChannelType;
  provider: string;
  config_masked: Record<string, string>;
  is_active: boolean;
  last_tested_at?: string | null;
  last_test_ok: boolean;
  created_at: string;
  updated_at: string;
}

export interface NotificationChannelRequest {
  channel: NotificationChannelType;
  provider: string;
  config: Record<string, string>;
  is_active: boolean;
}

export interface NotificationChannelTestResult {
  ok: boolean;
  message?: string | null;
}

// ─── API calls ────────────────────────────────────────────────────────────────

export async function listNotificationChannels(): Promise<NotificationChannelResponse[]> {
  const { data } = await apiClient.get<ApiResponse<NotificationChannelResponse[]>>("/notification-channels");
  return data.data;
}

export async function getNotificationChannel(id: string): Promise<NotificationChannelResponse> {
  const { data } = await apiClient.get<ApiResponse<NotificationChannelResponse>>(`/notification-channels/${id}`);
  return data.data;
}

export async function createNotificationChannel(
  body: NotificationChannelRequest
): Promise<NotificationChannelResponse> {
  const { data } = await apiClient.post<ApiResponse<NotificationChannelResponse>>("/notification-channels", body);
  return data.data;
}

export async function updateNotificationChannel(
  id: string,
  body: NotificationChannelRequest
): Promise<NotificationChannelResponse> {
  const { data } = await apiClient.put<ApiResponse<NotificationChannelResponse>>(`/notification-channels/${id}`, body);
  return data.data;
}

export async function deleteNotificationChannel(id: string): Promise<void> {
  await apiClient.delete(`/notification-channels/${id}`);
}

export async function testNotificationChannel(id: string): Promise<NotificationChannelTestResult> {
  const { data } = await apiClient.post<ApiResponse<NotificationChannelTestResult>>(
    `/notification-channels/${id}/test`
  );
  return data.data;
}
