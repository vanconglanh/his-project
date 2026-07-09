"use client";

import { usePathname } from "next/navigation";
import { BottomNav } from "@/components/BottomNav";
import { Header } from "@/components/Header";

const NO_CHROME_PREFIXES = ["/login", "/activate"];

/** Bọc nội dung trang + header thương hiệu + bottom-nav, trừ màn đăng nhập/kích hoạt */
export function AppShell({ children }: { children: React.ReactNode }) {
  const pathname = usePathname();
  const hideChrome = NO_CHROME_PREFIXES.some((p) => pathname.startsWith(p));

  return (
    <div className="mx-auto min-h-screen max-w-lg pb-24">
      {!hideChrome && <Header />}
      {children}
      {!hideChrome && <BottomNav />}
    </div>
  );
}
