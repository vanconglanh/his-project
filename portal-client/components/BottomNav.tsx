"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { cn } from "@/lib/utils";

const TABS = [
  { href: "/", label: "Trang chủ", icon: "🏠" },
  { href: "/queue", label: "Hàng đợi", icon: "🎫" },
  { href: "/appointments", label: "Đặt lịch", icon: "📅" },
  { href: "/me", label: "Hồ sơ", icon: "👤" },
];

export function BottomNav() {
  const pathname = usePathname();

  return (
    <nav
      aria-label="Điều hướng chính"
      className="fixed inset-x-0 bottom-0 z-40 border-t border-[--color-border] bg-white/95 backdrop-blur"
      style={{ paddingBottom: "env(safe-area-inset-bottom)" }}
    >
      <ul className="grid grid-cols-4">
        {TABS.map((tab) => {
          const isActive = tab.href === "/" ? pathname === "/" : pathname.startsWith(tab.href);
          return (
            <li key={tab.href}>
              <Link
                href={tab.href}
                aria-current={isActive ? "page" : undefined}
                className={cn(
                  "flex min-h-[64px] flex-col items-center justify-center gap-1 py-2 text-sm font-semibold",
                  "focus-visible:outline-none focus-visible:ring-[3px] focus-visible:ring-[--color-focus] focus-visible:ring-inset",
                  isActive ? "text-[--color-primary]" : "text-[--color-text-muted]"
                )}
              >
                <span className="text-2xl" aria-hidden="true">
                  {tab.icon}
                </span>
                <span>{tab.label}</span>
              </Link>
            </li>
          );
        })}
      </ul>
    </nav>
  );
}
