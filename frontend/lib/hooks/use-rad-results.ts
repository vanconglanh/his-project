"use client";

import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import {
  listRadResults,
  createRadResult,
  updateRadResult,
  verifyRadResult,
  uploadDicomFiles,
  type RadResultListParams,
  type RadResultCreateRequest,
  type RadResultUpdateRequest,
} from "@/lib/api/rad-results";
import { getErrorMessage } from "@/lib/utils/errors";

export const radResultKeys = {
  all: ["rad-results"] as const,
  list: (params?: RadResultListParams) => [...radResultKeys.all, "list", params] as const,
};

export function useRadResults(params?: RadResultListParams) {
  return useQuery({
    queryKey: radResultKeys.list(params),
    queryFn: () => listRadResults(params),
    retry: 2,
  });
}

export function useCreateRadResult() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: RadResultCreateRequest) => createRadResult(body),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: radResultKeys.all });
      toast.success("Đã nhập kết quả CĐHA");
    },
    onError: (e) => toast.error(getErrorMessage(e)),
  });
}

export function useUpdateRadResult(id: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: RadResultUpdateRequest) => updateRadResult(id, body),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: radResultKeys.all });
      toast.success("Đã cập nhật kết quả CĐHA");
    },
    onError: (e) => toast.error(getErrorMessage(e)),
  });
}

export function useVerifyRadResult() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => verifyRadResult(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: radResultKeys.all });
      toast.success("Đã ký phát hành kết quả CĐHA");
    },
    onError: (e) => toast.error(getErrorMessage(e)),
  });
}

export function useUploadDicom(radResultId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (files: File[]) => uploadDicomFiles(radResultId, files),
    onSuccess: (data) => {
      qc.invalidateQueries({ queryKey: radResultKeys.all });
      toast.success(`Đã tải lên ${data.uploaded_count} ảnh DICOM`);
    },
    onError: (e) => toast.error(getErrorMessage(e)),
  });
}
