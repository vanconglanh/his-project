import apiClient from "./client";
import type { ApiResponse } from "./types";

// ─── Danh muc ma dung chung (CODE_MASTER / CODE_DETAIL_MASTER) ─────────────────

export interface CodeGroup {
  id: string;
  name: string;
}

export interface CodeItem {
  code: string;
  name: string;
}

/** Danh sach nhom ma: GET /api/v1/codes */
export async function getCodeGroups(): Promise<CodeGroup[]> {
  const res = await apiClient.get<ApiResponse<CodeGroup[]>>("/codes");
  return res.data.data;
}

/** Danh sach ma trong 1 nhom: GET /api/v1/codes/{groupId} */
export async function getCodeDetails(groupId: string): Promise<CodeItem[]> {
  const res = await apiClient.get<ApiResponse<CodeItem[]>>(`/codes/${groupId}`);
  return res.data.data;
}

/** Nap nhieu nhom 1 lan: GET /api/v1/codes/batch?ids=GENDER,BLOOD_TYPE */
export async function getCodeBatch(
  groupIds: string[]
): Promise<Record<string, CodeItem[]>> {
  const res = await apiClient.get<ApiResponse<Record<string, CodeItem[]>>>(
    "/codes/batch",
    { params: { ids: groupIds.join(",") } }
  );
  return res.data.data;
}
