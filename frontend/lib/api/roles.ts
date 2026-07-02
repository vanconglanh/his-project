import apiClient from "./client";
import type {
  ApiResponse,
  ApiMeta,
  RoleResponse,
  CreateRoleRequest,
  UpdateRoleRequest,
  PermissionResponse,
  AuditLogResponse,
} from "./types";

export interface ListAuditLogsParams {
  page?: number;
  page_size?: number;
  sort?: string;
  user_id?: string;
  action?: string;
  resource_type?: string;
  from?: string;
  to?: string;
}

export interface PagedAuditLogs {
  data: AuditLogResponse[];
  meta: ApiMeta;
}

export async function listRoles(): Promise<RoleResponse[]> {
  const { data } = await apiClient.get<ApiResponse<RoleResponse[]>>("/roles");
  return data.data;
}

export async function getRole(code: string): Promise<RoleResponse> {
  const { data } = await apiClient.get<ApiResponse<RoleResponse>>(`/roles/${code}`);
  return data.data;
}

export async function createRole(payload: CreateRoleRequest): Promise<RoleResponse> {
  const { data } = await apiClient.post<ApiResponse<RoleResponse>>("/roles", payload);
  return data.data;
}

export async function updateRole(code: string, payload: UpdateRoleRequest): Promise<RoleResponse> {
  const { data } = await apiClient.put<ApiResponse<RoleResponse>>(`/roles/${code}`, payload);
  return data.data;
}

export async function deleteRole(code: string): Promise<void> {
  await apiClient.delete(`/roles/${code}`);
}

export async function listPermissions(resource?: string): Promise<PermissionResponse[]> {
  const { data } = await apiClient.get<ApiResponse<PermissionResponse[]>>("/permissions", {
    params: resource ? { resource } : undefined,
  });
  return data.data;
}

export async function listAuditLogs(params?: ListAuditLogsParams): Promise<PagedAuditLogs> {
  const { data } = await apiClient.get<PagedAuditLogs>("/audit-logs", { params });
  return data;
}
