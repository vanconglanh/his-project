import apiClient, { API_BASE_URL } from "./client";
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

export interface AdmitTicketResult {
  encounter_id: string;
  created: boolean;
}

/** Đưa bệnh nhân từ vé hàng đợi vào khám: tạo/lấy lượt khám, trả về encounter_id. */
export async function admitTicket(ticketId: string): Promise<AdmitTicketResult> {
  const { data } = await apiClient.post<ApiResponse<AdmitTicketResult>>(
    `/reception/queue/${ticketId}/admit`
  );
  return data.data;
}

export function getTicketPdfUrl(ticketId: string): string {
  return `${API_BASE_URL}/api/v1/reception/queue/${ticketId}/ticket-pdf`;
}

export async function getRooms(): Promise<RoomResponse[]> {
  const { data } = await apiClient.get<ApiResponse<RoomResponse[]>>("/reception/rooms");
  return data.data;
}

export async function getReceptionStats(): Promise<ReceptionStats> {
  const { data } = await apiClient.get<ApiResponse<ReceptionStats>>("/reception/stats");
  return data.data;
}

// ─── [G05] Điều phối khám + chờ kết quả CLS ──────────────────────────────────

export interface ReassignTicketRequest {
  doctor_id?: string | null;
  room_id?: string | null;
  reason: string;
  acknowledge_schedule_warning?: boolean;
}

export interface TicketClsStatusResponse {
  id: string;
  status: string;
  status_label: string;
  room_id?: string | null;
  released_room_id?: string | null;
  waiting_cls_at?: string | null;
}

/** [G05] Đổi phòng / đổi bác sĩ cho vé tiếp đón, giữ nguyên mã lượt khám */
export async function reassignTicket(
  ticketId: string,
  body: ReassignTicketRequest
): Promise<ReceptionTicketResponse> {
  const { data } = await apiClient.put<ApiResponse<ReceptionTicketResponse>>(
    `/reception/tickets/${ticketId}/reassign`,
    body
  );
  return data.data;
}

/** Chuyển vé sang "Chờ kết quả CLS" — nhả phòng cho bệnh nhân kế tiếp */
export async function waitClsTicket(
  ticketId: string,
  body?: { cls_round_id?: string | null; note?: string | null }
): Promise<TicketClsStatusResponse> {
  const { data } = await apiClient.post<ApiResponse<TicketClsStatusResponse>>(
    `/reception/tickets/${ticketId}/wait-cls`,
    body ?? {}
  );
  return data.data;
}

/** Quay lại phòng khám sau khi có kết quả CLS */
export async function resumeTicket(
  ticketId: string,
  body?: { room_id?: string | null }
): Promise<TicketClsStatusResponse> {
  const { data } = await apiClient.post<ApiResponse<TicketClsStatusResponse>>(
    `/reception/tickets/${ticketId}/resume`,
    body ?? {}
  );
  return data.data;
}
