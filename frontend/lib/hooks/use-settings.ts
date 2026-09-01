"use client";

import { useMemo } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { isAxiosError } from "axios";
import { toast } from "sonner";
import * as settingsApi from "@/lib/api/settings";

// Cau hinh it thay doi -> cache 5 phut.
const SETTINGS_STALE_TIME = 5 * 60_000;

export const settingsKeys = {
  public: () => ["settings", "public"] as const,
  admin: () => ["settings", "admin"] as const,
};

/** Toan bo cau hinh public: map key -> value (string). */
export function usePublicSettings() {
  return useQuery({
    queryKey: settingsKeys.public(),
    queryFn: settingsApi.getPublicSettings,
    staleTime: SETTINGS_STALE_TIME,
  });
}

/**
 * Gia tri 1 cau hinh public dang so. Tra `fallback` khi dang tai/loi/khong parse duoc
 * de UI khong vo (vd ngan chan hardcode nguong duyet kho truoc khi API tra ve).
 */
export function useSettingNumber(key: string, fallback: number): number {
  const { data, isSuccess } = usePublicSettings();

  return useMemo(() => {
    if (isSuccess && data && data[key] !== undefined) {
      const parsed = Number(data[key]);
      if (!Number.isNaN(parsed)) return parsed;
    }
    return fallback;
  }, [isSuccess, data, key, fallback]);
}

/** Gia tri 1 cau hinh public dang chuoi. Tra `fallback` khi dang tai/loi. */
export function useSettingString(key: string, fallback: string): string {
  const { data, isSuccess } = usePublicSettings();

  return useMemo(() => {
    if (isSuccess && data && data[key] !== undefined) return data[key];
    return fallback;
  }, [isSuccess, data, key, fallback]);
}

// ─── Quản trị cấu hình hệ thống (admin/settings) ─────────────────────────────

/** Thông báo lỗi thân thiện, ưu tiên message từ envelope { error: { code, message } }. */
function getErrorMessage(error: unknown, fallback: string): string {
  if (isAxiosError(error)) {
    const apiError = error.response?.data?.error as
      | { code?: string; message?: string }
      | undefined;
    if (apiError?.message) return apiError.message;
  }
  return fallback;
}

/** Toàn bộ cấu hình hệ thống — dùng cho màn hình quản trị. */
export function useAdminSettings() {
  return useQuery({
    queryKey: settingsKeys.admin(),
    queryFn: settingsApi.getAdminSettings,
  });
}

export function useUpdateAdminSetting() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ key, value }: { key: string; value: string }) =>
      settingsApi.updateAdminSetting(key, value),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: settingsKeys.admin() });
      qc.invalidateQueries({ queryKey: settingsKeys.public() });
      toast.success("Đã lưu cấu hình");
    },
    onError: (error) => toast.error(getErrorMessage(error, "Lưu cấu hình thất bại")),
  });
}
