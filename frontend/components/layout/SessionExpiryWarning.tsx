"use client";

/**
 * BM-86 (Expectation #2): cảnh báo trước khi hết phiên do không hoạt động (idle timeout).
 *
 * - Theo dõi hoạt động người dùng (mousemove/mousedown/keydown/scroll/touchstart).
 * - Idle đạt (IDLE_LIMIT_MS - WARNING_LEAD_MS) -> hiện modal đếm ngược WARNING_LEAD_MS.
 * - "Tiếp tục làm việc" -> refresh token + reset idle. "Đăng xuất" hoặc đếm ngược về 0 -> logout.
 * - Khi modal đang hiện, hoạt động nền KHÔNG tự đóng modal (tránh vô tình gia hạn).
 * - Lưu ý: use-token-refresh vẫn tự refresh JWT khi tab mở; component này là lớp idle riêng
 *   ở phía UI, quyết định đăng xuất khi user bỏ đi.
 */

import { useCallback, useEffect, useRef, useState } from "react";
import { useTranslations } from "next-intl";
import { useAuthStore } from "@/lib/stores/auth-store";
import { refreshToken as apiRefreshToken } from "@/lib/api/auth";
import {
  AlertDialog,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog";
import { Button } from "@/components/ui/button";

// Mặc định 30' idle, cảnh báo trước 2' — healthcare default; BA có thể chỉnh.
const IDLE_LIMIT_MS = 30 * 60 * 1000;
const WARNING_LEAD_MS = 2 * 60 * 1000;
const ACTIVITY_THROTTLE_MS = 5 * 1000;
const ACTIVITY_EVENTS = ["mousemove", "mousedown", "keydown", "scroll", "touchstart"] as const;

export function SessionExpiryWarning() {
  const t = useTranslations("Auth.sessionExpiry");
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);
  const clearAuth = useAuthStore((s) => s.clearAuth);
  const updateTokens = useAuthStore((s) => s.updateTokens);

  const [open, setOpen] = useState(false);
  const [secondsLeft, setSecondsLeft] = useState(Math.floor(WARNING_LEAD_MS / 1000));
  const [extending, setExtending] = useState(false);

  const lastActivityRef = useRef<number>(Date.now());
  const lastResetRef = useRef<number>(0);
  const openRef = useRef(false);
  const warnAtRef = useRef<number>(0); // thời điểm modal hiện (để tính countdown chính xác)

  const logout = useCallback(async () => {
    openRef.current = false;
    setOpen(false);
    clearAuth();
    try {
      await fetch("/session/clear-cookie", { method: "POST" });
    } catch {
      // bỏ qua, vẫn redirect
    }
    window.location.href = "/login";
  }, [clearAuth]);

  const extend = useCallback(async () => {
    const current = useAuthStore.getState().refreshToken;
    if (!current) {
      await logout();
      return;
    }
    setExtending(true);
    try {
      const res = await apiRefreshToken({ refreshToken: current });
      updateTokens(res.accessToken, res.refreshToken);
      await fetch("/session/set-cookie", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ accessToken: res.accessToken, expiresIn: res.expiresIn }),
      });
      lastActivityRef.current = Date.now();
      openRef.current = false;
      setOpen(false);
    } catch {
      await logout();
    } finally {
      setExtending(false);
    }
  }, [logout, updateTokens]);

  // Lắng nghe hoạt động (throttle) — chỉ reset khi modal chưa hiện
  useEffect(() => {
    if (!isAuthenticated) return;
    lastActivityRef.current = Date.now();

    const onActivity = () => {
      if (openRef.current) return;
      const now = Date.now();
      if (now - lastResetRef.current < ACTIVITY_THROTTLE_MS) return;
      lastResetRef.current = now;
      lastActivityRef.current = now;
    };

    ACTIVITY_EVENTS.forEach((e) => window.addEventListener(e, onActivity, { passive: true }));
    return () => {
      ACTIVITY_EVENTS.forEach((e) => window.removeEventListener(e, onActivity));
    };
  }, [isAuthenticated]);

  // Ticker 1s: kiểm tra idle / cập nhật đếm ngược (dùng timestamp, không lệch khi tab bị throttle)
  useEffect(() => {
    if (!isAuthenticated) return;

    const id = setInterval(() => {
      const now = Date.now();
      if (!openRef.current) {
        if (now - lastActivityRef.current >= IDLE_LIMIT_MS - WARNING_LEAD_MS) {
          openRef.current = true;
          warnAtRef.current = now;
          setSecondsLeft(Math.floor(WARNING_LEAD_MS / 1000));
          setOpen(true);
        }
        return;
      }
      const remainMs = WARNING_LEAD_MS - (now - warnAtRef.current);
      if (remainMs <= 0) {
        void logout();
      } else {
        setSecondsLeft(Math.ceil(remainMs / 1000));
      }
    }, 1000);

    return () => clearInterval(id);
  }, [isAuthenticated, logout]);

  if (!isAuthenticated) return null;

  const mm = String(Math.floor(secondsLeft / 60)).padStart(2, "0");
  const ss = String(secondsLeft % 60).padStart(2, "0");

  return (
    // Không cho đóng bằng click ngoài/Esc: user phải chọn một trong hai nút
    <AlertDialog open={open} onOpenChange={() => {}}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>{t("title")}</AlertDialogTitle>
          <AlertDialogDescription>{t("message")}</AlertDialogDescription>
        </AlertDialogHeader>
        <p className="text-center text-lg font-semibold tabular-nums" role="timer" aria-live="polite">
          {t("countdown", { seconds: `${mm}:${ss}` })}
        </p>
        <AlertDialogFooter>
          <Button variant="outline" className="min-h-11" onClick={() => void logout()}>
            {t("logoutButton")}
          </Button>
          <Button className="min-h-11" disabled={extending} onClick={() => void extend()}>
            {t("extendButton")}
          </Button>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  );
}
