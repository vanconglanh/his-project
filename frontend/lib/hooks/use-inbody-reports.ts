"use client";

import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import {
  uploadInBodyReport,
  listInBodyReports,
  confirmInBodyReport,
  type ConfirmInBodyFieldItem,
} from "@/lib/api/inbody-reports";
import { vitalKeys } from "@/lib/hooks/use-vital-signs";
import { getErrorMessage } from "@/lib/utils/errors";

export const inbodyKeys = {
  all: ["inbody-reports"] as const,
  list: (patientId: string) => [...inbodyKeys.all, "list", patientId] as const,
};

export function useInBodyReports(patientId: string) {
  return useQuery({
    queryKey: inbodyKeys.list(patientId),
    queryFn: () => listInBodyReports(patientId),
    enabled: !!patientId,
    retry: 2,
  });
}

export function useUploadInBodyReport(patientId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ file, encounterId }: { file: File; encounterId?: string }) =>
      uploadInBodyReport(patientId, file, encounterId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: inbodyKeys.list(patientId) });
    },
    onError: (e) => toast.error(getErrorMessage(e, "Đọc file InBody thất bại")),
  });
}

export function useConfirmInBodyReport(patientId: string, encounterId?: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({
      id,
      encounter_id,
      fields,
    }: {
      id: string;
      encounter_id?: string;
      fields: ConfirmInBodyFieldItem[];
    }) => confirmInBodyReport(id, { encounter_id, fields }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: inbodyKeys.list(patientId) });
      if (encounterId) {
        qc.invalidateQueries({ queryKey: vitalKeys.list(encounterId) });
        qc.invalidateQueries({ queryKey: vitalKeys.latest(encounterId) });
      }
      qc.invalidateQueries({ queryKey: vitalKeys.history(patientId) });
      toast.success("Đã lưu kết quả InBody vào hồ sơ");
    },
    onError: (e) => toast.error(getErrorMessage(e, "Lưu kết quả InBody thất bại")),
  });
}
