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
    // BM-27 (Lark Bug-Feedback): cac ma loi o day KHONG khop voi ma that su tra ve tu
    // CloseEncounterCommandHandler (DIAGNOSIS_REQUIRED, DIAGNOSIS_PRIMARY_REQUIRED,
    // ENCOUNTER_MISSING_CHIEF_COMPLAINT...) nen luon roi vao nhanh "That bai" chung chung -
    // tester ky so xong van khong Ket thuc kham duoc ma khong biet ly do that. Map dung tung
    // ma loi backend sang thong bao cu the, huong dan ro buoc can lam tiep theo.
    onError: (err: unknown) => {
      const code =
        (err as { response?: { data?: { error?: { code?: string } } } })?.response?.data?.error?.code;
      switch (code) {
        case "ENCOUNTER_MISSING_CHIEF_COMPLAINT":
          toast.error("Vui lòng nhập lý do khám chính trước khi kết thúc khám");
          break;
        case "DIAGNOSIS_REQUIRED":
          toast.error("Vui lòng thêm ít nhất 1 chẩn đoán trước khi kết thúc khám");
          break;
        case "DIAGNOSIS_PRIMARY_REQUIRED":
          toast.error("Vui lòng chọn đúng 1 chẩn đoán chính (không được nhiều hơn hoặc ít hơn 1)");
          break;
        case "EMR_NOT_SIGNED":
          toast.error("Vui lòng ký số bệnh án trước khi kết thúc khám");
          break;
        case "ENCOUNTER_INVALID_TRANSITION":
          toast.error("Lượt khám đang ở trạng thái không cho phép kết thúc khám");
          break;
        case "ENCOUNTER_NOT_FOUND":
          toast.error("Không tìm thấy lượt khám");
          break;
        default:
          toast.error("Kết thúc khám thất bại, vui lòng thử lại hoặc liên hệ kỹ thuật");
      }
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

// ─── Khoá bệnh án & bản đính chính (G03) ─────────────────────────────────────

export const encounterLockKeys = {
  lockState: (id: string) => ["encounters", "lock-state", id] as const,
  addenda: (id: string) => ["encounters", "addenda", id] as const,
};

export function useEncounterLockState(id: string) {
  return useQuery({
    queryKey: encounterLockKeys.lockState(id),
    queryFn: () => encountersApi.getEncounterLockState(id),
    enabled: !!id,
    staleTime: 30_000,
    retry: 1,
  });
}

export function useEncounterAddenda(id: string, enabled = true) {
  return useQuery({
    queryKey: encounterLockKeys.addenda(id),
    queryFn: () => encountersApi.listEncounterAddenda(id),
    enabled: !!id && enabled,
    staleTime: 30_000,
    retry: 1,
  });
}

export function useCreateEncounterAddendum(id: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: encountersApi.CreateAddendumRequest) =>
      encountersApi.createEncounterAddendum(id, body),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: encounterLockKeys.addenda(id) });
      qc.invalidateQueries({ queryKey: encounterLockKeys.lockState(id) });
      qc.invalidateQueries({ queryKey: encounterKeys.detail(id) });
      toast.success("Đã tạo bản đính chính");
    },
    onError: (err: unknown) => {
      const code = (err as { response?: { data?: { error?: { code?: string } } } })?.response?.data
        ?.error?.code;
      if (code === "FORBIDDEN") toast.error("Bạn không có quyền tạo bản đính chính");
      else toast.error("Tạo bản đính chính thất bại");
    },
  });
}
