import apiClient from "./client";
import type { ApiResponse, ApiMeta } from "./types";

// ─── Types ────────────────────────────────────────────────────────────────────

export type WarehouseType = "MAIN" | "DISPENSING" | "COLD_CHAIN" | "NARCOTIC";

export interface WarehouseRequest {
  code: string;
  name: string;
  type?: WarehouseType;
  address?: string;
  manager_user_id?: string | null;
}

export interface Warehouse extends WarehouseRequest {
  id: string;
  tenant_id: string;
  created_at: string;
}

export type PurchaseOrderStatus = "DRAFT" | "SENT" | "PARTIAL" | "RECEIVED" | "CANCELLED";

export interface PurchaseOrderRequest {
  supplier_id: string;
  warehouse_id: string;
  order_no?: string;
  ordered_at?: string;
  expected_delivery?: string;
  note?: string;
  items: Array<{
    drug_id: string;
    quantity_ordered: number;
    unit_price: number;
  }>;
}

export interface PurchaseOrderResponse {
  id: string;
  tenant_id: string;
  supplier_id: string;
  supplier_name: string;
  warehouse_id: string;
  order_no: string;
  ordered_at: string;
  expected_delivery?: string;
  status: PurchaseOrderStatus;
  items: Array<{
    drug_id: string;
    drug_name: string;
    quantity_ordered: number;
    quantity_received: number;
    unit_price: number;
  }>;
  total_amount: number;
  created_at: string;
}

export interface GoodsReceivedRequest {
  po_id?: string;
  received_at: string;
  note?: string;
  items: Array<{
    drug_id: string;
    batch_no: string;
    manufacture_date?: string;
    expiry_date: string;
    quantity_received: number;
    unit_cost: number;
  }>;
}

export interface StockResponse {
  id: string;
  tenant_id: string;
  warehouse_id: string;
  drug_id: string;
  drug_name: string;
  batch_no: string;
  manufacture_date?: string | null;
  expiry_date: string;
  quantity_available: number;
  quantity_reserved: number;
  unit_cost: number;
  days_to_expiry: number;
  is_near_expiry: boolean;
  is_low_stock: boolean;
}

export interface StockMovementResponse {
  id: string;
  tenant_id: string;
  warehouse_id: string;
  movement_type: "IMPORT" | "EXPORT" | "TRANSFER" | "ADJUST" | "RETURN";
  drug_id: string;
  drug_name: string;
  batch_no: string;
  quantity: number;
  unit_cost: number;
  reference_type: "PO" | "GRN" | "PRESCRIPTION" | "ADJUSTMENT" | "TRANSFER" | "RETURN";
  reference_id: string;
  movement_at: string;
  performed_by: string;
  reason?: string;
}

export interface StockListParams {
  warehouse_id?: string;
  drug_id?: string;
  batch_no?: string;
  low_stock?: boolean;
  near_expiry?: boolean;
  page?: number;
  page_size?: number;
}

export interface MovementListParams {
  warehouse_id?: string;
  drug_id?: string;
  movement_type?: string;
  from_date?: string;
  to_date?: string;
  page?: number;
  page_size?: number;
}

// ─── API calls ────────────────────────────────────────────────────────────────

export async function listWarehouses(): Promise<Warehouse[]> {
  const { data } = await apiClient.get<ApiResponse<Warehouse[]>>("/warehouses");
  return data.data;
}

export async function createWarehouse(body: WarehouseRequest): Promise<Warehouse> {
  const { data } = await apiClient.post<ApiResponse<Warehouse>>("/warehouses", body);
  return data.data;
}

export async function updateWarehouse(id: string, body: WarehouseRequest): Promise<Warehouse> {
  const { data } = await apiClient.put<ApiResponse<Warehouse>>(`/warehouses/${id}`, body);
  return data.data;
}

export async function deleteWarehouse(id: string): Promise<void> {
  await apiClient.delete(`/warehouses/${id}`);
}

export async function listPurchaseOrders(params?: {
  status?: PurchaseOrderStatus;
  supplier_id?: string;
  page?: number;
  page_size?: number;
}): Promise<{ data: PurchaseOrderResponse[]; meta: ApiMeta }> {
  const { data } = await apiClient.get<{ data: PurchaseOrderResponse[]; meta: ApiMeta }>(
    "/pharmacy/purchase-orders",
    { params }
  );
  return data;
}

export async function createPurchaseOrder(body: PurchaseOrderRequest): Promise<PurchaseOrderResponse> {
  const { data } = await apiClient.post<ApiResponse<PurchaseOrderResponse>>("/pharmacy/purchase-orders", body);
  return data.data;
}

export async function createGrn(
  poId: string,
  body: GoodsReceivedRequest
): Promise<{ grn_id: string; po_status: PurchaseOrderStatus; stocks_updated: StockResponse[] }> {
  const { data } = await apiClient.post<ApiResponse<{ grn_id: string; po_status: PurchaseOrderStatus; stocks_updated: StockResponse[] }>>(
    `/pharmacy/purchase-orders/${poId}/grn`,
    body
  );
  return data.data;
}

export async function listStocks(
  params?: StockListParams
): Promise<{ data: StockResponse[]; meta: ApiMeta }> {
  const { data } = await apiClient.get<{ data: StockResponse[]; meta: ApiMeta }>("/pharmacy/stocks", { params });
  return data;
}

export async function createAdjustment(body: {
  warehouse_id: string;
  reason: "STOCKTAKE" | "DAMAGED" | "EXPIRED" | "LOST" | "OTHER";
  note?: string;
  items: Array<{ drug_id: string; batch_no: string; quantity_diff: number }>;
}): Promise<{ adjustment_id: string; movements: StockMovementResponse[] }> {
  const { data } = await apiClient.post<ApiResponse<{ adjustment_id: string; movements: StockMovementResponse[] }>>(
    "/pharmacy/adjustments",
    body
  );
  return data.data;
}

export async function listMovements(
  params?: MovementListParams
): Promise<{ data: StockMovementResponse[]; meta: ApiMeta }> {
  const { data } = await apiClient.get<{ data: StockMovementResponse[]; meta: ApiMeta }>(
    "/pharmacy/movements",
    { params }
  );
  return data;
}

export async function getLowStockAlerts(warehouse_id?: string): Promise<StockResponse[]> {
  const { data } = await apiClient.get<ApiResponse<StockResponse[]>>("/pharmacy/alerts/low-stock", {
    params: warehouse_id ? { warehouse_id } : undefined,
  });
  return data.data;
}

export async function getNearExpiryAlerts(days: 30 | 60 | 90 = 60, warehouse_id?: string): Promise<StockResponse[]> {
  const { data } = await apiClient.get<ApiResponse<StockResponse[]>>("/pharmacy/alerts/near-expiry", {
    params: { days, ...(warehouse_id ? { warehouse_id } : {}) },
  });
  return data.data;
}
