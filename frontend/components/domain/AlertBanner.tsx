"use client";

import Link from "next/link";
import { AlertTriangle, XCircle, Info, X } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";
import type { AlertItem } from "@/lib/api/dashboard";

interface Props {
  alerts: AlertItem[];
  onDismiss?: (id: string) => void;
}

const severityConfig = {
  CRITICAL: { icon: XCircle, cls: "bg-red-50 border-red-200 text-red-800 dark:bg-red-950/30 dark:border-red-800 dark:text-red-300" },
  WARNING: { icon: AlertTriangle, cls: "bg-amber-50 border-amber-200 text-amber-800 dark:bg-amber-950/30 dark:border-amber-800 dark:text-amber-300" },
  INFO: { icon: Info, cls: "bg-blue-50 border-blue-200 text-blue-800 dark:bg-blue-950/30 dark:border-blue-800 dark:text-blue-300" },
};

export function AlertBanner({ alerts, onDismiss }: Props) {
  if (!alerts.length) return null;

  return (
    <div className="space-y-2">
      {alerts.map((alert) => {
        const cfg = severityConfig[alert.severity];
        const Icon = cfg.icon;
        return (
          <div
            key={alert.id}
            className={cn("flex items-center gap-3 rounded-lg border px-4 py-3 text-sm", cfg.cls)}
            role="alert"
          >
            <Icon className="h-4 w-4 shrink-0" />
            <span className="flex-1">{alert.message}</span>
            {alert.count > 1 && (
              <Badge variant="outline" className="text-xs">
                {alert.count}
              </Badge>
            )}
            {alert.link && (
              <Link href={alert.link} className="underline text-xs font-medium hover:no-underline">
                Xem
              </Link>
            )}
            {onDismiss && (
              <Button
                variant="ghost"
                size="sm"
                className="h-6 w-6 p-0 -mr-1"
                onClick={() => onDismiss(alert.id)}
                aria-label="Bỏ qua cảnh báo"
              >
                <X className="h-3 w-3" />
              </Button>
            )}
          </div>
        );
      })}
    </div>
  );
}
