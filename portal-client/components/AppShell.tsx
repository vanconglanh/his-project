"use client";

import { usePathname } from "next/navigation";
import { BottomNav } from "@/components/BottomNav";

const NO_NAV_PREFIXES = ["/login", "/activate"];

/** Bọc nội dung trang + hiển thị bottom-nav, trừ màn hình đăng nhập/kích hoạt */
export function AppShell({ children }: { children: React.ReactNode }) {
  const pathname = usePathname();
  const hideNav = NO_NAV_PREFIXES.some((p) => pathname.startsWith(p));

  return (
    <div className="mx-auto min-h-screen max-w-lg pb-24">
      {children}
      {!hideNav && <BottomNav />}
    </div>
  );
}
