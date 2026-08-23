import { create } from "zustand";
import { persist } from "zustand/middleware";
import type { UserProfile } from "@/lib/api/types";

interface AuthState {
  user: UserProfile | null;
  accessToken: string | null;
  refreshToken: string | null;
  isAuthenticated: boolean;
  permissions: string[];
  /** Tên hiển thị vai trò (vd "Quản trị viên") — CHỈ dùng để hiển thị lên UI, KHÔNG dùng để tính quyền */
  roles: string[];
  /** Mã vai trò ổn định (vd "ADMIN") — nguồn duy nhất cho logic phân quyền/super-admin */
  roleCodes: string[];
}

interface AuthActions {
  setAuth: (
    user: UserProfile,
    accessToken: string,
    refreshToken: string,
    permissions?: string[],
    roles?: string[],
    roleCodes?: string[]
  ) => void;
  clearAuth: () => void;
  updateTokens: (accessToken: string, refreshToken: string) => void;
}

export const useAuthStore = create<AuthState & AuthActions>()(
  persist(
    (set) => ({
      user: null,
      accessToken: null,
      refreshToken: null,
      isAuthenticated: false,
      permissions: [],
      roles: [],
      roleCodes: [],

      setAuth: (user, accessToken, refreshToken, permissions = [], roles = [], roleCodes = []) =>
        set({ user, accessToken, refreshToken, isAuthenticated: true, permissions, roles, roleCodes }),

      clearAuth: () =>
        set({
          user: null,
          accessToken: null,
          refreshToken: null,
          isAuthenticated: false,
          permissions: [],
          roles: [],
          roleCodes: [],
        }),

      updateTokens: (accessToken, refreshToken) =>
        set({ accessToken, refreshToken }),
    }),
    {
      name: "auth-store",
      partialize: (state) => ({
        user: state.user,
        accessToken: state.accessToken,
        refreshToken: state.refreshToken,
        isAuthenticated: state.isAuthenticated,
        permissions: state.permissions,
        roles: state.roles,
        roleCodes: state.roleCodes,
      }),
    }
  )
);
