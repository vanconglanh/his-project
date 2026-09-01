"use client";

import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { isAxiosError } from "axios";
import { toast } from "sonner";
import * as adminCodesApi from "@/lib/api/admin-codes";
import type {
  CreateCodeDetailRequest,
  UpdateCodeDetailRequest,
} from "@/lib/api/admin-codes";
import { codeKeys } from "./use-codes";

/** Thông báo lỗi thân thiện, ưu tiên message từ envelope { error: { code, message } }. */
function getErrorMessage(error: unknown, fallback: string): string {
  if (isAxiosError(error)) {
    const apiError = error.response?.data?.error as
      | { code?: string; message?: string }
      | undefined;
    if (apiError?.code === "CODE_DUPLICATED") {
      return apiError.message ?? "Mã đã tồn tại trong nhóm này.";
    }
    if (apiError?.code === "CODE_IS_SYSTEM_READONLY") {
      return apiError.message ?? "Mã hệ thống chỉ có thể ẩn, không thể sửa/xoá.";
    }
    if (apiError?.message) return apiError.message;
  }
  return fallback;
}

export const adminCodeKeys = {
  groups: () => ["admin-codes", "groups"] as const,
  details: (groupId: string) => ["admin-codes", "details", groupId] as const,
};

export function useAdminCodeGroups() {
  return useQuery({
    queryKey: adminCodeKeys.groups(),
    queryFn: adminCodesApi.listAdminCodeGroups,
  });
}

export function useAdminCodeDetails(groupId: string) {
  return useQuery({
    queryKey: adminCodeKeys.details(groupId),
    queryFn: () => adminCodesApi.listAdminCodeDetails(groupId),
    enabled: !!groupId,
  });
}

/** Sau khi sửa danh mục -> invalidate luôn cache useCodes public để dropdown cập nhật ngay. */
function invalidateAll(qc: ReturnType<typeof useQueryClient>, groupId: string) {
  qc.invalidateQueries({ queryKey: adminCodeKeys.details(groupId) });
  qc.invalidateQueries({ queryKey: codeKeys.items(groupId) });
}

export function useCreateAdminCodeDetail(groupId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: CreateCodeDetailRequest) =>
      adminCodesApi.createAdminCodeDetail(groupId, body),
    onSuccess: () => {
      invalidateAll(qc, groupId);
      toast.success("Đã thêm giá trị mới");
    },
    onError: (error) => toast.error(getErrorMessage(error, "Thêm giá trị thất bại")),
  });
}

export function useUpdateAdminCodeDetail(groupId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, body }: { id: string | number; body: UpdateCodeDetailRequest }) =>
      adminCodesApi.updateAdminCodeDetail(groupId, id, body),
    onSuccess: () => {
      invalidateAll(qc, groupId);
      toast.success("Đã cập nhật giá trị");
    },
    onError: (error) => toast.error(getErrorMessage(error, "Cập nhật giá trị thất bại")),
  });
}

export function useSetAdminCodeDetailVisibility(groupId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ code, isHidden }: { code: string; isHidden: boolean }) =>
      adminCodesApi.setAdminCodeDetailVisibility(groupId, code, isHidden),
    onSuccess: (_data, variables) => {
      invalidateAll(qc, groupId);
      toast.success(variables.isHidden ? "Đã ẩn giá trị" : "Đã hiện giá trị");
    },
    onError: (error) => toast.error(getErrorMessage(error, "Cập nhật hiển thị thất bại")),
  });
}

export function useDeleteAdminCodeDetail(groupId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string | number) => adminCodesApi.deleteAdminCodeDetail(groupId, id),
    onSuccess: () => {
      invalidateAll(qc, groupId);
      toast.success("Đã xoá giá trị");
    },
    onError: (error) => toast.error(getErrorMessage(error, "Xoá giá trị thất bại")),
  });
}
