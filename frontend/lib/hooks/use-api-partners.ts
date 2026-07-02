"use client";

import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import {
  listApiPartners,
  getApiPartner,
  createApiPartner,
  updateApiPartner,
  deleteApiPartner,
  regenerateApiKey,
  testApiPartnerCall,
  getApiPartnerUsageStats,
  getApiPartnerRequestLogs,
} from "@/lib/api/api-partners";
import type {
  ListApiPartnersParams,
  ApiPartnerCreateRequest,
  ApiPartnerUpdateRequest,
} from "@/lib/api/api-partners";

export const apiPartnerKeys = {
  all: ["api-partners"] as const,
  list: (params?: ListApiPartnersParams) => ["api-partners", "list", params] as const,
  detail: (id: string) => ["api-partners", "detail", id] as const,
  usage: (id: string, from?: string, to?: string) =>
    ["api-partners", "usage", id, from, to] as const,
  logs: (id: string, page?: number, statusCode?: number) =>
    ["api-partners", "logs", id, page, statusCode] as const,
};

export function useApiPartners(params?: ListApiPartnersParams) {
  return useQuery({
    queryKey: apiPartnerKeys.list(params),
    queryFn: () => listApiPartners(params),
  });
}

export function useApiPartner(id: string) {
  return useQuery({
    queryKey: apiPartnerKeys.detail(id),
    queryFn: () => getApiPartner(id),
    enabled: !!id,
  });
}

export function useApiPartnerUsageStats(id: string, from?: string, to?: string) {
  return useQuery({
    queryKey: apiPartnerKeys.usage(id, from, to),
    queryFn: () => getApiPartnerUsageStats(id, { from, to }),
    enabled: !!id,
  });
}

export function useApiPartnerRequestLogs(id: string, page = 1, statusCode?: number) {
  return useQuery({
    queryKey: apiPartnerKeys.logs(id, page, statusCode),
    queryFn: () => getApiPartnerRequestLogs(id, { page, status_code: statusCode }),
    enabled: !!id,
  });
}

export function useCreateApiPartner() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: ApiPartnerCreateRequest) => createApiPartner(body),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: apiPartnerKeys.all });
    },
    onError: () => {
      toast.error("Tạo đối tác thất bại");
    },
  });
}

export function useUpdateApiPartner(id: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: ApiPartnerUpdateRequest) => updateApiPartner(id, body),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: apiPartnerKeys.all });
      toast.success("Cập nhật đối tác thành công");
    },
    onError: () => {
      toast.error("Cập nhật đối tác thất bại");
    },
  });
}

export function useDeleteApiPartner() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => deleteApiPartner(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: apiPartnerKeys.all });
      toast.success("Đã xoá đối tác");
    },
    onError: () => {
      toast.error("Xoá đối tác thất bại");
    },
  });
}

export function useRegenerateApiKey() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => regenerateApiKey(id),
    onSuccess: (_, id) => {
      qc.invalidateQueries({ queryKey: apiPartnerKeys.detail(id) });
      qc.invalidateQueries({ queryKey: apiPartnerKeys.all });
    },
    onError: () => {
      toast.error("Tạo lại API key thất bại");
    },
  });
}

export function useTestApiPartnerCall() {
  return useMutation({
    mutationFn: (id: string) => testApiPartnerCall(id),
    onSuccess: () => {
      toast.success("Test call thành công");
    },
    onError: () => {
      toast.error("Test call thất bại");
    },
  });
}
