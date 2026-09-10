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

export async function createLabOrders(
  encounterId: string,
  tests: LabOrderRequest[],
  // BM-14: cho phep them dich vu vao 1 dot da ton tai (dieu chinh), khong chi tao dot moi.
  roundId?: string
) {
  const res = await apiClient.post<ApiResponse<LabOrderResponse[]>>(
    `/encounters/${encounterId}/lab-orders`,
    { tests, round_id: roundId }
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

export async function createRadOrders(
  encounterId: string,
  orders: RadOrderRequest[],
  roundId?: string
) {
  const res = await apiClient.post<ApiResponse<RadOrderResponse[]>>(
    `/encounters/${encounterId}/rad-orders`,
    { orders, round_id: roundId }
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

// ─── PDF chỉ định (letterhead branded, QuestPDF server-side) ─────────────────

// BM-17: roundId optional - truyen vao de phieu CHI in dung 1 dot (khong gop tat ca cac
// dot cua luot kham + tranh trung dich vu giua cac dot). Khong truyen = giu hanh vi cu.
export function getLabOrdersPdfUrl(encounterId: string, roundId?: string): string {
  const base = `${apiClient.defaults.baseURL}/encounters/${encounterId}/lab-orders/pdf`;
  return roundId ? `${base}?roundId=${roundId}` : base;
}

export function getRadOrdersPdfUrl(encounterId: string, roundId?: string): string {
  const base = `${apiClient.defaults.baseURL}/encounters/${encounterId}/rad-orders/pdf`;
  return roundId ? `${base}?roundId=${roundId}` : base;
}

export async function printLabOrdersPdf(encounterId: string, roundId?: string): Promise<void> {
  const { printPdfBlob } = await import("@/lib/utils/printPdfBlob");
  await printPdfBlob(getLabOrdersPdfUrl(encounterId, roundId));
}

export async function printRadOrdersPdf(encounterId: string, roundId?: string): Promise<void> {
  const { printPdfBlob } = await import("@/lib/utils/printPdfBlob");
  await printPdfBlob(getRadOrdersPdfUrl(encounterId, roundId));
}
