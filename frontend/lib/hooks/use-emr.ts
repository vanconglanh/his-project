"use client";

import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import * as emrApi from "@/lib/api/emr";
import type { EmrSaveRequest, SignEmrRequest, EmrTemplateRequest } from "@/lib/api/types";

export const emrKeys = {
  content: (encounterId: string) => ["emr", "content", encounterId] as const,
  versions: (encounterId: string) => ["emr", "versions", encounterId] as const,
  templates: (params?: object) => ["emr", "templates", params] as const,
};

export function useEmr(encounterId: string) {
  return useQuery({
    queryKey: emrKeys.content(encounterId),
    queryFn: () => emrApi.getEmr(encounterId),
    enabled: !!encounterId,
    staleTime: 60_000,
    retry: 1,
  });
}

export function useSaveEmrDraft(encounterId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: EmrSaveRequest) => emrApi.saveEmrDraft(encounterId, body),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: emrKeys.content(encounterId) });
    },
    onError: (err: unknown) => {
      const code = (err as { response?: { data?: { error?: { code?: string } } } })?.response?.data?.error?.code;
      if (code === "EMR_ALREADY_SIGNED") toast.error("Bệnh án đã ký số, không thể sửa");
      else toast.error("Lưu bệnh án thất bại");
    },
  });
}

export function useSignEmr(encounterId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: SignEmrRequest) => emrApi.signEmr(encounterId, body),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: emrKeys.content(encounterId) });
      qc.invalidateQueries({ queryKey: emrKeys.versions(encounterId) });
      toast.success("Ký số bệnh án thành công");
    },
    onError: () => toast.error("Ký số bệnh án thất bại"),
  });
}

export function useEmrVersions(encounterId: string) {
  return useQuery({
    queryKey: emrKeys.versions(encounterId),
    queryFn: () => emrApi.getEmrVersions(encounterId),
    enabled: !!encounterId,
    staleTime: 60_000,
  });
}

export function useEmrTemplates(params?: { speciality?: string; is_system?: boolean }) {
  return useQuery({
    queryKey: emrKeys.templates(params),
    queryFn: () => emrApi.listEmrTemplates(params),
    staleTime: 5 * 60_000,
  });
}

export function useCreateEmrTemplate() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: EmrTemplateRequest) => emrApi.createEmrTemplate(body),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: emrKeys.templates() });
      toast.success("Tạo mẫu bệnh án thành công");
    },
    onError: () => toast.error("Tạo mẫu bệnh án thất bại"),
  });
}

export function useDeleteEmrTemplate() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => emrApi.deleteEmrTemplate(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: emrKeys.templates() });
      toast.success("Xóa mẫu bệnh án thành công");
    },
    onError: () => toast.error("Xóa mẫu bệnh án thất bại"),
  });
}
