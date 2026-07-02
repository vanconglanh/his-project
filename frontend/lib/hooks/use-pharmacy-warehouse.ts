import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import {
  listWarehouses,
  createWarehouse,
  updateWarehouse,
  deleteWarehouse,
  listPurchaseOrders,
  createPurchaseOrder,
  createGrn,
  listStocks,
  createAdjustment,
  listMovements,
  getLowStockAlerts,
  getNearExpiryAlerts,
  type WarehouseRequest,
  type PurchaseOrderRequest,
  type GoodsReceivedRequest,
  type StockListParams,
  type MovementListParams,
  type PurchaseOrderStatus,
} from "../api/pharmacy-warehouse";

export const warehouseKeys = {
  all: ["warehouses"] as const,
  list: () => [...warehouseKeys.all, "list"] as const,
};

export const stockKeys = {
  all: ["stocks"] as const,
  list: (params?: StockListParams) => [...stockKeys.all, "list", params] as const,
  lowStock: (warehouse_id?: string) => [...stockKeys.all, "low-stock", warehouse_id] as const,
  nearExpiry: (days?: number, warehouse_id?: string) => [...stockKeys.all, "near-expiry", days, warehouse_id] as const,
};

export const poKeys = {
  all: ["purchase-orders"] as const,
  list: (params?: object) => [...poKeys.all, "list", params] as const,
};

export const movementKeys = {
  all: ["movements"] as const,
  list: (params?: MovementListParams) => [...movementKeys.all, "list", params] as const,
};

export function useWarehouses() {
  return useQuery({
    queryKey: warehouseKeys.list(),
    queryFn: listWarehouses,
    staleTime: 5 * 60_000,
  });
}

export function useCreateWarehouse() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: WarehouseRequest) => createWarehouse(body),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: warehouseKeys.all });
      toast.success("Đã tạo kho");
    },
    onError: () => toast.error("Tạo kho thất bại"),
  });
}

export function useUpdateWarehouse(id: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: WarehouseRequest) => updateWarehouse(id, body),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: warehouseKeys.all });
      toast.success("Đã cập nhật kho");
    },
    onError: () => toast.error("Cập nhật thất bại"),
  });
}

export function useDeleteWarehouse() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => deleteWarehouse(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: warehouseKeys.all });
      toast.success("Đã xóa kho");
    },
    onError: () => toast.error("Xóa thất bại"),
  });
}

export function usePurchaseOrders(params?: { status?: PurchaseOrderStatus; supplier_id?: string; page?: number; page_size?: number }) {
  return useQuery({
    queryKey: poKeys.list(params),
    queryFn: () => listPurchaseOrders(params),
  });
}

export function useCreatePurchaseOrder() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: PurchaseOrderRequest) => createPurchaseOrder(body),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: poKeys.all });
      toast.success("Đã tạo đơn đặt hàng");
    },
    onError: () => toast.error("Tạo đơn thất bại"),
  });
}

export function useCreateGrn(poId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: GoodsReceivedRequest) => createGrn(poId, body),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: poKeys.all });
      qc.invalidateQueries({ queryKey: stockKeys.all });
      toast.success("Nhập kho thành công, tồn kho đã cập nhật");
    },
    onError: () => toast.error("Nhập kho thất bại"),
  });
}

export function useStocks(params?: StockListParams) {
  return useQuery({
    queryKey: stockKeys.list(params),
    queryFn: () => listStocks(params),
  });
}

export function useLowStockAlerts(warehouse_id?: string) {
  return useQuery({
    queryKey: stockKeys.lowStock(warehouse_id),
    queryFn: () => getLowStockAlerts(warehouse_id),
  });
}

export function useNearExpiryAlerts(days: 30 | 60 | 90 = 60, warehouse_id?: string) {
  return useQuery({
    queryKey: stockKeys.nearExpiry(days, warehouse_id),
    queryFn: () => getNearExpiryAlerts(days, warehouse_id),
  });
}

export function useCreateAdjustment() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: Parameters<typeof createAdjustment>[0]) => createAdjustment(body),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: stockKeys.all });
      toast.success("Điều chỉnh tồn kho thành công");
    },
    onError: () => toast.error("Điều chỉnh thất bại"),
  });
}

export function useMovements(params?: MovementListParams) {
  return useQuery({
    queryKey: movementKeys.list(params),
    queryFn: () => listMovements(params),
  });
}
