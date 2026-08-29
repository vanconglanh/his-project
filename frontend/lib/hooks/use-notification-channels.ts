import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import {
  listNotificationChannels,
  createNotificationChannel,
  updateNotificationChannel,
  deleteNotificationChannel,
  testNotificationChannel,
  type NotificationChannelRequest,
} from "../api/notification-channels";
import { getErrorMessage } from "../utils/errors";

export const notificationChannelKeys = {
  all: ["notification-channels"] as const,
  list: () => [...notificationChannelKeys.all, "list"] as const,
};

export function useNotificationChannels() {
  return useQuery({
    queryKey: notificationChannelKeys.list(),
    queryFn: listNotificationChannels,
  });
}

export function useCreateNotificationChannel() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: NotificationChannelRequest) => createNotificationChannel(body),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: notificationChannelKeys.list() });
      toast.success("Đã tạo kênh thông báo");
    },
    onError: (err) => toast.error(getErrorMessage(err, "Tạo kênh thất bại")),
  });
}

export function useUpdateNotificationChannel() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, body }: { id: string; body: NotificationChannelRequest }) =>
      updateNotificationChannel(id, body),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: notificationChannelKeys.list() });
      toast.success("Đã cập nhật kênh thông báo");
    },
    onError: (err) => toast.error(getErrorMessage(err, "Cập nhật thất bại")),
  });
}

export function useDeleteNotificationChannel() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => deleteNotificationChannel(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: notificationChannelKeys.list() });
      toast.success("Đã xóa (reset) cấu hình kênh");
    },
    onError: (err) => toast.error(getErrorMessage(err, "Xóa thất bại")),
  });
}

export function useTestNotificationChannel() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => testNotificationChannel(id),
    onSuccess: (result) => {
      qc.invalidateQueries({ queryKey: notificationChannelKeys.list() });
      if (result.ok) {
        toast.success(result.message || "Kết nối thành công");
      } else {
        toast.error(result.message || "Kết nối thất bại");
      }
    },
    onError: (err) => toast.error(getErrorMessage(err, "Test kết nối thất bại")),
  });
}
