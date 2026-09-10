import apiClient from "./client";
import type { ApiResponse } from "./types";
import type { ClsCatalogItem } from "./types";

// BM-12 (Lark Bug-Feedback): Bo chi dinh mau CLS, luu RIENG tung bac si (PO xac nhan
// 2026-09-10). Endpoint backend: /cls-order-templates.

/** Cart item luu trong template: nguyen ClsCatalogItem da chon + co dang "khong thu phi" hay khong. */
export interface ClsOrderTemplateItem extends ClsCatalogItem {
  is_free?: boolean;
}

export interface ClsOrderTemplate {
  id: string;
  name: string;
  items: ClsOrderTemplateItem[];
  created_at: string;
}

export interface CreateClsOrderTemplateRequest {
  name: string;
  items: ClsOrderTemplateItem[];
}

export async function listClsOrderTemplates(): Promise<ClsOrderTemplate[]> {
  const res = await apiClient.get<{ data: ClsOrderTemplate[] }>("/cls-order-templates");
  return res.data.data ?? [];
}

export async function createClsOrderTemplate(body: CreateClsOrderTemplateRequest) {
  const res = await apiClient.post<ApiResponse<ClsOrderTemplate>>("/cls-order-templates", body);
  return res.data.data;
}

export async function deleteClsOrderTemplate(id: string) {
  await apiClient.delete(`/cls-order-templates/${id}`);
}
