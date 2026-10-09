import { AppSidebar } from "@/components/layout/AppSidebar";
import { AppTopbar } from "@/components/layout/AppTopbar";
import { CommandPalette } from "@/components/layout/CommandPalette";
import { ShortcutsModal } from "@/components/layout/ShortcutsModal";
import { GlobalKeyHandler } from "@/components/layout/GlobalKeyHandler";
import { OnboardingTour } from "@/components/domain/OnboardingTour";
import { TokenRefreshGuard } from "@/components/layout/TokenRefreshGuard";

export default function DashboardLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <div className="flex h-screen overflow-hidden">
      <AppSidebar />
      <div className="flex flex-1 flex-col overflow-hidden min-w-0">
        <AppTopbar />
        {/* BM-79: overflow-x-hidden tránh layout vỡ ngang ở cửa sổ hẹp */}
        <main className="flex-1 overflow-y-auto overflow-x-hidden p-4 md:p-6">{children}</main>
      </div>
      {/* Global UI overlays */}
      <CommandPalette />
      <ShortcutsModal />
      <GlobalKeyHandler />
      <OnboardingTour />
      {/* BM-86: Proactive JWT refresh để tránh logout đột ngột khi đang hoạt động */}
      <TokenRefreshGuard />
    </div>
  );
}
