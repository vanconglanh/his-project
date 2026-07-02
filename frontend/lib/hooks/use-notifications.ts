"use client";

import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import {
  listNotifications,
  getUnreadCount,
  markNotificationRead,
  markAllNotificationsRead,
  deleteNotification,
  subscribeWebPush,
  unsubscribeWebPush,
  getVapidPublicKey,
  getNotificationPreferences,
  updateNotificationPreferences,
  generateVapidKey,
  getVapidStatus,
  sendTestNotification,
  listNotificationLogs,
} from "@/lib/api/notifications";
import type { NotificationPreferenceRequest } from "@/lib/api/notifications";

export const notificationKeys = {
  all: ["notifications"] as const,
  inbox: (params?: object) => ["notifications", "inbox", params] as const,
  unreadCount: ["notifications", "unread-count"] as const,
  preferences: ["notifications", "preferences"] as const,
  vapidStatus: ["notifications", "vapid-status"] as const,
  vapidPublicKey: ["notifications", "vapid-public-key"] as const,
  logs: (params?: object) => ["notifications", "logs", params] as const,
};

export function useNotificationInbox(params?: {
  page?: number;
  page_size?: number;
  unread_only?: boolean;
}) {
  return useQuery({
    queryKey: notificationKeys.inbox(params),
    queryFn: () => listNotifications(params),
    retry: false,
  });
}

export function useUnreadCount() {
  return useQuery({
    queryKey: notificationKeys.unreadCount,
    queryFn: getUnreadCount,
    refetchInterval: 30_000,
    retry: false,
  });
}

export function useMarkRead() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => markNotificationRead(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: notificationKeys.all });
    },
  });
}

export function useMarkAllRead() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: markAllNotificationsRead,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: notificationKeys.all });
      toast.success("Đã đánh dấu tất cả đã đọc");
    },
  });
}

export function useDeleteNotification() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => deleteNotification(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: notificationKeys.all });
    },
  });
}

export function useVapidPublicKey() {
  return useQuery({
    queryKey: notificationKeys.vapidPublicKey,
    queryFn: getVapidPublicKey,
    retry: false,
  });
}

export function useVapidStatus() {
  return useQuery({
    queryKey: notificationKeys.vapidStatus,
    queryFn: getVapidStatus,
  });
}

export function useGenerateVapidKey() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: generateVapidKey,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: notificationKeys.vapidStatus });
      toast.success("Đã tạo VAPID key thành công");
    },
    onError: () => {
      toast.error("Tạo VAPID key thất bại");
    },
  });
}

export function useSendTestNotification() {
  return useMutation({
    mutationFn: sendTestNotification,
    onSuccess: () => {
      toast.success("Đã gửi thông báo test");
    },
    onError: () => {
      toast.error("Gửi thông báo thất bại");
    },
  });
}

export function useNotificationLogs(params?: { page?: number; page_size?: number }) {
  return useQuery({
    queryKey: notificationKeys.logs(params),
    queryFn: () => listNotificationLogs(params),
  });
}

export function useNotificationPreferences() {
  return useQuery({
    queryKey: notificationKeys.preferences,
    queryFn: getNotificationPreferences,
  });
}

export function useUpdateNotificationPreferences() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: NotificationPreferenceRequest) => updateNotificationPreferences(body),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: notificationKeys.preferences });
      toast.success("Đã lưu tuỳ chọn thông báo");
    },
    onError: () => {
      toast.error("Lưu tuỳ chọn thất bại");
    },
  });
}

export function useSubscribeWebPush() {
  return useMutation({
    mutationFn: subscribeWebPush,
    onSuccess: () => {
      toast.success("Đã bật thông báo trình duyệt");
    },
    onError: () => {
      toast.error("Đăng ký thông báo thất bại");
    },
  });
}

export function useUnsubscribeWebPush() {
  return useMutation({
    mutationFn: (endpoint: string) => unsubscribeWebPush(endpoint),
    onSuccess: () => {
      toast.info("Đã tắt thông báo trình duyệt");
    },
  });
}
