"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import * as recallApi from "@/lib/api/recall";
import type { NotifyRecallRequest, RecallListParams, UpdateRecallStatusRequest } from "@/lib/api/types";

export const recallKeys = {
  all: ["recall"] as const,
  list: (params?: RecallListParams) => [...recallKeys.all, "list", params] as const,
};

export function useRecallList(params?: RecallListParams) {
  return useQuery({
    queryKey: recallKeys.list(params),
    queryFn: () => recallApi.listRecall(params),
    staleTime: 30_000,
    retry: 1,
  });
}

export function useUpdateRecallStatus() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, body }: { id: string; body: UpdateRecallStatusRequest }) =>
      recallApi.updateRecallStatus(id, body),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: recallKeys.all });
      toast.success("Đã cập nhật trạng thái nhắc tái khám");
    },
    onError: () => toast.error("Cập nhật trạng thái thất bại"),
  });
}

export function useNotifyRecall() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, body }: { id: string; body?: NotifyRecallRequest }) => recallApi.notifyRecall(id, body),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: recallKeys.all });
      toast.success("Đã gửi nhắc lịch cho bệnh nhân");
    },
    onError: () => toast.error("Gửi nhắc lịch thất bại"),
  });
}
