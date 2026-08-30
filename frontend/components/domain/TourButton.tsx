"use client";

/**
 * Nút "Hướng dẫn" đặt ở topbar. Kích hoạt product tour (driver.js) đúng theo
 * route + vai trò hiện tại. Đồng thời tự động chạy tour 1 lần đầu khi user vào
 * trang lần đầu (nếu chưa từng xem — lưu trong localStorage).
 */
import { useCallback, useEffect, useRef, useState } from "react";
import { usePathname, useRouter, useSearchParams } from "next/navigation";
import { HelpCircle } from "lucide-react";
import { Button } from "@/components/ui/button";
import { usePermissions } from "@/lib/hooks/use-permissions";
import { useAuthStore } from "@/lib/stores/auth-store";
import { resolveTour, normalizeRouteKey } from "@/lib/tours";
import {
  runTour,
  countAvailableSteps,
  isTourSeen,
  isOnboardingSeen,
} from "@/lib/tours/engine";
import { ONBOARDING_DONE_EVENT } from "@/components/domain/OnboardingTour";

export function TourButton() {
  const pathname = usePathname();
  const router = useRouter();
  const searchParams = useSearchParams();
  const { has } = usePermissions();
  const userId = useAuthStore((s) => s.user?.id);

  // Chỉ render sau khi mount để tránh lệch SSR (driver.js + localStorage là client-only).
  const [mounted, setMounted] = useState(false);
  useEffect(() => setMounted(true), []);

  const can = useCallback((code: string) => has(code), [has]);

  const tour = resolveTour(pathname, can);
  const routeKey = normalizeRouteKey(pathname);

  const start = useCallback(() => {
    if (!tour || userId == null) return;
    runTour({ tour, can, route: routeKey, userId });
  }, [tour, can, routeKey, userId]);

  // Auto-chạy 1 lần đầu tiên vào trang (nếu chưa xem), HOẶC chạy cưỡng bức khi có
  // query `?tour=1` (đi từ trang Trung tâm trợ giúp /help, bấm "Xem lại hướng dẫn").
  // Dùng ref chống chạy 2 lần do StrictMode / re-render. Delay nhẹ để DOM (tabs, form) render xong.
  const autoRanFor = useRef<string | null>(null);
  const forceStart = searchParams.get("tour") === "1";
  useEffect(() => {
    if (!mounted || !tour || userId == null) return;
    if (autoRanFor.current === routeKey) return;
    if (!forceStart && isTourSeen(routeKey, userId)) return;

    autoRanFor.current = routeKey;
    const uid = userId;

    function startPageTour() {
      // Kiểm tra lại số bước khả dụng — nếu 0 (element chưa render đủ) vẫn để runTour
      // tự đánh dấu đã xem, tránh lặp.
      if (countAvailableSteps(tour!, can) > 0) {
        runTour({ tour: tour!, can, route: routeKey, userId: uid });
      }
      if (forceStart) {
        const params = new URLSearchParams(searchParams.toString());
        params.delete("tour");
        const qs = params.toString();
        router.replace(qs ? `${pathname}?${qs}` : pathname, { scroll: false });
      }
    }

    // Nếu tour "Làm quen hệ thống" (onboarding) chưa được xem -> chờ nó chạy xong
    // trước rồi mới auto-chạy tour trang lẻ, tránh 2 tour đè lên nhau khi đăng nhập lần đầu.
    if (!forceStart && !isOnboardingSeen(uid)) {
      const onOnboardingDone = () => {
        window.clearTimeout(fallback);
        startPageTour();
      };
      window.addEventListener(ONBOARDING_DONE_EVENT, onOnboardingDone, { once: true });
      const fallback = window.setTimeout(() => {
        window.removeEventListener(ONBOARDING_DONE_EVENT, onOnboardingDone);
        startPageTour();
      }, 12000);
      return () => {
        window.removeEventListener(ONBOARDING_DONE_EVENT, onOnboardingDone);
        window.clearTimeout(fallback);
      };
    }

    const t = window.setTimeout(startPageTour, 800);
    return () => window.clearTimeout(t);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [mounted, routeKey, userId, forceStart]);

  if (!mounted) return null;

  const hasTour = tour != null && userId != null;
  const stepCount = hasTour ? countAvailableSteps(tour!, can) : 0;
  const disabled = !hasTour || stepCount === 0;

  const label = disabled
    ? "Chưa có hướng dẫn cho trang này"
    : "Hướng dẫn sử dụng trang này";

  return (
    <Button
      variant="ghost"
      size="icon"
      onClick={start}
      disabled={disabled}
      className="min-h-[44px] min-w-[44px]"
      aria-label={label}
      title={label}
      data-tour="topbar-help"
    >
      <HelpCircle className="h-5 w-5" aria-hidden="true" />
    </Button>
  );
}
