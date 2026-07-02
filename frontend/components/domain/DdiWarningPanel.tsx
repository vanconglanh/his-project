"use client";

import { Alert, AlertDescription } from "@/components/ui/alert";
import { Badge } from "@/components/ui/badge";
import { cn } from "@/lib/utils";
import type { DdiWarning, DdiSeverity } from "@/lib/api/prescriptions";
import { AlertTriangle, AlertOctagon, Info, XOctagon } from "lucide-react";

const severityConfig: Record<
  DdiSeverity,
  { label: string; badgeClass: string; iconClass: string; Icon: React.ElementType; alertClass: string }
> = {
  MINOR: {
    label: "Nhẹ",
    badgeClass: "bg-yellow-100 text-yellow-800 border-yellow-300",
    iconClass: "text-yellow-600",
    Icon: Info,
    alertClass: "border-yellow-300 bg-yellow-50",
  },
  MODERATE: {
    label: "Trung bình",
    badgeClass: "bg-orange-100 text-orange-800 border-orange-300",
    iconClass: "text-orange-600",
    Icon: AlertTriangle,
    alertClass: "border-orange-300 bg-orange-50",
  },
  MAJOR: {
    label: "Nghiêm trọng",
    badgeClass: "bg-red-100 text-red-800 border-red-300",
    iconClass: "text-red-600",
    Icon: AlertOctagon,
    alertClass: "border-red-300 bg-red-50",
  },
  CONTRAINDICATED: {
    label: "Chống chỉ định",
    badgeClass: "bg-red-600 text-white border-red-700",
    iconClass: "text-red-700",
    Icon: XOctagon,
    alertClass: "border-red-600 bg-red-50",
  },
};

interface Props {
  warnings: DdiWarning[];
  hasContraindicated?: boolean;
}

export function DdiWarningPanel({ warnings, hasContraindicated }: Props) {
  if (!warnings || warnings.length === 0) return null;

  return (
    <div className="space-y-2">
      {hasContraindicated && (
        <Alert className="border-red-600 bg-red-50">
          <XOctagon className="h-4 w-4 text-red-700" />
          <AlertDescription className="text-red-800 font-semibold">
            Cảnh báo: Phát hiện cặp thuốc chống chỉ định. Không thể ký đơn cho đến khi xử lý.
          </AlertDescription>
        </Alert>
      )}

      <p className="text-sm font-medium text-foreground">
        Cảnh báo tương tác thuốc ({warnings.length})
      </p>

      <div className="space-y-2">
        {warnings.map((w, i) => {
          const cfg = severityConfig[w.severity];
          const { Icon } = cfg;
          return (
            <div
              key={i}
              className={cn("rounded-md border p-3 flex gap-3", cfg.alertClass)}
            >
              <Icon className={cn("h-4 w-4 mt-0.5 shrink-0", cfg.iconClass)} />
              <div className="flex-1 min-w-0 space-y-1">
                <div className="flex items-center gap-2 flex-wrap">
                  <span className="text-sm font-medium">
                    {w.drug1_name} + {w.drug2_name}
                  </span>
                  <Badge
                    className={cn("text-[10px] px-1.5 py-0 border", cfg.badgeClass)}
                    variant="outline"
                  >
                    {cfg.label}
                  </Badge>
                  <span className="text-[10px] text-muted-foreground">Bằng chứng: {w.evidence_level}</span>
                </div>
                <p className="text-xs text-foreground/80">{w.description}</p>
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
}
