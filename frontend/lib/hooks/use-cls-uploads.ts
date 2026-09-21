"use client";

import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { useEffect, useState } from "react";
import {
  listClsUploads,
  uploadCls,
  deleteClsUpload,
  getClsUploadContentBlobUrl,
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

// BM-26 (Lark Bug-Feedback): tra ve Blob URL cuc bo cho 1 file CLS (thay vi signed_url MinIO
// noi bo khong the truy cap tu trinh duyet - xem ghi chu chi tiet o getClsUploadContentBlobUrl).
// Dung useState/useEffect thay vi useQuery: can revoke object URL dung luc unmount/doi item,
// react-query khong co hook lifecycle phu hop cho viec don dep tai nguyen trinh duyet nay.
export function useClsUploadContentUrl(patientId: string, id: string | undefined) {
  const [url, setUrl] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(false);

  useEffect(() => {
    if (!patientId || !id) return;
    let cancelled = false;
    let objectUrl: string | null = null;
    setIsLoading(true);
    getClsUploadContentBlobUrl(patientId, id)
      .then((blobUrl) => {
        if (cancelled) {
          URL.revokeObjectURL(blobUrl);
          return;
        }
        objectUrl = blobUrl;
        setUrl(blobUrl);
      })
      .catch(() => {
        if (!cancelled) setUrl(null);
      })
      .finally(() => {
        if (!cancelled) setIsLoading(false);
      });

    return () => {
      cancelled = true;
      if (objectUrl) URL.revokeObjectURL(objectUrl);
    };
  }, [patientId, id]);

  return { url, isLoading };
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
