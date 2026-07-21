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
    // Set httpOnly cookie qua Route Handler để tránh XSS đọc token (NEW-001)
    await fetch("/session/set-cookie", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ accessToken: response.accessToken, expiresIn: response.expiresIn }),
    });
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
      // Xóa httpOnly cookie qua Route Handler (NEW-001)
      await fetch("/session/clear-cookie", { method: "POST" });
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
