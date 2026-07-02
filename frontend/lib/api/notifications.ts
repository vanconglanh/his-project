import apiClient from "./client";
import type { ApiMeta } from "./types";

// ─── Types ────────────────────────────────────────────────────────────────────

export interface NotificationResponse {
  id: string;
  type: string;
  title: string;
  body: string;
  data_json?: Record<string, unknown>;
  read_at: string | null;
  created_at: string;
}

export interface WebPushSubscriptionRequest {
  endpoint: string;
  p256dh_key: string;
  auth_key: string;
  user_agent?: string;
}

export interface NotificationPreferenceRequest {
  position?: "TOP_RIGHT" | "BOTTOM_RIGHT" | "CENTER";
  sound_enabled?: boolean;
  sound_name?: string;
  browser_push_enabled?: boolean;
  types_disabled?: string[];
}

export interface NotificationPreferenceResponse extends NotificationPreferenceRequest {
  updated_at: string;
}

export interface VapidPublicKeyResponse {
  public_key: string;
}

// ─── Notifications CRUD ───────────────────────────────────────────────────────

export async function listNotifications(params?: {
  page?: number;
  page_size?: number;
  unread_only?: boolean;
}) {
  const res = await apiClient.get<{ data: NotificationResponse[]; meta: ApiMeta }>(
    "/notifications/inbox",
    { params }
  );
  return res.data;
}

export async function getUnreadCount() {
  const res = await apiClient.get<{ count: number }>("/notifications/unread-count");
  return res.data;
}

export async function markNotificationRead(id: string) {
  await apiClient.post(`/notifications/${id}/mark-read`);
}

export async function markAllNotificationsRead() {
  await apiClient.post("/notifications/mark-all-read");
}

export async function deleteNotification(id: string) {
  await apiClient.delete(`/notifications/${id}`);
}

// ─── Web Push ─────────────────────────────────────────────────────────────────

export async function subscribeWebPush(body: WebPushSubscriptionRequest) {
  await apiClient.post("/notifications/web-push/subscribe", body);
}

export async function unsubscribeWebPush(endpoint: string) {
  await apiClient.delete("/notifications/web-push/unsubscribe", { params: { endpoint } });
}

export async function getVapidPublicKey() {
  const res = await apiClient.get<VapidPublicKeyResponse>(
    "/notifications/web-push/vapid-public-key"
  );
  return res.data;
}

// ─── Preferences ─────────────────────────────────────────────────────────────

export async function getNotificationPreferences() {
  const res = await apiClient.get<NotificationPreferenceResponse>("/notifications/preferences");
  return res.data;
}

export async function updateNotificationPreferences(body: NotificationPreferenceRequest) {
  const res = await apiClient.put<NotificationPreferenceResponse>(
    "/notifications/preferences",
    body
  );
  return res.data;
}

// ─── Admin: VAPID ─────────────────────────────────────────────────────────────

export async function generateVapidKey() {
  const res = await apiClient.post<{ public_key: string }>("/notifications/vapid/generate");
  return res.data;
}

export async function getVapidStatus() {
  const res = await apiClient.get<{ configured: boolean; public_key?: string }>(
    "/notifications/vapid/status"
  );
  return res.data;
}

export async function sendTestNotification(body: { user_id?: string; title: string; body: string }) {
  await apiClient.post("/notifications/test-send", body);
}

export async function listNotificationLogs(params?: { page?: number; page_size?: number }) {
  const res = await apiClient.get<{ data: NotificationResponse[]; meta: ApiMeta }>(
    "/notifications/logs",
    { params }
  );
  return res.data;
}
