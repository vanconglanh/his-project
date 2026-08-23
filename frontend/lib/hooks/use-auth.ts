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
    // BE trả roles (tên hiển thị) + roleCodes (mã ổn định) lồng trong user object.
    // KHÔNG gộp chung 2 mảng: roles chỉ dùng để HIỂN THỊ lên UI, roleCodes là nguồn
    // DUY NHẤT cho logic phân quyền/super-admin (usePermissions) — nếu gộp, tenant
    // tạo role có tên hiển thị trùng "Admin" sẽ bị FE nhầm là super admin dù BE đã
    // chặn đúng (menu quản trị hiện ra rồi bấm vào lỗi 403).
    const userRoles = (response.user?.roles ?? []) as string[];
    const userRoleCodes = (response.user?.roleCodes ?? []) as string[];
    setAuth(
      response.user,
      response.accessToken,
      response.refreshToken,
      response.permissions ?? [],
      userRoles,
      userRoleCodes
    );
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
