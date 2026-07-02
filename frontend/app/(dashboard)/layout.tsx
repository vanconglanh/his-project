import { AppSidebar } from "@/components/layout/AppSidebar";
import { AppTopbar } from "@/components/layout/AppTopbar";
import { CommandPalette } from "@/components/layout/CommandPalette";
import { ShortcutsModal } from "@/components/layout/ShortcutsModal";
import { GlobalKeyHandler } from "@/components/layout/GlobalKeyHandler";

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
        <main className="flex-1 overflow-y-auto p-4 md:p-6">{children}</main>
      </div>
      {/* Global UI overlays */}
      <CommandPalette />
      <ShortcutsModal />
      <GlobalKeyHandler />
    </div>
  );
}
