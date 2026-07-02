import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import {
  listBhytExports,
  getBhytExport,
  createBhytExport,
  deleteBhytExport,
  generateBhytXml,
  regenerateBhytXml,
  validateBhytXml,
  signBhytXml,
  submitBhyt,
  listBhytExportItems,
  type BhytExportListParams,
  type CreateBhytExportRequest,
  type BhytExportItemListParams,
} from "@/lib/api/bhyt-export";

export const BHYT_EXPORT_KEYS = {
  all: ["bhyt-exports"] as const,
  list: (params?: BhytExportListParams) => ["bhyt-exports", "list", params] as const,
  detail: (id: string) => ["bhyt-exports", id] as const,
  items: (id: string, tableNo: number, params?: BhytExportItemListParams) =>
    ["bhyt-exports", id, "items", tableNo, params] as const,
};

export function useBhytExports(params?: BhytExportListParams) {
  return useQuery({
    queryKey: BHYT_EXPORT_KEYS.list(params),
    queryFn: () => listBhytExports(params),
  });
}

export function useBhytExport(id: string) {
  return useQuery({
    queryKey: BHYT_EXPORT_KEYS.detail(id),
    queryFn: () => getBhytExport(id),
    enabled: Boolean(id),
  });
}

export function useBhytExportItems(id: string, tableNo: number, params?: BhytExportItemListParams) {
  return useQuery({
    queryKey: BHYT_EXPORT_KEYS.items(id, tableNo, params),
    queryFn: () => listBhytExportItems(id, tableNo, params),
    enabled: Boolean(id) && tableNo >= 1 && tableNo <= 5,
  });
}

export function useCreateBhytExport() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: CreateBhytExportRequest) => createBhytExport(body),
    onSuccess: () => qc.invalidateQueries({ queryKey: BHYT_EXPORT_KEYS.all }),
  });
}

export function useDeleteBhytExport() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => deleteBhytExport(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: BHYT_EXPORT_KEYS.all }),
  });
}

export function useGenerateBhytXml() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => generateBhytXml(id),
    onSuccess: (data) => {
      qc.invalidateQueries({ queryKey: BHYT_EXPORT_KEYS.detail(data.id) });
      qc.invalidateQueries({ queryKey: BHYT_EXPORT_KEYS.all });
    },
  });
}

export function useRegenerateBhytXml() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => regenerateBhytXml(id),
    onSuccess: (data) => {
      qc.invalidateQueries({ queryKey: BHYT_EXPORT_KEYS.detail(data.id) });
      qc.invalidateQueries({ queryKey: BHYT_EXPORT_KEYS.all });
    },
  });
}

export function useValidateBhytXml() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => validateBhytXml(id),
    onSuccess: (_data, id) => {
      qc.invalidateQueries({ queryKey: BHYT_EXPORT_KEYS.detail(id) });
    },
  });
}

export function useSignBhytXml() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, cert_thumbprint, pin }: { id: string; cert_thumbprint?: string; pin?: string }) =>
      signBhytXml(id, { cert_thumbprint, pin }),
    onSuccess: (data) => {
      qc.invalidateQueries({ queryKey: BHYT_EXPORT_KEYS.detail(data.id) });
      qc.invalidateQueries({ queryKey: BHYT_EXPORT_KEYS.all });
    },
  });
}

export function useSubmitBhyt() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => submitBhyt(id),
    onSuccess: (data) => {
      qc.invalidateQueries({ queryKey: BHYT_EXPORT_KEYS.detail(data.id) });
      qc.invalidateQueries({ queryKey: BHYT_EXPORT_KEYS.all });
    },
  });
}
