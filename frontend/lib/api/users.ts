import apiClient from "./client";
import type {
  ApiResponse,
  ApiMeta,
  UserResponse,
  InviteUserRequest,
  AcceptInviteRequest,
  UpdateUserRequest,
  UpdateMeRequest,
  ChangePasswordRequest,
  Setup2FAResponse,
  Enable2FAResponse,
} from "./types";

export interface ListUsersParams {
  page?: number;
  page_size?: number;
  sort?: string;
  q?: string;
  role?: string;
  status?: string;
}

export interface PagedUsers {
  data: UserResponse[];
  meta: ApiMeta;
}

export interface InviteUserResponse {
  user_id: string;
  email: string;
  invite_expires_at: string;
}

export interface AcceptInviteResponse {
  user: UserResponse;
  access_token: string;
  refresh_token: string;
}

export async function listUsers(params?: ListUsersParams): Promise<PagedUsers> {
  const { data } = await apiClient.get<PagedUsers>("/users", { params });
  return data;
}

export async function inviteUser(payload: InviteUserRequest): Promise<InviteUserResponse> {
  const { data } = await apiClient.post<ApiResponse<InviteUserResponse>>("/users/invite", payload);
  return data.data;
}

export async function acceptInvite(payload: AcceptInviteRequest): Promise<AcceptInviteResponse> {
  const { data } = await apiClient.post<ApiResponse<AcceptInviteResponse>>("/users/accept-invite", payload);
  return data.data;
}

export async function getUser(id: string): Promise<UserResponse> {
  const { data } = await apiClient.get<ApiResponse<UserResponse>>(`/users/${id}`);
  return data.data;
}

export async function updateUser(id: string, payload: UpdateUserRequest): Promise<UserResponse> {
  const { data } = await apiClient.put<ApiResponse<UserResponse>>(`/users/${id}`, payload);
  return data.data;
}

export async function deleteUser(id: string): Promise<void> {
  await apiClient.delete(`/users/${id}`);
}

export async function assignRoles(id: string, role_codes: string[]): Promise<UserResponse> {
  const { data } = await apiClient.post<ApiResponse<UserResponse>>(`/users/${id}/roles`, { role_codes });
  return data.data;
}

export async function revokeRole(id: string, roleCode: string): Promise<void> {
  await apiClient.delete(`/users/${id}/roles/${roleCode}`);
}

export async function disableUser(id: string): Promise<UserResponse> {
  const { data } = await apiClient.post<ApiResponse<UserResponse>>(`/users/${id}/disable`);
  return data.data;
}

export async function enableUser(id: string): Promise<UserResponse> {
  const { data } = await apiClient.post<ApiResponse<UserResponse>>(`/users/${id}/enable`);
  return data.data;
}

export async function getMe(): Promise<UserResponse> {
  const { data } = await apiClient.get<ApiResponse<UserResponse>>("/users/me");
  return data.data;
}

export async function updateMe(payload: UpdateMeRequest): Promise<UserResponse> {
  const { data } = await apiClient.put<ApiResponse<UserResponse>>("/users/me", payload);
  return data.data;
}

export async function changePassword(payload: ChangePasswordRequest): Promise<void> {
  await apiClient.post("/users/me/change-password", payload);
}

export async function setup2FA(): Promise<Setup2FAResponse> {
  const { data } = await apiClient.post<ApiResponse<Setup2FAResponse>>("/users/me/2fa/setup");
  return data.data;
}

export async function enable2FA(code: string): Promise<Enable2FAResponse> {
  const { data } = await apiClient.post<ApiResponse<Enable2FAResponse>>("/users/me/2fa/enable", { code });
  return data.data;
}

export async function disable2FA(password: string, code?: string): Promise<void> {
  await apiClient.post("/users/me/2fa/disable", { password, code });
}

export async function forgotPassword(email: string): Promise<void> {
  await apiClient.post("/auth/forgot-password", { email });
}

export async function resetPassword(token: string, new_password: string): Promise<void> {
  await apiClient.post("/auth/reset-password", { token, new_password });
}
