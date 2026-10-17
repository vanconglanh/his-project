"use client";

import { useRouter } from "next/navigation";
import { useQueryClient } from "@tanstack/react-query";
import { useAuthStore } from "@/lib/stores/auth-store";
import { login as apiLogin, logout as apiLogout, ssoLogout } from "@/lib/api/auth";
import type { LoginRequest, LoginResponse } from "@/lib/api/types";

export function useAuth() {
  const router = useRouter();
  const queryClient = useQueryClient();
  const { user, isAuthenticated, accessToken, setAuth, clearAuth } =
    useAuthStore();
  const setMfaSetupToken = useAuthStore((s) => s.setMfaSetupToken);

  /**
   * Thiết lập phiên đăng nhập đầy đủ từ 1 LoginResponse "thật" (đủ token).
   * Dùng chung cho login thường LẪN bước verify 2FA thành công.
   */
  async function establishSession(response: LoginResponse) {
    // BE trả roles + roleCodes lồng trong user object. Gộp cả 2 để hook usePermissions check linh hoạt.
    const userRoles = (response.user?.roles ?? []) as string[];
    const userRoleCodes = (response.user?.roleCodes ?? []) as string[];
    const allRoles = [...userRoles, ...userRoleCodes];
    setAuth(
      response.user,
      response.accessToken,
      response.refreshToken,
      response.permissions ?? [],
      allRoles
    );
    // Set httpOnly cookie qua Route Handler để tránh XSS đọc token (NEW-001)
    await fetch("/session/set-cookie", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ accessToken: response.accessToken, expiresIn: response.expiresIn }),
    });
  }

  async function login(payload: LoginRequest): Promise<LoginResponse> {
    const response = await apiLogin(payload);
    // Trạng thái cần 2FA (nhập TOTP hoặc thiết lập 2FA) → KHÔNG thiết lập phiên,
    // trả response để LoginForm điều hướng sang bước phù hợp.
    if (response.requires2fa || response.mfaSetupRequired) {
      return response;
    }
    await establishSession(response);
    return response;
  }

  async function logout() {
    const { refreshToken } = useAuthStore.getState();
    // Single logout SSO: BE trả endSessionUrl khi phiên là SSO; phiên mật khẩu → null.
    let endSessionUrl: string | null = null;
    try {
      if (refreshToken) {
        await apiLogout(refreshToken);
      }
    } catch {
      // ignore logout API errors
    }
    try {
      endSessionUrl = await ssoLogout();
    } catch {
      // ignore: lỗi SSO logout → đăng xuất thường
    }
    clearAuth();
    // Xóa toàn bộ TanStack Query cache để tránh data của tài khoản trước bị lộ (BM-87)
    queryClient.clear();
    // Xóa httpOnly cookie qua Route Handler (NEW-001)
    try {
      await fetch("/session/clear-cookie", { method: "POST" });
    } catch {
      // ignore
    }
    if (endSessionUrl) {
      // RP-initiated logout: điều hướng toàn trang sang IdP để đóng phiên DiaB.
      window.location.href = endSessionUrl;
      return;
    }
    router.push("/login");
  }

  return {
    user,
    isAuthenticated,
    accessToken,
    login,
    logout,
    establishSession,
    setMfaSetupToken,
  };
}
