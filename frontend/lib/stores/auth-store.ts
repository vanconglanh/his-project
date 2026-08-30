import { create } from "zustand";
import { persist } from "zustand/middleware";
import type { UserProfile } from "@/lib/api/types";

interface AuthState {
  user: UserProfile | null;
  accessToken: string | null;
  refreshToken: string | null;
  isAuthenticated: boolean;
  permissions: string[];
  roles: string[];
}

interface AuthActions {
  setAuth: (
    user: UserProfile,
    accessToken: string,
    refreshToken: string,
    permissions?: string[],
    roles?: string[]
  ) => void;
  clearAuth: () => void;
  updateTokens: (accessToken: string, refreshToken: string) => void;
  /**
   * Lưu tạm token thiết lập 2FA (aud="mfa-setup") để apiClient tự đính kèm khi
   * gọi me/2fa/setup + me/2fa/enable. KHÔNG set isAuthenticated → user vẫn CHƯA
   * đăng nhập, không truy cập được dashboard / API nghiệp vụ.
   */
  setMfaSetupToken: (accessToken: string) => void;
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

      setAuth: (user, accessToken, refreshToken, permissions = [], roles = []) =>
        set({ user, accessToken, refreshToken, isAuthenticated: true, permissions, roles }),

      clearAuth: () =>
        set({
          user: null,
          accessToken: null,
          refreshToken: null,
          isAuthenticated: false,
          permissions: [],
          roles: [],
        }),

      updateTokens: (accessToken, refreshToken) =>
        set({ accessToken, refreshToken }),

      setMfaSetupToken: (accessToken) =>
        set({
          accessToken,
          refreshToken: null,
          user: null,
          isAuthenticated: false,
          permissions: [],
          roles: [],
        }),
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
      }),
    }
  )
);
