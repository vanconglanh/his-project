"use client";

/**
 * Tự động chạy tour "Làm quen hệ thống" đúng 1 lần khi 1 user cụ thể lần đầu
 * đăng nhập vào hệ thống — độc lập với tour trang lẻ (xem TourButton.tsx).
 * Đặt trong layout dashboard để chạy trên mọi trang, nhưng chỉ thực sự kích hoạt
 * khi user chưa từng xem (kiểm tra localStorage key `tour-onboarding-seen:{userId}`).
 */
import { useCallback, useEffect, useRef, useState } from "react";
import { usePermissions } from "@/lib/hooks/use-permissions";
import { useAuthStore } from "@/lib/stores/auth-store";
import { onboardingTour } from "@/lib/tours/onboarding";
import { runOnboardingTour, isOnboardingSeen } from "@/lib/tours/engine";

/** Sự kiện global bắn ra khi tour onboarding kết thúc (hoặc không có gì để chạy) —
 *  TourButton lắng nghe để tránh chạy đè tour trang lẻ trong lúc onboarding đang chạy. */
export const ONBOARDING_DONE_EVENT = "prodiab:onboarding-tour-done";

export function OnboardingTour() {
  const { has } = usePermissions();
  const userId = useAuthStore((s) => s.user?.id);
  const can = useCallback((code: string) => has(code), [has]);

  const [mounted, setMounted] = useState(false);
  useEffect(() => setMounted(true), []);

  const ranFor = useRef<number | string | null>(null);
  useEffect(() => {
    if (!mounted || userId == null) return;
    if (ranFor.current === userId) return;
    if (isOnboardingSeen(userId)) return;

    ranFor.current = userId;
    const t = window.setTimeout(() => {
      runOnboardingTour({
        tour: onboardingTour,
        can,
        userId,
        // Trì hoãn 1 tick trước khi bắn sự kiện: onDone được gọi từ bên trong
        // onDestroyed của driver.js instance onboarding — nếu tạo driver.js
        // instance MỚI (tour trang lẻ) ngay lập tức trong cùng call stack đó
        // (reentrant), driver.js sẽ gắn sai sự kiện cho nút đóng (X) của
        // instance mới, khiến markTourSeen không bao giờ chạy khi đóng bằng X.
        onDone: () =>
          window.setTimeout(
            () => window.dispatchEvent(new CustomEvent(ONBOARDING_DONE_EVENT)),
            50
          ),
      });
    }, 900);
    return () => window.clearTimeout(t);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [mounted, userId]);

  return null;
}
