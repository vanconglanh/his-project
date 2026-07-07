"use client";

import { AlertTriangle, AlertOctagon, Info } from "lucide-react";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { cn } from "@/lib/utils";
import type { DeteriorationFlag } from "@/lib/api/types";

const SEVERITY_CONFIG: Record<string, { icon: React.ElementType; className: string; label: string }> = {
  HIGH: {
    icon: AlertOctagon,
    className: "border-red-300 bg-red-50 text-red-800 dark:border-red-800 dark:bg-red-950/30 dark:text-red-200",
    label: "Nguy cơ cao",
  },
  MEDIUM: {
    icon: AlertTriangle,
    className:
      "border-amber-300 bg-amber-50 text-amber-800 dark:border-amber-800 dark:bg-amber-950/30 dark:text-amber-200",
    label: "Cảnh báo",
  },
  LOW: {
    icon: Info,
    className: "border-blue-300 bg-blue-50 text-blue-800 dark:border-blue-800 dark:bg-blue-950/30 dark:text-blue-200",
    label: "Lưu ý",
  },
};

interface Props {
  flags?: DeteriorationFlag[];
}

export function DeteriorationBanner({ flags }: Props) {
  if (!flags || flags.length === 0) return null;

  return (
    <div className="space-y-2">
      {flags.map((flag, idx) => {
        const cfg = SEVERITY_CONFIG[flag.severity] ?? SEVERITY_CONFIG.MEDIUM;
        const Icon = cfg.icon;
        return (
          <Alert key={`${flag.code}-${idx}`} className={cn(cfg.className)}>
            <Icon className="h-4 w-4" />
            <AlertTitle>
              {cfg.label} — {flag.code}
            </AlertTitle>
            <AlertDescription className="text-inherit">{flag.message}</AlertDescription>
          </Alert>
        );
      })}
    </div>
  );
}
