import apiClient from "./client";
import type { ApiResponse } from "./types";

// ─── Quản trị danh mục mã (CODE_MASTER / CODE_DETAIL_MASTER) — dành cho tenant ─

export interface AdminCodeGroup {
  id: string;
  name: string;
  is_system: boolean;
  is_active: boolean;
}

export interface AdminCodeDetail {
  id: string | number;
  code: string;
  name: string;
  name_en: string | null;
  sort_order: number;
  is_active: boolean;
  is_hidden: boolean;
  is_system: boolean;
  tenant_id: number | null;
  extra: Record<string, unknown> | null;
  /** Mã riêng của tenant (override/thêm mới) — không phải mã hệ thống dùng chung. */
  is_override: boolean;
  is_default: boolean;
}

export interface CreateCodeDetailRequest {
  code: string;
  name: string;
  name_en?: string;
  sort_order?: number;
  extra?: Record<string, unknown>;
}

export interface UpdateCodeDetailRequest {
  name: string;
  name_en?: string;
  sort_order?: number;
  is_active?: boolean;
  extra?: Record<string, unknown>;
}

/** Danh sách nhóm mã: GET /api/v1/admin/codes */
export async function listAdminCodeGroups(): Promise<AdminCodeGroup[]> {
  const res = await apiClient.get<ApiResponse<AdminCodeGroup[]>>("/admin/codes");
  return res.data.data;
}

/** Danh sách giá trị trong 1 nhóm: GET /api/v1/admin/codes/{groupId}/details */
export async function listAdminCodeDetails(groupId: string): Promise<AdminCodeDetail[]> {
  const res = await apiClient.get<ApiResponse<AdminCodeDetail[]>>(
    `/admin/codes/${groupId}/details`
  );
  return res.data.data;
}

/** Thêm giá trị mới (riêng tenant): POST /api/v1/admin/codes/{groupId}/details */
export async function createAdminCodeDetail(
  groupId: string,
  body: CreateCodeDetailRequest
): Promise<AdminCodeDetail> {
  const res = await apiClient.post<ApiResponse<AdminCodeDetail>>(
    `/admin/codes/${groupId}/details`,
    body
  );
  return res.data.data;
}

/** Sửa giá trị: PUT /api/v1/admin/codes/{groupId}/details/{id} */
export async function updateAdminCodeDetail(
  groupId: string,
  id: string | number,
  body: UpdateCodeDetailRequest
): Promise<AdminCodeDetail> {
  const res = await apiClient.put<ApiResponse<AdminCodeDetail>>(
    `/admin/codes/${groupId}/details/${id}`,
    body
  );
  return res.data.data;
}

/** Ẩn/hiện giá trị (dùng cho mã hệ thống): PATCH /api/v1/admin/codes/{groupId}/details/{code}/visibility */
export async function setAdminCodeDetailVisibility(
  groupId: string,
  code: string,
  isHidden: boolean
): Promise<void> {
  await apiClient.patch(`/admin/codes/${groupId}/details/${code}/visibility`, {
    is_hidden: isHidden,
  });
}

/** Xoá giá trị (chỉ mã riêng tenant): DELETE /api/v1/admin/codes/{groupId}/details/{id} */
export async function deleteAdminCodeDetail(
  groupId: string,
  id: string | number
): Promise<void> {
  await apiClient.delete(`/admin/codes/${groupId}/details/${id}`);
}
