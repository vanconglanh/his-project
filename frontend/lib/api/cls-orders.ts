import apiClient from "./client";
import type {
  ApiResponse,
  LabOrderRequest,
  LabOrderResponse,
  RadOrderRequest,
  RadOrderResponse,
  ClsCatalogItem,
  LabOrderStatus,
  RadOrderStatus,
} from "./types";

export async function createLabOrders(encounterId: string, tests: LabOrderRequest[]) {
  const res = await apiClient.post<ApiResponse<LabOrderResponse[]>>(
    `/encounters/${encounterId}/lab-orders`,
    { tests }
  );
  return res.data.data;
}

export async function listLabOrders(encounterId: string) {
  const res = await apiClient.get<ApiResponse<LabOrderResponse[]>>(
    `/encounters/${encounterId}/lab-orders`
  );
  return res.data.data;
}

export async function updateLabOrderStatus(id: string, status: LabOrderStatus, note?: string) {
  await apiClient.put(`/lab-orders/${id}`, { status, note });
}

export async function deleteLabOrder(id: string) {
  await apiClient.delete(`/lab-orders/${id}`);
}

export async function createRadOrders(encounterId: string, orders: RadOrderRequest[]) {
  const res = await apiClient.post<ApiResponse<RadOrderResponse[]>>(
    `/encounters/${encounterId}/rad-orders`,
    { orders }
  );
  return res.data.data;
}

export async function listRadOrders(encounterId: string) {
  const res = await apiClient.get<ApiResponse<RadOrderResponse[]>>(
    `/encounters/${encounterId}/rad-orders`
  );
  return res.data.data;
}

export async function updateRadOrderStatus(id: string, status: RadOrderStatus, note?: string) {
  await apiClient.put(`/rad-orders/${id}`, { status, note });
}

export async function deleteRadOrder(id: string) {
  await apiClient.delete(`/rad-orders/${id}`);
}

export async function searchClsCatalog(params: { q?: string; kind?: "LAB" | "RAD"; limit?: number }) {
  const res = await apiClient.get<ApiResponse<ClsCatalogItem[]>>("/cls-catalog/tests", { params });
  return res.data.data;
}
