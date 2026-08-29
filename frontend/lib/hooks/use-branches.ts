import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import {
  listBranches,
  getBranch,
  createBranch,
  updateBranch,
  deleteBranch,
  setDefaultBranch,
  setBranchStatus,
  getBranchUsers,
  addBranchUsers,
  type BranchRequest,
  type ListBranchesParams,
} from "../api/branches";

export const branchKeys = {
  all: ["branches"] as const,
  list: (params?: ListBranchesParams) => [...branchKeys.all, "list", params] as const,
  detail: (id: number | string) => [...branchKeys.all, "detail", id] as const,
  users: (id: number | string) => [...branchKeys.all, "users", id] as const,
};

export function useBranches(params?: ListBranchesParams) {
  return useQuery({
    queryKey: branchKeys.list(params),
    queryFn: () => listBranches(params),
  });
}

export function useBranch(id: number | string | undefined) {
  return useQuery({
    queryKey: branchKeys.detail(id ?? ""),
    queryFn: () => getBranch(id as number | string),
    enabled: !!id,
  });
}

export function useBranchUsers(id: number | string | undefined) {
  return useQuery({
    queryKey: branchKeys.users(id ?? ""),
    queryFn: () => getBranchUsers(id as number | string),
    enabled: !!id,
  });
}

export function useCreateBranch() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: BranchRequest) => createBranch(body),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: branchKeys.all });
      toast.success("Đã tạo chi nhánh");
    },
    onError: () => toast.error("Tạo chi nhánh thất bại"),
  });
}

export function useUpdateBranch(id: number | string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: BranchRequest) => updateBranch(id, body),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: branchKeys.detail(id) });
      qc.invalidateQueries({ queryKey: branchKeys.list() });
      toast.success("Đã cập nhật chi nhánh");
    },
    onError: () => toast.error("Cập nhật chi nhánh thất bại"),
  });
}

export function useDeleteBranch() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: number | string) => deleteBranch(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: branchKeys.all });
      toast.success("Đã xóa chi nhánh");
    },
    onError: () => toast.error("Xóa chi nhánh thất bại"),
  });
}

export function useSetDefaultBranch() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: number | string) => setDefaultBranch(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: branchKeys.all });
      toast.success("Đã đặt làm chi nhánh mặc định");
    },
    onError: () => toast.error("Đặt chi nhánh mặc định thất bại"),
  });
}

export function useSetBranchStatus() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, is_active }: { id: number | string; is_active: boolean }) =>
      setBranchStatus(id, is_active),
    onSuccess: (_data, variables) => {
      qc.invalidateQueries({ queryKey: branchKeys.all });
      toast.success(variables.is_active ? "Đã bật chi nhánh" : "Đã tắt chi nhánh");
    },
    onError: () => toast.error("Cập nhật trạng thái thất bại"),
  });
}

export function useAddBranchUsers(id: number | string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (user_ids: string[]) => addBranchUsers(id, user_ids),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: branchKeys.users(id) });
      toast.success("Đã gán người dùng vào chi nhánh");
    },
    onError: () => toast.error("Gán người dùng thất bại"),
  });
}
