"use client";

import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import * as encountersApi from "@/lib/api/encounters";
import type {
  ListEncountersParams,
} from "@/lib/api/encounters";
import type { EncounterCreateRequest, EncounterUpdateRequest, DiagnosisRequest } from "@/lib/api/types";

export const encounterKeys = {
  all: ["encounters"] as const,
  list: (params?: ListEncountersParams) => ["encounters", "list", params] as const,
  detail: (id: string) => ["encounters", "detail", id] as const,
  timeline: (id: string) => ["encounters", "timeline", id] as const,
  over12h: () => ["encounters", "over-12h"] as const,
};

export function useEncounters(params?: ListEncountersParams) {
  return useQuery({
    queryKey: encounterKeys.list(params),
    queryFn: () => encountersApi.listEncounters(params),
    staleTime: 30_000,
    retry: 2,
  });
}

export function useEncounter(id: string) {
  return useQuery({
    queryKey: encounterKeys.detail(id),
    queryFn: () => encountersApi.getEncounter(id),
    staleTime: 30_000,
    enabled: !!id,
    retry: 2,
  });
}

export function useEncounterTimeline(encounterId: string) {
  return useQuery({
    queryKey: encounterKeys.timeline(encounterId),
    queryFn: () => encountersApi.getEncounterTimeline(encounterId),
    enabled: !!encounterId,
    staleTime: 15_000,
  });
}

export function useOver12hAlerts() {
  return useQuery({
    queryKey: encounterKeys.over12h(),
    queryFn: encountersApi.listOver12hAlerts,
    staleTime: 60_000,
    refetchInterval: 5 * 60_000,
  });
}

export function useCreateEncounter() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: EncounterCreateRequest) => encountersApi.createEncounter(body),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: encounterKeys.all });
      toast.success("Tạo lượt khám thành công");
    },
    onError: () => toast.error("Tạo lượt khám thất bại"),
  });
}

export function useUpdateEncounter(id: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: EncounterUpdateRequest) => encountersApi.updateEncounter(id, body),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: encounterKeys.detail(id) });
      toast.success("Cập nhật lượt khám thành công");
    },
    onError: () => toast.error("Cập nhật lượt khám thất bại"),
  });
}

export function useStartEncounter(id: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: () => encountersApi.startEncounter(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: encounterKeys.detail(id) });
      qc.invalidateQueries({ queryKey: encounterKeys.all });
      toast.success("Bắt đầu khám");
    },
    onError: () => toast.error("Không thể bắt đầu khám"),
  });
}

export function useCloseEncounter(id: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: () => encountersApi.closeEncounter(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: encounterKeys.detail(id) });
      qc.invalidateQueries({ queryKey: encounterKeys.all });
      toast.success("Đã đóng lượt khám");
    },
    onError: (err: unknown) => {
      const msg =
        (err as { response?: { data?: { error?: { code?: string } } } })?.response?.data?.error?.code;
      if (msg === "ENCOUNTER_MISSING_DIAGNOSIS")
        toast.error("Cần có ít nhất 1 chẩn đoán chính (PRIMARY)");
      else if (msg === "EMR_NOT_SIGNED")
        toast.error("Bệnh án chưa được ký số");
      else toast.error("Đóng lượt khám thất bại");
    },
  });
}

export function useAddDiagnosis(encounterId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: DiagnosisRequest) => encountersApi.addDiagnosis(encounterId, body),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: encounterKeys.detail(encounterId) });
      toast.success("Đã thêm chẩn đoán");
    },
    onError: () => toast.error("Thêm chẩn đoán thất bại"),
  });
}

export function useDeleteDiagnosis(encounterId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (diagnosisId: string) => encountersApi.deleteDiagnosis(encounterId, diagnosisId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: encounterKeys.detail(encounterId) });
      toast.success("Đã xóa chẩn đoán");
    },
    onError: () => toast.error("Xóa chẩn đoán thất bại"),
  });
}
