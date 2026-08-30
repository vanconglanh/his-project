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
  getBranchBhytCompliance,
  cloneBranch,
  getBranchReadiness,
  activateBranch,
  type BranchRequest,
  type ListBranchesParams,
  type CloneBranchRequest,
} from "../api/branches";

export const branchKeys = {
  all: ["branches"] as const,
  list: (params?: ListBranchesParams) => [...branchKeys.all, "list", params] as const,
  detail: (id: number | string) => [...branchKeys.all, "detail", id] as const,
  users: (id: number | string) => [...branchKeys.all, "users", id] as const,
  compliance: () => [...branchKeys.all, "bhyt-compliance"] as const,
  readiness: (id: number | string) => [...branchKeys.all, "readiness", id] as const,
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

export function useBranchBhytCompliance() {
  return useQuery({
    queryKey: branchKeys.compliance(),
    queryFn: () => getBranchBhytCompliance(),
  });
}

export function useCloneBranch() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({
      sourceBranchId,
      body,
    }: {
      sourceBranchId: number | string;
      body: CloneBranchRequest;
    }) => cloneBranch(sourceBranchId, body),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: branchKeys.all });
      toast.success("Đã nhân bản chi nhánh");
    },
    onError: () => toast.error("Nhân bản chi nhánh thất bại"),
  });
}

export function useBranchReadiness(id: number | string | undefined) {
  return useQuery({
    queryKey: branchKeys.readiness(id ?? ""),
    queryFn: () => getBranchReadiness(id as number | string),
    enabled: !!id,
  });
}

export function useActivateBranch() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: number | string) => activateBranch(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: branchKeys.all });
      toast.success("Đã kích hoạt chi nhánh");
    },
    onError: (err: unknown) => {
      const e = err as {
        response?: { data?: { error?: { code?: string; message?: string } } };
      };
      if (e.response?.data?.error?.code === "BRANCH_NOT_READY") {
        toast.error("Chi nhánh chưa đạt checklist go-live", {
          description: e.response.data.error.message,
        });
      } else {
        toast.error("Kích hoạt chi nhánh thất bại");
      }
    },
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
