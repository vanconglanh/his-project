import apiClient from "./client";
import type { ApiResponse } from "./types";

// ─── Cấu hình hệ thống (his_settings) ───────────────────────────────────────

/** Cấu hình public: map key -> value (string). GET /api/v1/settings/public */
export async function getPublicSettings(): Promise<Record<string, string>> {
  const res = await apiClient.get<ApiResponse<Record<string, string>>>("/settings/public");
  return res.data.data;
}

export type SettingDataType = "int" | "decimal" | "bool" | "string";

export interface AdminSettingItem {
  key: string;
  label_vi: string;
  description_vi: string | null;
  data_type: SettingDataType;
  value_group: string;
  value: string;
  default_value: string;
  is_overridden: boolean;
  is_public: boolean;
}

/** Danh sách toàn bộ cấu hình cho admin: GET /api/v1/admin/settings */
export async function getAdminSettings(): Promise<AdminSettingItem[]> {
  const res = await apiClient.get<ApiResponse<AdminSettingItem[]>>("/admin/settings");
  return res.data.data;
}

/** Cập nhật 1 cấu hình: PUT /api/v1/admin/settings/{key} */
export async function updateAdminSetting(key: string, value: string): Promise<AdminSettingItem> {
  const res = await apiClient.put<ApiResponse<AdminSettingItem>>(`/admin/settings/${key}`, {
    value,
  });
  return res.data.data;
}
