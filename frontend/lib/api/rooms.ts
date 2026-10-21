import apiClient from "./client";
import type { ApiResponse, ApiMeta } from "./types";

/** Khớp RoomAdminResponse (backend, JSON snake_case). */
export interface RoomAdminResponse {
  id: string;
  tenant_id: number | null;
  room_code: string;
  name: string;
  max_per_day: number;
  is_active: boolean;
  created_at: string;
  updated_at: string;
}

export interface CreateRoomRequest {
  room_code: string;
  name: string;
  max_per_day?: number;
  is_active?: boolean;
}

export interface UpdateRoomRequest {
  room_code?: string;
  name?: string;
  max_per_day?: number;
  is_active?: boolean;
}

export async function listRoomsAdmin(params?: {
  is_active?: boolean;
  page?: number;
  page_size?: number;
}): Promise<{ data: RoomAdminResponse[]; meta: ApiMeta }> {
  const { data } = await apiClient.get<{ data: RoomAdminResponse[]; meta: ApiMeta }>("/rooms", { params });
  return data;
}

export async function getRoomAdmin(id: string): Promise<RoomAdminResponse> {
  const { data } = await apiClient.get<ApiResponse<RoomAdminResponse>>(`/rooms/${id}`);
  return data.data;
}

export async function createRoomAdmin(body: CreateRoomRequest): Promise<RoomAdminResponse> {
  const { data } = await apiClient.post<ApiResponse<RoomAdminResponse>>("/rooms", body);
  return data.data;
}

export async function updateRoomAdmin(id: string, body: UpdateRoomRequest): Promise<RoomAdminResponse> {
  const { data } = await apiClient.put<ApiResponse<RoomAdminResponse>>(`/rooms/${id}`, body);
  return data.data;
}

export async function deleteRoomAdmin(id: string): Promise<void> {
  await apiClient.delete(`/rooms/${id}`);
}
