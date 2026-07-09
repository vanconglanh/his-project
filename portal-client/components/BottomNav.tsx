"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { cn } from "@/lib/utils";
import { HomeIcon, QueueIcon, CalendarIcon, UserIcon, HeartPulseIcon } from "@/components/icons";

const TABS = [
  { href: "/", label: "Trang chủ", Icon: HomeIcon },
  { href: "/queue", label: "Hàng đợi", Icon: QueueIcon },
  { href: "/health", label: "Sức khoẻ", Icon: HeartPulseIcon },
  { href: "/appointments", label: "Đặt lịch", Icon: CalendarIcon },
  { href: "/me", label: "Hồ sơ", Icon: UserIcon },
];

/** Thanh điều hướng dưới cùng 4 tab lớn, cố định, dễ bấm cho mọi lứa tuổi */
export function BottomNav() {
  const pathname = usePathname();

  return (
    <nav
      aria-label="Điều hướng chính"
      className="fixed inset-x-0 bottom-0 z-40 border-t border-slate-200 bg-white pb-[env(safe-area-inset-bottom)]"
    >
      <ul className="mx-auto grid max-w-lg grid-cols-5">
        {TABS.map(({ href, label, Icon }) => {
          const active = href === "/" ? pathname === "/" : pathname.startsWith(href);
          return (
            <li key={href}>
              <Link
                href={href}
                aria-current={active ? "page" : undefined}
                className={cn(
                  "flex min-h-16 flex-col items-center justify-center gap-1 text-sm font-medium",
                  "focus-visible:outline-none focus-visible:ring-[3px] focus-visible:ring-teal-600 focus-visible:ring-inset",
                  active ? "text-[#01645A]" : "text-slate-500",
                )}
              >
                <span
                  className={cn(
                    "flex h-11 w-11 items-center justify-center rounded-full transition-colors",
                    active ? "bg-[#01645A] text-white shadow-md" : "",
                  )}
                >
                  <Icon className="h-6 w-6" />
                </span>
                <span className={cn(active && "font-semibold")}>{label}</span>
              </Link>
            </li>
          );
        })}
      </ul>
    </nav>
  );
}
