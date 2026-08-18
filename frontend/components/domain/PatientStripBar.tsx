"use client";

import { AlertTriangle, ChevronRight } from "lucide-react";
import { Button } from "@/components/ui/button";
import { SimpleAvatar } from "@/components/domain/SimpleAvatar";
import { cn } from "@/lib/utils";

export interface PatientStripBarProps {
  fullName: string;
  subtitle?: string | null;
  patientCode?: string | null;
  bloodPressure?: string | null;
  bloodPressureAbnormal?: boolean;
  onOpenProfile: () => void;
  className?: string;
}

/** Thanh tóm tắt bệnh nhân 1 dòng — chỉ hiện dưới 1024px (thay cho sidebar) */
export function PatientStripBar({
  fullName,
  subtitle,
  patientCode,
  bloodPressure,
  bloodPressureAbnormal,
  onOpenProfile,
  className,
}: PatientStripBarProps) {
  return (
    <div
      className={cn(
        "sticky top-14 z-10 flex h-12 items-center gap-2 border-b border-border bg-card/95 px-3 backdrop-blur",
        className
      )}
    >
      <SimpleAvatar name={fullName} size="sm" />
      <span className="font-medium truncate">{fullName}</span>
      {subtitle && (
        <span className="text-xs text-muted-foreground truncate hidden sm:inline">· {subtitle}</span>
      )}
      {patientCode && (
        <span className="text-xs text-muted-foreground font-mono tabular-nums hidden sm:inline">
          · {patientCode}
        </span>
      )}
      {bloodPressure && (
        <span
          className={cn(
            "text-xs tabular-nums inline-flex items-center gap-1",
            bloodPressureAbnormal
              ? "text-[color:var(--status-critical)] font-semibold"
              : "text-muted-foreground"
          )}
        >
          · HA {bloodPressure}
          {bloodPressureAbnormal && (
            <AlertTriangle className="h-3 w-3" aria-label="Giá trị bất thường" />
          )}
        </span>
      )}
      <Button
        variant="outline"
        size="sm"
        className="ml-auto gap-1 min-h-[36px]"
        onClick={onOpenProfile}
        aria-label="Mở hồ sơ bệnh nhân"
      >
        Hồ sơ
        <ChevronRight className="h-4 w-4" aria-hidden="true" />
      </Button>
    </div>
  );
}
