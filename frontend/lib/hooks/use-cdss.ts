"use client";

import { useMutation, useQuery } from "@tanstack/react-query";
import { toast } from "sonner";
import * as cdssApi from "@/lib/api/cdss";
import type { CdssCheckRequest, CdssOverrideRequest } from "@/lib/api/types";

export const cdssKeys = {
  check: (body: CdssCheckRequest) => ["cdss", "check", body] as const,
};

/**
 * Kiểm tra CDSS (tương tác thuốc, chống chỉ định...) theo danh sách thuốc hiện tại.
 * Component gọi hook này nên tự debounce items trước khi truyền vào.
 */
export function useCdssCheck(body: CdssCheckRequest, enabled: boolean) {
  return useQuery({
    queryKey: cdssKeys.check(body),
    queryFn: () => cdssApi.checkCdss(body),
    enabled,
    staleTime: 0,
    retry: 1,
  });
}

export function useCdssOverride() {
  return useMutation({
    mutationFn: (body: CdssOverrideRequest) => cdssApi.overrideCdss(body),
    onSuccess: () => toast.success("Đã ghi nhận lý do bỏ qua cảnh báo"),
    onError: () => toast.error("Không thể ghi nhận bỏ qua cảnh báo"),
  });
}
