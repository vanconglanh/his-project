"use client";

import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import * as diabetesApi from "@/lib/api/diabetes";
import type { DiabetesAssessmentRequest } from "@/lib/api/types";

export const diabetesKeys = {
  assessment: (encounterId: string) => ["diabetes", "assessment", encounterId] as const,
  history: (patientId: string) => ["diabetes", "history", patientId] as const,
};

export function useDiabetesAssessment(encounterId: string) {
  return useQuery({
    queryKey: diabetesKeys.assessment(encounterId),
    queryFn: () => diabetesApi.getDiabetesAssessment(encounterId),
    enabled: !!encounterId,
    staleTime: 60_000,
    retry: 1,
  });
}

export function useDiabetesHistory(patientId: string, params?: { date_from?: string; date_to?: string }) {
  return useQuery({
    queryKey: [...diabetesKeys.history(patientId), params],
    queryFn: () => diabetesApi.getDiabetesHistory(patientId, params),
    enabled: !!patientId,
    staleTime: 60_000,
  });
}

export function useCreateDiabetesAssessment(encounterId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: DiabetesAssessmentRequest) =>
      diabetesApi.createDiabetesAssessment(encounterId, body),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: diabetesKeys.assessment(encounterId) });
      toast.success("Lưu đánh giá ĐTĐ thành công");
    },
    onError: () => toast.error("Lưu đánh giá ĐTĐ thất bại"),
  });
}

export function useUpdateDiabetesAssessment(encounterId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: DiabetesAssessmentRequest) =>
      diabetesApi.updateDiabetesAssessment(encounterId, body),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: diabetesKeys.assessment(encounterId) });
      toast.success("Cập nhật đánh giá ĐTĐ thành công");
    },
    onError: () => toast.error("Cập nhật đánh giá ĐTĐ thất bại"),
  });
}
