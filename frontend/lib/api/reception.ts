import apiClient from "./client";
import type {
  ApiResponse,
  CheckInRequest,
  ReceptionTicketResponse,
  RoomResponse,
  ReceptionStats,
} from "./types";

export interface QueueParams {
  room_id?: string;
  status?: string;
  date?: string;
}

export async function checkIn(body: CheckInRequest): Promise<ReceptionTicketResponse> {
  const { data } = await apiClient.post<ApiResponse<ReceptionTicketResponse>>(
    "/reception/check-in",
    body
  );
  return data.data;
}

export async function getQueue(params?: QueueParams): Promise<ReceptionTicketResponse[]> {
  const { data } = await apiClient.get<ApiResponse<ReceptionTicketResponse[]>>(
    "/reception/queue",
    { params }
  );
  return data.data;
}

export async function callTicket(ticketId: string): Promise<ReceptionTicketResponse> {
  const { data } = await apiClient.put<ApiResponse<ReceptionTicketResponse>>(
    `/reception/queue/${ticketId}/call`
  );
  return data.data;
}

export async function skipTicket(ticketId: string): Promise<ReceptionTicketResponse> {
  const { data } = await apiClient.put<ApiResponse<ReceptionTicketResponse>>(
    `/reception/queue/${ticketId}/skip`
  );
  return data.data;
}

export async function cancelTicket(ticketId: string, reason?: string): Promise<ReceptionTicketResponse> {
  const { data } = await apiClient.put<ApiResponse<ReceptionTicketResponse>>(
    `/reception/queue/${ticketId}/cancel`,
    { reason }
  );
  return data.data;
}

export function getTicketPdfUrl(ticketId: string): string {
  const base = process.env.NEXT_PUBLIC_API_BASE_URL || "http://localhost:5000";
  return `${base}/api/v1/reception/queue/${ticketId}/ticket-pdf`;
}

export async function getRooms(): Promise<RoomResponse[]> {
  const { data } = await apiClient.get<ApiResponse<RoomResponse[]>>("/reception/rooms");
  return data.data;
}

export async function getReceptionStats(): Promise<ReceptionStats> {
  const { data } = await apiClient.get<ApiResponse<ReceptionStats>>("/reception/stats");
  return data.data;
}
