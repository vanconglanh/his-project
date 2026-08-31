"use client";

import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import {
  uploadLegacyImportBatch,
  listLegacyImportBatches,
  getLegacyImportBatch,
  listLegacyImportItems,
  matchLegacyImportItem,
  confirmLegacyImportItem,
  rejectLegacyImportItem,
  type LegacyImportItemListParams,
  type LegacyImportBatchStatus,
  type LegacyImportDocType,
} from "@/lib/api/legacy-import";
import { getErrorMessage } from "@/lib/utils/errors";

export const legacyImportKeys = {
  all: ["legacy-imports"] as const,
  list: (params?: { page?: number; page_size?: number }) =>
    [...legacyImportKeys.all, "list", params] as const,
  detail: (id: string) => [...legacyImportKeys.all, "detail", id] as const,
  items: (batchId: string, params?: LegacyImportItemListParams) =>
    [...legacyImportKeys.all, "items", batchId, params] as const,
};

const IN_PROGRESS_STATUSES: LegacyImportBatchStatus[] = ["pending", "processing"];

// ─── Queries ──────────────────────────────────────────────────────────────────

export function useLegacyImportBatches(params?: { page?: number; page_size?: number }) {
  return useQuery({
    queryKey: legacyImportKeys.list(params),
    queryFn: () => listLegacyImportBatches(params),
    refetchInterval: (query) => {
      const hasInProgress = (query.state.data?.data ?? []).some((b) =>
        IN_PROGRESS_STATUSES.includes(b.status)
      );
      return hasInProgress ? 3000 : false;
    },
  });
}

export function useLegacyImportBatch(id: string) {
  return useQuery({
    queryKey: legacyImportKeys.detail(id),
    queryFn: () => getLegacyImportBatch(id),
    enabled: !!id,
    refetchInterval: (query) =>
      query.state.data && IN_PROGRESS_STATUSES.includes(query.state.data.status) ? 3000 : false,
  });
}

export function useLegacyImportItems(batchId: string, params?: LegacyImportItemListParams) {
  return useQuery({
    queryKey: legacyImportKeys.items(batchId, params),
    queryFn: () => listLegacyImportItems(batchId, params),
    enabled: !!batchId,
  });
}

// ─── Mutations ────────────────────────────────────────────────────────────────

export function useUploadLegacyImportBatch() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (file: File) => uploadLegacyImportBatch(file),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: legacyImportKeys.all });
      toast.success("Đã tải lên file ZIP, hệ thống đang OCR nền");
    },
    onError: (e) => toast.error(getErrorMessage(e, "Tải lên file ZIP thất bại")),
  });
}

export function useMatchLegacyImportItem(batchId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ itemId, patientId }: { itemId: string; patientId: string }) =>
      matchLegacyImportItem(itemId, patientId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: legacyImportKeys.items(batchId) });
      toast.success("Đã gán bệnh nhân");
    },
    onError: (e) => toast.error(getErrorMessage(e, "Gán bệnh nhân thất bại")),
  });
}

export function useConfirmLegacyImportItem(batchId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({
      itemId,
      ocr_text,
      patient_id,
      doc_type,
    }: {
      itemId: string;
      ocr_text?: string;
      patient_id?: string;
      doc_type?: LegacyImportDocType;
    }) => confirmLegacyImportItem(itemId, { ocr_text, patient_id, doc_type }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: legacyImportKeys.items(batchId) });
      qc.invalidateQueries({ queryKey: legacyImportKeys.detail(batchId) });
      toast.success("Đã xác nhận lưu tài liệu vào hồ sơ bệnh nhân");
    },
    onError: (e) => toast.error(getErrorMessage(e, "Xác nhận lưu tài liệu thất bại")),
  });
}

export function useRejectLegacyImportItem(batchId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (itemId: string) => rejectLegacyImportItem(itemId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: legacyImportKeys.items(batchId) });
      toast.success("Đã từ chối ảnh này");
    },
    onError: (e) => toast.error(getErrorMessage(e, "Từ chối ảnh thất bại")),
  });
}
