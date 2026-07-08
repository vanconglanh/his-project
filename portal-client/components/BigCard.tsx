"use client";

import type { ReactNode } from "react";
import Link from "next/link";
import { cn } from "@/lib/utils";

interface BigCardProps {
  href?: string;
  onClick?: () => void;
  icon: ReactNode;
  title: string;
  subtitle?: string;
  badge?: ReactNode;
  className?: string;
}

export function BigCard({ href, onClick, icon, title, subtitle, badge, className }: BigCardProps) {
  const content = (
    <div
      className={cn(
        "flex min-h-[112px] w-full items-center gap-4 rounded-2xl border border-[--color-border] bg-white p-5 shadow-sm",
        "transition-transform duration-150 hover:shadow-md active:scale-[0.98]",
        "focus-visible:outline-none focus-visible:ring-[3px] focus-visible:ring-[--color-focus] focus-visible:ring-offset-2",
        className
      )}
    >
      <div className="flex h-14 w-14 shrink-0 items-center justify-center rounded-xl bg-[--color-primary-soft] text-3xl text-[--color-primary]">
        {icon}
      </div>
      <div className="flex-1 text-left">
        <p className="text-xl font-bold text-[--color-text]">{title}</p>
        {subtitle ? <p className="mt-1 text-base text-[--color-text-muted]">{subtitle}</p> : null}
      </div>
      {badge}
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
    <button type="button" onClick={onClick} className="block w-full text-left" aria-label={title}>
      {content}
    </button>
  );
}
