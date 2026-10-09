"use client";

/**
 * BM-86: Proactive token refresh — tự động refresh access token trước khi hết hạn
 * 2 phút, tránh user bị đăng xuất đột ngột khi đang hoạt động.
 *
 * Cách hoạt động:
 * - Mount hook này trong dashboard layout (sau khi đăng nhập thành công).
 * - Đọc accessToken từ auth-store, decode payload để lấy `exp` (unix timestamp).
 * - Đặt setTimeout để refresh trước khi exp 2 phút (120s).
 * - Sau mỗi lần refresh thành công, cập nhật token và đặt lại timer.
 * - Nếu refresh thất bại → logout.
 */

import { useEffect, useRef } from "react";
import { useAuthStore } from "@/lib/stores/auth-store";
import { refreshToken as apiRefreshToken } from "@/lib/api/auth";

const REFRESH_BEFORE_EXPIRY_MS = 2 * 60 * 1000; // 2 phút trước khi hết hạn

function getTokenExp(token: string): number | null {
  try {
    const payload = JSON.parse(atob(token.split(".")[1]));
    return typeof payload.exp === "number" ? payload.exp * 1000 : null;
  } catch {
    return null;
  }
}

export function useTokenRefresh() {
  const timerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const accessToken = useAuthStore((s) => s.accessToken);
  const refreshTokenValue = useAuthStore((s) => s.refreshToken);
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);
  const updateTokens = useAuthStore((s) => s.updateTokens);
  const clearAuth = useAuthStore((s) => s.clearAuth);

  useEffect(() => {
    if (!isAuthenticated || !accessToken || !refreshTokenValue) return;

    const exp = getTokenExp(accessToken);
    if (!exp) return;

    const now = Date.now();
    const msUntilRefresh = Math.max(exp - now - REFRESH_BEFORE_EXPIRY_MS, 0);

    if (timerRef.current) clearTimeout(timerRef.current);

    timerRef.current = setTimeout(async () => {
      const currentRefreshToken = useAuthStore.getState().refreshToken;
      if (!currentRefreshToken) return;

      try {
        const res = await apiRefreshToken({ refreshToken: currentRefreshToken });
        updateTokens(res.accessToken, res.refreshToken);
        // Cập nhật httpOnly cookie (NEW-001)
        await fetch("/session/set-cookie", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ accessToken: res.accessToken, expiresIn: res.expiresIn }),
        });
      } catch {
        // Refresh thất bại → đăng xuất
        clearAuth();
        await fetch("/session/clear-cookie", { method: "POST" });
        window.location.href = "/login";
      }
    }, msUntilRefresh);

    return () => {
      if (timerRef.current) clearTimeout(timerRef.current);
    };
  }, [accessToken, refreshTokenValue, isAuthenticated, updateTokens, clearAuth]);
}
