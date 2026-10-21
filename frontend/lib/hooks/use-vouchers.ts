import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import {
  listVouchers,
  createVoucher,
  updateVoucher,
  deleteVoucher,
  type VoucherBody,
} from "../api/vouchers";

export const voucherKeys = {
  all: ["vouchers"] as const,
  list: (params?: object) => [...voucherKeys.all, "list", params] as const,
};

function errMsg(e: unknown, fallback: string): string {
  const m = (e as { response?: { data?: { error?: { message?: string } } } })?.response?.data?.error?.message;
  return m || fallback;
}

export function useVouchers(params?: { is_active?: boolean; page?: number; page_size?: number }) {
  return useQuery({
    queryKey: voucherKeys.list(params),
    queryFn: () => listVouchers(params),
  });
}

export function useCreateVoucher() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: VoucherBody & { code: string }) => createVoucher(body),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: voucherKeys.all });
      toast.success("Đã thêm voucher");
    },
    onError: (e) => toast.error(errMsg(e, "Thêm voucher thất bại")),
  });
}

export function useUpdateVoucher() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, body }: { id: string; body: VoucherBody }) => updateVoucher(id, body),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: voucherKeys.all });
      toast.success("Đã cập nhật voucher");
    },
    onError: (e) => toast.error(errMsg(e, "Cập nhật voucher thất bại")),
  });
}

export function useDeleteVoucher() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => deleteVoucher(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: voucherKeys.all });
      toast.success("Đã xóa voucher");
    },
    onError: (e) => toast.error(errMsg(e, "Xóa voucher thất bại")),
  });
}
