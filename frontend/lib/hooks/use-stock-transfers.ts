import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import {
  listStockTransfers,
  getStockTransfer,
  createStockTransfer,
  submitStockTransfer,
  approveStockTransfer,
  rejectStockTransfer,
  shipStockTransfer,
  receiveStockTransfer,
  partialReceiveStockTransfer,
  closeStockTransfer,
  cancelStockTransfer,
  type StockTransferListParams,
  type CreateStockTransferRequest,
  type ApproveStockTransferRequest,
  type RejectStockTransferRequest,
  type ReceiveStockTransferRequest,
} from "@/lib/api/stock-transfers";

export const STOCK_TRANSFER_KEYS = {
  all: ["stock-transfers"] as const,
  list: (params?: StockTransferListParams) => ["stock-transfers", "list", params] as const,
  detail: (id: string) => ["stock-transfers", id] as const,
};

export function useStockTransfers(params?: StockTransferListParams) {
  return useQuery({
    queryKey: STOCK_TRANSFER_KEYS.list(params),
    queryFn: () => listStockTransfers(params),
  });
}

export function useStockTransfer(id: string) {
  return useQuery({
    queryKey: STOCK_TRANSFER_KEYS.detail(id),
    queryFn: () => getStockTransfer(id),
    enabled: Boolean(id),
  });
}

export function useCreateStockTransfer() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: CreateStockTransferRequest) => createStockTransfer(body),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: STOCK_TRANSFER_KEYS.all });
      toast.success("Đã tạo phiếu điều chuyển kho");
    },
    onError: () => toast.error("Tạo phiếu điều chuyển thất bại"),
  });
}

function invalidateDetail(qc: ReturnType<typeof useQueryClient>, id: string) {
  qc.invalidateQueries({ queryKey: STOCK_TRANSFER_KEYS.detail(id) });
  qc.invalidateQueries({ queryKey: STOCK_TRANSFER_KEYS.all });
}

export function useSubmitStockTransfer() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => submitStockTransfer(id),
    onSuccess: (data) => {
      invalidateDetail(qc, data.id);
      toast.success("Đã gửi duyệt phiếu điều chuyển");
    },
    onError: () => toast.error("Gửi duyệt thất bại"),
  });
}

export function useApproveStockTransfer() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, body }: { id: string; body?: ApproveStockTransferRequest }) =>
      approveStockTransfer(id, body),
    onSuccess: (data) => {
      invalidateDetail(qc, data.id);
      toast.success("Đã duyệt phiếu điều chuyển");
    },
    onError: () => toast.error("Duyệt phiếu thất bại"),
  });
}

export function useRejectStockTransfer() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, body }: { id: string; body: RejectStockTransferRequest }) =>
      rejectStockTransfer(id, body),
    onSuccess: (data) => {
      invalidateDetail(qc, data.id);
      toast.success("Đã từ chối phiếu điều chuyển");
    },
    onError: () => toast.error("Từ chối phiếu thất bại"),
  });
}

export function useShipStockTransfer() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => shipStockTransfer(id),
    onSuccess: (data) => {
      invalidateDetail(qc, data.id);
      toast.success("Đã xuất hàng, phiếu đang trên đường vận chuyển");
    },
    onError: () => toast.error("Xuất hàng thất bại"),
  });
}

export function useReceiveStockTransfer() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, body }: { id: string; body?: ReceiveStockTransferRequest }) =>
      receiveStockTransfer(id, body),
    onSuccess: (data) => {
      invalidateDetail(qc, data.id);
      toast.success("Đã xác nhận nhận hàng");
    },
    onError: () => toast.error("Xác nhận nhận hàng thất bại"),
  });
}

export function usePartialReceiveStockTransfer() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, body }: { id: string; body: ReceiveStockTransferRequest }) =>
      partialReceiveStockTransfer(id, body),
    onSuccess: (data) => {
      invalidateDetail(qc, data.id);
      toast.success("Đã ghi nhận hàng về (thiếu/khác số lượng)");
    },
    onError: () => toast.error("Ghi nhận hàng về thất bại"),
  });
}

export function useCloseStockTransfer() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => closeStockTransfer(id),
    onSuccess: (data) => {
      invalidateDetail(qc, data.id);
      toast.success("Đã đóng phiếu điều chuyển");
    },
    onError: () => toast.error("Đóng phiếu thất bại"),
  });
}

export function useCancelStockTransfer() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => cancelStockTransfer(id),
    onSuccess: (data) => {
      invalidateDetail(qc, data.id);
      toast.success("Đã huỷ phiếu điều chuyển");
    },
    onError: () => toast.error("Huỷ phiếu thất bại"),
  });
}
