"use client";

import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import {
  listClsUploads,
  uploadCls,
  deleteClsUpload,
} from "@/lib/api/cls-uploads";
import { getErrorMessage } from "@/lib/utils/errors";

export const clsKeys = {
  all: ["cls-uploads"] as const,
  patient: (patientId: string) => [...clsKeys.all, "patient", patientId] as const,
};

export function useClsUploads(patientId: string) {
  return useQuery({
    queryKey: clsKeys.patient(patientId),
    queryFn: () => listClsUploads(patientId),
    enabled: !!patientId,
  });
}

export function useUploadCls(patientId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({
      file,
      docType,
      encounterId,
      note,
    }: {
      file: File;
      docType: string;
      encounterId?: string;
      note?: string;
    }) => uploadCls(patientId, file, docType, encounterId, note),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: clsKeys.patient(patientId) });
      toast.success("Upload kết quả CLS thành công");
    },
    onError: (e) => toast.error(getErrorMessage(e)),
  });
}

export function useDeleteClsUpload(patientId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => deleteClsUpload(patientId, id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: clsKeys.patient(patientId) });
      toast.success("Đã xoá tài liệu CLS");
    },
    onError: (e) => toast.error(getErrorMessage(e)),
  });
}
