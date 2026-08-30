"use client";

/**
 * Trang "Trung tâm trợ giúp" — liệt kê toàn bộ tour hướng dẫn (product tour) hiện có
 * trong registry, lọc theo quyền của user đang đăng nhập, nhóm theo module nghiệp vụ.
 * Bấm "Xem lại hướng dẫn" -> điều hướng sang đúng trang, gắn `?tour=1` để trang đích
 * tự kích hoạt tour ngay khi load (xem components/domain/TourButton.tsx).
 */
import { useRouter } from "next/navigation";
import { HelpCircle, PlayCircle, Sparkles } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { PageHeader } from "@/components/ui/page-header";
import { usePermissions } from "@/lib/hooks/use-permissions";
import { TOUR_CATALOG } from "@/lib/tours";
import { onboardingTour } from "@/lib/tours/onboarding";
import { onboardingSeenKey } from "@/lib/tours/engine";
import { useAuthStore } from "@/lib/stores/auth-store";

export default function HelpCenterPage() {
  const router = useRouter();
  const { hasAny } = usePermissions();
  const userId = useAuthStore((s) => s.user?.id);

  const visibleTours = TOUR_CATALOG.filter(
    (e) => !e.permissions || e.permissions.length === 0 || hasAny(e.permissions)
  );

  const groups = visibleTours.reduce<Record<string, typeof visibleTours>>((acc, entry) => {
    (acc[entry.module] ??= []).push(entry);
    return acc;
  }, {});

  function openTour(route: string, forceAutoStart: boolean) {
    router.push(forceAutoStart ? `${route}?tour=1` : route);
  }

  function replayOnboarding() {
    if (userId == null) return;
    // Xoá trạng thái đã xem để tour onboarding tự chạy lại ngay khi vào trang bất kỳ,
    // sau đó về trang chủ (nơi mọi thành phần dùng chung — sidebar, chi nhánh... đều có mặt).
    try {
      window.localStorage.removeItem(onboardingSeenKey(userId));
    } catch {
      /* localStorage bị chặn -> bỏ qua */
    }
    router.push("/");
  }

  return (
    <div className="space-y-6">
      <PageHeader
        title="Trung tâm trợ giúp"
        description="Xem lại hướng dẫn sử dụng cho từng màn hình trong hệ thống, phù hợp với vai trò của bạn."
      />

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2 text-base">
            <Sparkles className="h-4 w-4 text-primary" aria-hidden="true" />
            {onboardingTour.name}
          </CardTitle>
        </CardHeader>
        <CardContent className="flex flex-wrap items-center justify-between gap-3">
          <p className="text-sm text-muted-foreground max-w-2xl">
            Giới thiệu các thành phần dùng chung mọi trang: menu điều hướng, đổi chi nhánh, tìm
            kiếm toàn cục, thông báo và menu tài khoản. Tour này đã tự chạy 1 lần khi bạn đăng
            nhập lần đầu — bấm nút bên cạnh nếu muốn xem lại.
          </p>
          <Button
            onClick={replayOnboarding}
            disabled={userId == null}
            className="min-h-[44px]"
          >
            <PlayCircle className="h-4 w-4 mr-1.5" aria-hidden="true" />
            Xem lại hướng dẫn
          </Button>
        </CardContent>
      </Card>

      {Object.keys(groups).length === 0 && (
        <Card>
          <CardContent className="flex flex-col items-center justify-center gap-2 py-10 text-center text-muted-foreground">
            <HelpCircle className="h-8 w-8" aria-hidden="true" />
            <p>Chưa có hướng dẫn nào phù hợp với vai trò hiện tại của bạn.</p>
          </CardContent>
        </Card>
      )}

      {Object.entries(groups).map(([module, entries]) => (
        <div key={module} className="space-y-3">
          <h2 className="text-sm font-semibold text-muted-foreground uppercase tracking-wider">
            {module}
          </h2>
          <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-3">
            {entries.map((entry) => (
              <Card key={entry.tour.id} data-testid={`help-tour-${entry.tour.id}`}>
                <CardHeader>
                  <CardTitle className="text-base">{entry.tour.name}</CardTitle>
                </CardHeader>
                <CardContent className="space-y-3">
                  <p className="text-sm text-muted-foreground">{entry.description}</p>
                  <Button
                    variant="outline"
                    className="min-h-[44px] w-full"
                    data-testid={`help-tour-btn-${entry.tour.id}`}
                    onClick={() => openTour(entry.route, !entry.requiresRecord)}
                  >
                    <PlayCircle className="h-4 w-4 mr-1.5" aria-hidden="true" />
                    {entry.requiresRecord ? "Đi tới trang danh sách" : "Xem lại hướng dẫn"}
                  </Button>
                </CardContent>
              </Card>
            ))}
          </div>
        </div>
      ))}
    </div>
  );
}
