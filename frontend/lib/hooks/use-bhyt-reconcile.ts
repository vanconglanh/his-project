import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import {
  importReconcileFile,
  listReconcileItems,
  disputeReconcileItem,
  acceptReconcileItem,
  getReconcileSummary,
  type ReconcileListParams,
} from "@/lib/api/bhyt-reconcile";

export const BHYT_RECONCILE_KEYS = {
  all: ["bhyt-reconcile"] as const,
  items: (exportId: string, params?: ReconcileListParams) =>
    ["bhyt-reconcile", exportId, "items", params] as const,
  summary: (exportId: string) => ["bhyt-reconcile", exportId, "summary"] as const,
};

export function useReconcileItems(exportId: string, params?: ReconcileListParams) {
  return useQuery({
    queryKey: BHYT_RECONCILE_KEYS.items(exportId, params),
    queryFn: () => listReconcileItems(exportId, params),
    enabled: Boolean(exportId),
  });
}

export function useReconcileSummary(exportId: string) {
  return useQuery({
    queryKey: BHYT_RECONCILE_KEYS.summary(exportId),
    queryFn: () => getReconcileSummary(exportId),
    enabled: Boolean(exportId),
  });
}

export function useImportReconcileFile() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ exportId, file }: { exportId: string; file: File }) =>
      importReconcileFile(exportId, file),
    onSuccess: (_data, vars) => {
      qc.invalidateQueries({ queryKey: BHYT_RECONCILE_KEYS.items(vars.exportId) });
      qc.invalidateQueries({ queryKey: BHYT_RECONCILE_KEYS.summary(vars.exportId) });
    },
  });
}

export function useDisputeReconcileItem(exportId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ itemId, reason, evidence_file_path }: { itemId: string; reason: string; evidence_file_path?: string }) =>
      disputeReconcileItem(itemId, { reason, evidence_file_path }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: BHYT_RECONCILE_KEYS.items(exportId) });
      qc.invalidateQueries({ queryKey: BHYT_RECONCILE_KEYS.summary(exportId) });
    },
  });
}

export function useAcceptReconcileItem(exportId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ itemId, note }: { itemId: string; note?: string }) =>
      acceptReconcileItem(itemId, { note }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: BHYT_RECONCILE_KEYS.items(exportId) });
      qc.invalidateQueries({ queryKey: BHYT_RECONCILE_KEYS.summary(exportId) });
    },
  });
}
