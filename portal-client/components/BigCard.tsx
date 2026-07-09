import Link from "next/link";
import { cn } from "@/lib/utils";
import type { ReactNode } from "react";

interface BigCardProps {
  href?: string;
  icon: ReactNode;
  title: string;
  subtitle?: string;
  badge?: string | number;
  className?: string;
  onClick?: () => void;
}

/** Thẻ lớn có icon + label chữ, dùng cho trang chủ (Hàng đợi/Đặt lịch/Kết quả/Hồ sơ) */
export function BigCard({ href, icon, title, subtitle, badge, className, onClick }: BigCardProps) {
  const content = (
    <div
      className={cn(
        "relative flex h-full min-h-28 flex-col items-center justify-center gap-2 rounded-2xl border border-[var(--border-soft)] bg-white p-4 text-center shadow-[var(--shadow-card)] transition-all",
        "hover:-translate-y-0.5 hover:shadow-[var(--shadow-card-hover)] active:translate-y-0",
        "focus-visible:outline-none focus-visible:ring-[3px] focus-visible:ring-teal-600 focus-visible:ring-offset-2",
        className,
      )}
    >
      {badge != null && (
        <span className="absolute right-2 top-2 flex min-w-6 items-center justify-center rounded-full bg-red-600 px-2 py-0.5 text-sm font-bold text-white">
          {badge}
        </span>
      )}
      <div className="flex h-14 w-14 items-center justify-center rounded-full bg-teal-50 text-teal-700" aria-hidden="true">
        {icon}
      </div>
      <span className="text-lg font-semibold text-slate-900">{title}</span>
      {subtitle && <span className="text-base text-slate-500">{subtitle}</span>}
    </div>
  );

  if (href) {
    return (
      <Link href={href} className="block h-full" aria-label={title}>
        {content}
      </Link>
    );
  }

  return (
    <button type="button" onClick={onClick} className="block h-full w-full" aria-label={title}>
      {content}
    </button>
  );
}
