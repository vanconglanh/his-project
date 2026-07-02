"use client";

import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import {
  listLabResults,
  createLabResult,
  updateLabResult,
  verifyLabResult,
  unverifyLabResult,
  importLabResults,
  listAbnormalLabResults,
  getLabResultTrend,
  batchVerifyLabResults,
  type LabResultListParams,
  type LabResultCreateRequest,
  type LabResultUpdateRequest,
} from "@/lib/api/lab-results";
import { getErrorMessage } from "@/lib/utils/errors";

export const labResultKeys = {
  all: ["lab-results"] as const,
  list: (params?: LabResultListParams) => [...labResultKeys.all, "list", params] as const,
  abnormal: (params?: object) => [...labResultKeys.all, "abnormal", params] as const,
  trend: (patientId: string, testCode: string) => [...labResultKeys.all, "trend", patientId, testCode] as const,
};

export function useLabResults(params?: LabResultListParams) {
  return useQuery({
    queryKey: labResultKeys.list(params),
    queryFn: () => listLabResults(params),
    retry: 2,
  });
}

export function useAbnormalLabResults(params?: { severity?: "ALL" | "CRITICAL_ONLY"; from_date?: string; to_date?: string }) {
  return useQuery({
    queryKey: labResultKeys.abnormal(params),
    queryFn: () => listAbnormalLabResults(params),
    retry: 2,
  });
}

export function useLabResultTrend(patientId: string, testCode: string, enabled = true) {
  return useQuery({
    queryKey: labResultKeys.trend(patientId, testCode),
    queryFn: () => getLabResultTrend({ patient_id: patientId, test_code: testCode }),
    enabled: enabled && !!patientId && !!testCode,
    staleTime: 60_000,
  });
}

export function useCreateLabResult() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: LabResultCreateRequest) => createLabResult(body),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: labResultKeys.all });
      toast.success("Đã nhập kết quả xét nghiệm");
    },
    onError: (e) => toast.error(getErrorMessage(e)),
  });
}

export function useUpdateLabResult(id: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: LabResultUpdateRequest) => updateLabResult(id, body),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: labResultKeys.all });
      toast.success("Đã cập nhật kết quả xét nghiệm");
    },
    onError: (e) => toast.error(getErrorMessage(e)),
  });
}

export function useVerifyLabResult() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => verifyLabResult(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: labResultKeys.all });
      toast.success("Đã xác thực kết quả");
    },
    onError: (e) => toast.error(getErrorMessage(e)),
  });
}

export function useUnverifyLabResult() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => unverifyLabResult(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: labResultKeys.all });
      toast.success("Đã hủy xác thực kết quả");
    },
    onError: (e) => toast.error(getErrorMessage(e)),
  });
}

export function useImportLabResults() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ file, format, auto_verify }: { file: File; format: "CSV" | "HL7_ORU"; auto_verify?: boolean }) =>
      importLabResults(file, format, auto_verify),
    onSuccess: (data) => {
      qc.invalidateQueries({ queryKey: labResultKeys.all });
      toast.success(`Import thành công: ${data.success_count} dòng, thất bại: ${data.failed_count} dòng`);
    },
    onError: (e) => toast.error(getErrorMessage(e)),
  });
}

export function useBatchVerifyLabResults() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (ids: string[]) => batchVerifyLabResults(ids),
    onSuccess: (data) => {
      qc.invalidateQueries({ queryKey: labResultKeys.all });
      toast.success(`Đã xác thực ${data.success_count} kết quả`);
    },
    onError: (e) => toast.error(getErrorMessage(e)),
  });
}
