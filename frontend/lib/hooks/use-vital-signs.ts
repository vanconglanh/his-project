"use client";

import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import * as vitalApi from "@/lib/api/vital-signs";
import type { VitalSignsRequest } from "@/lib/api/types";

export const vitalKeys = {
  list: (encounterId: string) => ["vital-signs", "list", encounterId] as const,
  latest: (encounterId: string) => ["vital-signs", "latest", encounterId] as const,
  history: (patientId: string) => ["vital-signs", "history", patientId] as const,
};

export function useVitalSigns(encounterId: string) {
  return useQuery({
    queryKey: vitalKeys.list(encounterId),
    queryFn: () => vitalApi.listVitalSigns(encounterId),
    enabled: !!encounterId,
    staleTime: 30_000,
    retry: 2,
  });
}

export function useLatestVitalSigns(encounterId: string) {
  return useQuery({
    queryKey: vitalKeys.latest(encounterId),
    queryFn: () => vitalApi.getLatestVitalSign(encounterId),
    enabled: !!encounterId,
    staleTime: 30_000,
    retry: 2,
  });
}

export function useVitalSignsHistory(patientId: string, params?: { date_from?: string; date_to?: string; metric?: string }) {
  return useQuery({
    queryKey: [...vitalKeys.history(patientId), params],
    queryFn: () => vitalApi.getVitalSignHistory(patientId, params),
    enabled: !!patientId,
    staleTime: 60_000,
  });
}

export function useCreateVitalSigns(encounterId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: VitalSignsRequest) => vitalApi.createVitalSign(encounterId, body),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: vitalKeys.list(encounterId) });
      qc.invalidateQueries({ queryKey: vitalKeys.latest(encounterId) });
      toast.success("Đã lưu sinh hiệu");
    },
    onError: () => toast.error("Lưu sinh hiệu thất bại"),
  });
}

export function useUpdateVitalSign(encounterId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, body }: { id: string; body: VitalSignsRequest }) =>
      vitalApi.updateVitalSign(id, body),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: vitalKeys.list(encounterId) });
      qc.invalidateQueries({ queryKey: vitalKeys.latest(encounterId) });
      toast.success("Cập nhật sinh hiệu thành công");
    },
    onError: (err: unknown) => {
      const code = (err as { response?: { data?: { error?: { code?: string } } } })?.response?.data?.error?.code;
      if (code === "VITAL_EDIT_TIMEOUT") toast.error("Chỉ có thể sửa sinh hiệu trong 24h");
      else toast.error("Cập nhật sinh hiệu thất bại");
    },
  });
}
