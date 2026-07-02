import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import {
  listSuppliers,
  getSupplier,
  createSupplier,
  updateSupplier,
  deleteSupplier,
  type SupplierRequest,
} from "../api/suppliers";

export const supplierKeys = {
  all: ["suppliers"] as const,
  list: (params?: object) => [...supplierKeys.all, "list", params] as const,
  detail: (id: string) => [...supplierKeys.all, "detail", id] as const,
};

export function useSuppliers(params?: { q?: string; status?: string; page?: number; page_size?: number }) {
  return useQuery({
    queryKey: supplierKeys.list(params),
    queryFn: () => listSuppliers(params),
  });
}

export function useSupplier(id: string) {
  return useQuery({
    queryKey: supplierKeys.detail(id),
    queryFn: () => getSupplier(id),
    enabled: !!id,
  });
}

export function useCreateSupplier() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: SupplierRequest) => createSupplier(body),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: supplierKeys.all });
      toast.success("Đã tạo nhà cung cấp");
    },
    onError: () => toast.error("Tạo nhà cung cấp thất bại"),
  });
}

export function useUpdateSupplier(id: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: SupplierRequest) => updateSupplier(id, body),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: supplierKeys.detail(id) });
      qc.invalidateQueries({ queryKey: supplierKeys.list() });
      toast.success("Đã cập nhật nhà cung cấp");
    },
    onError: () => toast.error("Cập nhật thất bại"),
  });
}

export function useDeleteSupplier() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => deleteSupplier(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: supplierKeys.all });
      toast.success("Đã xóa nhà cung cấp");
    },
    onError: () => toast.error("Xóa thất bại"),
  });
}
