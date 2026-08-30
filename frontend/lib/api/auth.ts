import axios from "axios";
import apiClient, { API_BASE_URL } from "./client";
import type {
  LoginRequest,
  LoginResponse,
  RefreshTokenRequest,
  RefreshTokenResponse,
  Verify2FARequest,
  ApiResponse,
} from "./types";

export async function login(payload: LoginRequest): Promise<LoginResponse> {
  const { data } = await apiClient.post<ApiResponse<LoginResponse>>(
    "/auth/login",
    payload
  );
  return data.data;
}

/**
 * Bước 2 của login khi user đã bật 2FA. Dùng axios "trần" (không qua apiClient
 * interceptor) vì lỗi 401 (mã sai / token hết hạn) KHÔNG được kích hoạt cơ chế
 * refresh + redirect /login của interceptor — LoginForm tự xử lý theo error.code.
 */
export async function verify2fa(payload: Verify2FARequest): Promise<LoginResponse> {
  const { data } = await axios.post<ApiResponse<LoginResponse>>(
    `${API_BASE_URL}/api/v1/auth/2fa/verify`,
    payload,
    { headers: { "Content-Type": "application/json" } }
  );
  return data.data;
}

export async function refreshToken(
  payload: RefreshTokenRequest
): Promise<RefreshTokenResponse> {
  const { data } = await apiClient.post<ApiResponse<RefreshTokenResponse>>(
    "/auth/refresh",
    payload
  );
  return data.data;
}

export async function logout(refreshToken: string): Promise<void> {
  await apiClient.post("/auth/logout", { refreshToken });
}
