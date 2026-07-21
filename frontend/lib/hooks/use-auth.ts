"use client";

import { useRouter } from "next/navigation";
import { useAuthStore } from "@/lib/stores/auth-store";
import { login as apiLogin, logout as apiLogout } from "@/lib/api/auth";
import type { LoginRequest } from "@/lib/api/types";

export function useAuth() {
  const router = useRouter();
  const { user, isAuthenticated, accessToken, setAuth, clearAuth } =
    useAuthStore();

  async function login(payload: LoginRequest) {
    const response = await apiLogin(payload);
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
    // Đồng bộ cookie để middleware Edge đọc được (BUG-002bis)
    const maxAge = response.expiresIn ?? 86400;
    const secure = process.env.NODE_ENV === "production" ? "; Secure" : "";
    document.cookie = `his-access-token=${response.accessToken}; Path=/; Max-Age=${maxAge}; SameSite=Lax${secure}`;
    return response;
  }

  async function logout() {
    const { refreshToken } = useAuthStore.getState();
    try {
      if (refreshToken) {
        await apiLogout(refreshToken);
      }
    } catch {
      // ignore logout API errors
    } finally {
      clearAuth();
      // Xóa cookie đồng bộ với middleware (BUG-002bis)
      document.cookie = "his-access-token=; Path=/; Max-Age=0";
      router.push("/login");
    }
  }

  return {
    user,
    isAuthenticated,
    accessToken,
    login,
    logout,
  };
}
