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
  // BM-86: backend nhan body snake_case (refresh_token). Gui thang payload { refreshToken }
  // (camelCase) -> 400 -> proactive refresh (useTokenRefresh) that bai -> dang xuat giua chung.
  const { data } = await apiClient.post<ApiResponse<RefreshTokenResponse>>(
    "/auth/refresh",
    { refresh_token: payload.refreshToken }
  );
  return data.data;
}

function ssoAuthHeader(): Record<string, string> {
  try {
    const raw = typeof window !== "undefined" ? localStorage.getItem("auth-store") : null;
    const token = raw ? JSON.parse(raw)?.state?.accessToken : null;
    return token ? { Authorization: `Bearer ${token}` } : {};
  } catch {
    return {};
  }
}

/**
 * Single logout SSO. Dùng axios "trần" (không qua interceptor 401→refresh→redirect)
 * để lỗi không đá user về /login giữa chừng. Trả endSessionUrl (phiên SSO) hoặc null.
 */
export async function ssoLogout(): Promise<string | null> {
  const { data } = await axios.post<ApiResponse<{ endSessionUrl?: string | null }>>(
    `${API_BASE_URL}/api/v1/auth/sso/logout`,
    {},
    { withCredentials: true, timeout: 10000, headers: ssoAuthHeader() }
  );
  return data?.data?.endSessionUrl || null;
}

export async function logout(refreshToken: string): Promise<void> {
  await apiClient.post("/auth/logout", { refreshToken });
}
