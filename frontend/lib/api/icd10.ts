import apiClient from "./client";
import type { ApiResponse, Icd10Response, Icd10Category } from "./types";

export async function searchIcd10(params: {
  q: string;
  type?: "code" | "name" | "all";
  category?: string;
  billable_only?: boolean;
  limit?: number;
}) {
  const res = await apiClient.get<ApiResponse<Icd10Response[]>>("/icd10/search", { params });
  return res.data.data;
}

export async function getIcd10(code: string) {
  const res = await apiClient.get<ApiResponse<Icd10Response>>(`/icd10/${code}`);
  return res.data.data;
}

export async function listIcd10Categories() {
  const res = await apiClient.get<ApiResponse<Icd10Category[]>>("/icd10/categories");
  return res.data.data;
}
