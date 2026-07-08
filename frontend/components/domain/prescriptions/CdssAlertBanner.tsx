"use client";

import { useEffect, useMemo, useState } from "react";
import { AlertOctagon, AlertTriangle, Info, ShieldAlert } from "lucide-react";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { cn } from "@/lib/utils";
import { useCdssCheck } from "@/lib/hooks/use-cdss";
import { CdssOverrideModal } from "./CdssOverrideModal";
import type { CdssAlertResponse, CdssDrugInput, CdssSeverity } from "@/lib/api/types";

const SEVERITY_CONFIG: Record<
  CdssSeverity,
  { label: string; badgeClass: string; alertClass: string; iconClass: string; Icon: React.ElementType }
> = {
  CONTRAINDICATED: {
    label: "Chống chỉ định",
    badgeClass: "bg-red-600 text-white border-red-700",
    alertClass: "border-red-600 bg-red-50 dark:border-red-800 dark:bg-red-950/40",
    iconClass: "text-red-700",
    Icon: AlertOctagon,
  },
  MAJOR: {
    label: "Nghiêm trọng",
    badgeClass: "bg-red-100 text-red-800 border-red-300",
    alertClass: "border-red-300 bg-red-50 dark:border-red-800 dark:bg-red-950/30",
    iconClass: "text-red-600",
    Icon: AlertOctagon,
  },
  MODERATE: {
    label: "Trung bình",
    badgeClass: "bg-amber-100 text-amber-800 border-amber-300",
    alertClass: "border-amber-300 bg-amber-50 dark:border-amber-800 dark:bg-amber-950/30",
    iconClass: "text-amber-600",
    Icon: AlertTriangle,
  },
  MINOR: {
    label: "Nhẹ",
    badgeClass: "bg-blue-100 text-blue-800 border-blue-300",
    alertClass: "border-blue-300 bg-blue-50 dark:border-blue-800 dark:bg-blue-950/30",
    iconClass: "text-blue-600",
    Icon: Info,
  },
};

function alertKey(a: CdssAlertResponse) {
  return `${a.rule_type}::${a.rule_code ?? ""}`;
}

interface Props {
  items: CdssDrugInput[];
  patientId?: string;
  encounterId?: string;
  prescriptionId?: string;
  /** Gọi lại mỗi khi trạng thái chặn ký thay đổi (còn cảnh báo interruptive chưa override) */
  onBlockingChange?: (blocking: boolean) => void;
}

export function CdssAlertBanner({ items, patientId, encounterId, prescriptionId, onBlockingChange }: Props) {
  const [debouncedItems, setDebouncedItems] = useState<CdssDrugInput[]>(items);
  const [overriddenKeys, setOverriddenKeys] = useState<Set<string>>(new Set());
  const [overrideAlert, setOverrideAlert] = useState<CdssAlertResponse | null>(null);

  // Debounce ~400ms khi danh sách thuốc thay đổi
  useEffect(() => {
    const t = setTimeout(() => setDebouncedItems(items), 400);
    return () => clearTimeout(t);
  }, [items]);

  const validItems = useMemo(
    () => debouncedItems.filter((i) => i.drug_id || i.ingredient || i.atc_code),
    [debouncedItems]
  );

  const { data, isFetching } = useCdssCheck(
    {
      patient_id: patientId,
      encounter_id: encounterId,
      prescription_id: prescriptionId,
      items: validItems,
    },
    validItems.length > 0
  );

  const alerts = useMemo(() => data?.alerts ?? [], [data]);

  const blockingAlerts = useMemo(
    () => alerts.filter((a) => a.is_interruptive && !overriddenKeys.has(alertKey(a))),
    [alerts, overriddenKeys]
  );

  useEffect(() => {
    onBlockingChange?.(blockingAlerts.length > 0);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [blockingAlerts.length]);

  if (validItems.length === 0) return null;

  if (isFetching && !data) {
    return <Skeleton className="h-16 w-full" />;
  }

  if (alerts.length === 0) return null;

  const nonBlocking = alerts.filter((a) => !a.is_interruptive);

  return (
    <div className="space-y-2">
      {blockingAlerts.length > 0 && (
        <Alert className="border-red-600 bg-red-50 dark:border-red-800 dark:bg-red-950/40">
          <ShieldAlert className="h-4 w-4 text-red-700" />
          <AlertDescription className="text-red-800 dark:text-red-200 font-semibold">
            Phát hiện {blockingAlerts.length} cảnh báo nghiêm trọng — không thể ký đơn cho đến khi xử lý hoặc bỏ qua
            có lý do.
          </AlertDescription>
        </Alert>
      )}

      <div className="space-y-2">
        {[...blockingAlerts, ...nonBlocking].map((alert) => {
          const cfg = SEVERITY_CONFIG[alert.severity] ?? SEVERITY_CONFIG.MODERATE;
          const { Icon } = cfg;
          const isOverridden = overriddenKeys.has(alertKey(alert));
          return (
            <div key={alertKey(alert)} className={cn("rounded-md border p-3 flex gap-3", cfg.alertClass)}>
              <Icon className={cn("h-4 w-4 mt-0.5 shrink-0", cfg.iconClass)} />
              <div className="flex-1 min-w-0 space-y-1">
                <div className="flex items-center gap-2 flex-wrap">
                  <span className="text-sm font-medium">{alert.title}</span>
                  <Badge className={cn("text-[10px] px-1.5 py-0 border", cfg.badgeClass)} variant="outline">
                    {cfg.label}
                  </Badge>
                  {isOverridden && (
                    <Badge variant="secondary" className="text-[10px]">
                      Đã bỏ qua
                    </Badge>
                  )}
                </div>
                <p className="text-xs text-foreground/80">{alert.detail}</p>
                {alert.management && (
                  <p className="text-xs text-foreground/70">
                    <span className="font-medium">Xử trí: </span>
                    {alert.management}
                  </p>
                )}
              </div>
              {alert.is_interruptive && !isOverridden && (
                <Button
                  size="sm"
                  variant="outline"
                  className="h-8 shrink-0 self-start"
                  onClick={() => setOverrideAlert(alert)}
                >
                  Bỏ qua cảnh báo
                </Button>
              )}
            </div>
          );
        })}
      </div>

      <CdssOverrideModal
        open={!!overrideAlert}
        onClose={() => setOverrideAlert(null)}
        alert={overrideAlert}
        patientId={patientId}
        encounterId={encounterId}
        prescriptionId={prescriptionId}
        onOverridden={(alert) =>
          setOverriddenKeys((prev) => {
            const next = new Set(prev);
            next.add(alertKey(alert));
            return next;
          })
        }
      />
    </div>
  );
}
