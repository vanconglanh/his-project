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
        "relative flex min-h-28 flex-col items-center justify-center gap-2 rounded-2xl border-2 border-slate-200 bg-white p-4 text-center shadow-sm transition-colors",
        "hover:border-teal-500 hover:bg-teal-50",
        "focus-visible:outline-none focus-visible:ring-[3px] focus-visible:ring-teal-600 focus-visible:ring-offset-2",
        className,
      )}
    >
      {badge != null && (
        <span className="absolute right-2 top-2 flex min-w-6 items-center justify-center rounded-full bg-red-600 px-2 py-0.5 text-sm font-bold text-white">
          {badge}
        </span>
      )}
      <div className="text-teal-700" aria-hidden="true">
        {icon}
      </div>
      <span className="text-lg font-semibold text-slate-900">{title}</span>
      {subtitle && <span className="text-base text-slate-500">{subtitle}</span>}
    </div>
  );

  if (href) {
    return (
      <Link href={href} className="block" aria-label={title}>
        {content}
      </Link>
    );
  }

  return (
    <button type="button" onClick={onClick} className="block w-full" aria-label={title}>
      {content}
    </button>
  );
}
