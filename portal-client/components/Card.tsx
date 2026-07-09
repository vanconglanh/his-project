import { cn } from "@/lib/utils";
import type { HTMLAttributes } from "react";

/**
 * Card nền trắng chuẩn diaB: bo góc lớn, bóng đổ mềm tint brand, viền gần như vô hình.
 * Dùng thay cho các <div rounded-2xl border-2 border-slate-200 ...> rải rác để đồng nhất.
 */
export function Card({ className, ...rest }: HTMLAttributes<HTMLDivElement>) {
  return (
    <div
      className={cn(
        "rounded-2xl border border-[var(--border-soft)] bg-white p-4",
        "shadow-[var(--shadow-card)]",
        className,
      )}
      {...rest}
    />
  );
}
