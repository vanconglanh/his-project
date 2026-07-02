"use client";

import { cn } from "@/lib/utils";
import type { LabResultFlag } from "@/lib/api/lab-results";

interface FlagBadgeProps {
  flag: LabResultFlag;
  className?: string;
}

const FLAG_CONFIG: Record<LabResultFlag, { label: string; className: string }> = {
  NORMAL: { label: "Bình thường", className: "bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-300" },
  H: { label: "Cao", className: "bg-orange-100 text-orange-800 dark:bg-orange-900/30 dark:text-orange-300" },
  L: { label: "Thấp", className: "bg-blue-100 text-blue-800 dark:bg-blue-900/30 dark:text-blue-300" },
  HH: { label: "Rất cao", className: "bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-300" },
  LL: { label: "Rất thấp", className: "bg-purple-100 text-purple-700 dark:bg-purple-900/30 dark:text-purple-300" },
  CRITICAL: { label: "Nguy kịch", className: "bg-red-600 text-white animate-pulse" },
};

export function FlagBadge({ flag, className }: FlagBadgeProps) {
  const config = FLAG_CONFIG[flag] ?? FLAG_CONFIG.NORMAL;
  return (
    <span
      className={cn(
        "inline-flex items-center rounded-full px-2 py-0.5 text-xs font-semibold",
        config.className,
        className
      )}
      aria-label={`Cờ: ${config.label}`}
    >
      {flag === "CRITICAL" ? "! " : ""}
      {config.label}
    </span>
  );
}
