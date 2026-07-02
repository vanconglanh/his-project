"use client";

import { usePathname } from "next/navigation";
import { Menu, Search } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Separator } from "@/components/ui/separator";
import { UserMenu } from "./UserMenu";
import { ThemeToggle } from "./ThemeToggle";
import { useUiStore } from "@/lib/stores/ui-store";
import { NotificationDropdown } from "@/components/domain/NotificationDropdown";

const BREADCRUMB_MAP: Record<string, string> = {
  "/": "Tổng quan",
  "/reception": "Tiếp đón",
  "/patients": "Bệnh nhân",
  "/encounters": "Khám bệnh",
  "/labrad": "CLS",
  "/prescriptions": "Kê đơn",
  "/pharmacy": "Kho dược",
  "/cashier": "Thu ngân",
  "/bhyt": "BHYT",
  "/reports": "Báo cáo",
  "/admin": "Quản trị",
  "/billings": "Hoá đơn",
  "/drugs": "Danh mục thuốc",
  "/nurse": "Điều dưỡng",
  "/services": "Dịch vụ",
};

export function AppTopbar() {
  const pathname = usePathname();
  const { toggleSidebar, setCommandPaletteOpen } = useUiStore();

  const segment = "/" + pathname.split("/")[1];
  const pageTitle = BREADCRUMB_MAP[segment] ?? "Pro-Diab HIS";

  return (
    <header className="sticky top-0 z-30 flex h-16 shrink-0 items-center gap-3 border-b bg-background/95 backdrop-blur px-4">
      {/* Mobile sidebar toggle */}
      <Button
        variant="ghost"
        size="icon"
        onClick={toggleSidebar}
        className="lg:hidden min-h-[44px] min-w-[44px]"
        aria-label="Toggle menu"
      >
        <Menu className="h-5 w-5" aria-hidden="true" />
      </Button>

      {/* Page title */}
      <div className="flex items-center gap-2 min-w-0">
        <span className="font-semibold text-base truncate">{pageTitle}</span>
      </div>

      {/* Command palette trigger */}
      <button
        onClick={() => setCommandPaletteOpen(true)}
        className="hidden md:flex items-center gap-2 px-3 py-1.5 text-sm text-muted-foreground bg-muted/60 hover:bg-muted rounded-md border border-border transition-colors ml-2 min-h-[36px]"
        aria-label="Mở tìm kiếm (Ctrl+K)"
      >
        <Search className="h-3.5 w-3.5" aria-hidden="true" />
        <span className="hidden lg:inline">Tìm kiếm...</span>
        <kbd className="hidden lg:inline-flex items-center gap-1 text-xs border border-border rounded px-1 py-0.5 bg-background">
          ⌘K
        </kbd>
      </button>

      <div className="flex-1" />

      {/* Right actions */}
      <div className="flex items-center gap-1">
        {/* Mobile search icon */}
        <Button
          variant="ghost"
          size="icon"
          className="md:hidden min-h-[44px] min-w-[44px]"
          onClick={() => setCommandPaletteOpen(true)}
          aria-label="Tìm kiếm"
        >
          <Search className="h-5 w-5" aria-hidden="true" />
        </Button>
        <NotificationDropdown />
        <ThemeToggle />
        <Separator orientation="vertical" className="h-6 mx-1" />
        <UserMenu />
      </div>
    </header>
  );
}
