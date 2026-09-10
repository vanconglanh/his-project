"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import * as templatesApi from "@/lib/api/cls-order-templates";
import type { CreateClsOrderTemplateRequest } from "@/lib/api/cls-order-templates";

// BM-12: hooks quan ly bo chi dinh mau CLS - danh sach KHONG theo encounter (rieng tung
// bac si dang dang nhap, xem BE ClsOrderTemplatesController).
export const clsOrderTemplateKeys = {
  all: ["cls-order-templates"] as const,
};

function extractErrorMessage(err: unknown, fallback: string): string {
  const message = (err as { response?: { data?: { error?: { message?: string } } } })?.response?.data
    ?.error?.message;
  return message || fallback;
}

export function useClsOrderTemplates() {
  return useQuery({
    queryKey: clsOrderTemplateKeys.all,
    queryFn: templatesApi.listClsOrderTemplates,
    staleTime: 30_000,
  });
}

export function useCreateClsOrderTemplate() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: CreateClsOrderTemplateRequest) => templatesApi.createClsOrderTemplate(body),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: clsOrderTemplateKeys.all });
      toast.success("Đã lưu bộ chỉ định mẫu");
    },
    onError: (err) => toast.error(extractErrorMessage(err, "Lưu bộ chỉ định mẫu thất bại")),
  });
}

export function useDeleteClsOrderTemplate() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => templatesApi.deleteClsOrderTemplate(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: clsOrderTemplateKeys.all });
      toast.success("Đã xoá bộ chỉ định mẫu");
    },
    onError: (err) => toast.error(extractErrorMessage(err, "Xoá bộ chỉ định mẫu thất bại")),
  });
}
