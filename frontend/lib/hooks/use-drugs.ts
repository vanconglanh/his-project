import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import {
  listDrugs,
  searchDrugs,
  getDrug,
  createDrug,
  updateDrug,
  deleteDrug,
  importDrugsExcel,
  getEquivalentDrugs,
  getDrugInteractions,
  listDrugCategories,
  syncCucQld,
  type DrugListParams,
  type DrugMasterRequest,
} from "../api/drugs";

export const drugKeys = {
  all: ["drugs"] as const,
  list: (params?: DrugListParams) => [...drugKeys.all, "list", params] as const,
  detail: (id: string) => [...drugKeys.all, "detail", id] as const,
  search: (q: string) => [...drugKeys.all, "search", q] as const,
  equivalents: (id: string) => [...drugKeys.all, "equivalents", id] as const,
  interactions: (id: string) => [...drugKeys.all, "interactions", id] as const,
  categories: () => [...drugKeys.all, "categories"] as const,
};

export function useDrugs(params?: DrugListParams) {
  return useQuery({
    queryKey: drugKeys.list(params),
    queryFn: () => listDrugs(params),
  });
}

export function useDrugSearch(q: string) {
  return useQuery({
    queryKey: drugKeys.search(q),
    queryFn: () => searchDrugs(q),
    enabled: q.length >= 1,
    staleTime: 10_000,
  });
}

export function useDrug(id: string) {
  return useQuery({
    queryKey: drugKeys.detail(id),
    queryFn: () => getDrug(id),
    enabled: !!id,
  });
}

export function useEquivalentDrugs(id: string) {
  return useQuery({
    queryKey: drugKeys.equivalents(id),
    queryFn: () => getEquivalentDrugs(id),
    enabled: !!id,
  });
}

export function useDrugInteractions(id: string) {
  return useQuery({
    queryKey: drugKeys.interactions(id),
    queryFn: () => getDrugInteractions(id),
    enabled: !!id,
  });
}

export function useDrugCategories() {
  return useQuery({
    queryKey: drugKeys.categories(),
    queryFn: listDrugCategories,
    staleTime: 5 * 60_000,
  });
}

export function useCreateDrug() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: DrugMasterRequest) => createDrug(body),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: drugKeys.all });
      toast.success("Đã tạo thuốc");
    },
    onError: () => toast.error("Tạo thuốc thất bại"),
  });
}

export function useUpdateDrug(id: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: DrugMasterRequest) => updateDrug(id, body),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: drugKeys.detail(id) });
      qc.invalidateQueries({ queryKey: drugKeys.list() });
      toast.success("Đã cập nhật thuốc");
    },
    onError: () => toast.error("Cập nhật thất bại"),
  });
}

export function useDeleteDrug() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => deleteDrug(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: drugKeys.all });
      toast.success("Đã xóa thuốc");
    },
    onError: () => toast.error("Xóa thất bại"),
  });
}

export function useImportDrugs() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ file, mode }: { file: File; mode: "INSERT" | "UPSERT" }) => importDrugsExcel(file, mode),
    onSuccess: (result) => {
      qc.invalidateQueries({ queryKey: drugKeys.all });
      toast.success(`Import hoàn tất: ${result.inserted} thêm, ${result.updated} cập nhật, ${result.failed} lỗi`);
    },
    onError: () => toast.error("Import thất bại"),
  });
}

export function useSyncCucQld() {
  return useMutation({
    mutationFn: ({ mode, since }: { mode: "FULL" | "INCREMENTAL"; since?: string }) => syncCucQld(mode, since),
    onSuccess: () => toast.success("Đã bắt đầu đồng bộ CSDL Dược Quốc Gia"),
    onError: () => toast.error("Đồng bộ thất bại"),
  });
}
