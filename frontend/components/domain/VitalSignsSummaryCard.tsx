"use client";

import { Activity, AlertTriangle, Plus } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { EmptyState } from "@/components/ui/EmptyState";
import { formatVnTime } from "@/lib/utils/encounter-format";

export interface VitalSignsSummaryCardProps {
  vital?: Record<string, unknown> | null;
  measuredAt?: string | null;
  onViewAll: () => void;
  onAddNew?: () => void;
}

interface VitalItem {
  label: string;
  value: string;
  abnormal: boolean;
}

function num(v: unknown): number | null {
  return typeof v === "number" && !Number.isNaN(v) ? v : null;
}

function buildItems(vital: Record<string, unknown>): VitalItem[] {
  const temp = num(vital.temperature_c);
  const hr = num(vital.heart_rate_bpm);
  const sys = num(vital.bp_systolic);
  const dia = num(vital.bp_diastolic);
  const spo2 = num(vital.spo2_percent);
  const weight = num(vital.weight_kg);
  const glucose = num(vital.glucose_mg_dl);

  const items: VitalItem[] = [];
  if (temp != null) items.push({ label: "Nhiệt độ", value: `${temp}°C`, abnormal: temp > 38 });
  if (hr != null) items.push({ label: "Mạch", value: `${hr} l/ph`, abnormal: hr < 50 || hr > 100 });
  if (sys != null && dia != null)
    items.push({ label: "Huyết áp", value: `${sys}/${dia}`, abnormal: sys > 140 || dia > 90 });
  if (spo2 != null) items.push({ label: "SpO2", value: `${spo2}%`, abnormal: spo2 < 95 });
  if (weight != null) items.push({ label: "Cân nặng", value: `${weight} kg`, abnormal: false });
  if (glucose != null)
    items.push({ label: "Đường huyết", value: `${glucose} mg/dL`, abnormal: glucose > 180 });
  return items;
}

export function VitalSignsSummaryCard({
  vital,
  measuredAt,
  onViewAll,
  onAddNew,
}: VitalSignsSummaryCardProps) {
  const items = vital ? buildItems(vital) : [];

  return (
    <Card>
      <CardHeader className="pb-2 pt-3 px-4">
        <CardTitle className="text-sm font-semibold flex items-center justify-between gap-2">
          <span className="flex items-center gap-1.5">
            <Activity className="h-4 w-4" aria-hidden="true" />
            Sinh hiệu
          </span>
          <Button variant="ghost" size="sm" className="h-7 text-xs" onClick={onViewAll}>
            Xem tất cả
          </Button>
        </CardTitle>
      </CardHeader>
      <CardContent className="px-4 pb-3 space-y-3">
        {items.length === 0 ? (
          <EmptyState
            variant="generic"
            title="Chưa có sinh hiệu"
            description="Ghi sinh hiệu để theo dõi trong suốt ca khám."
            className="py-6"
            action={onAddNew ? { label: "Ghi sinh hiệu", onClick: onAddNew } : undefined}
          />
        ) : (
          <>
            <div className="grid grid-cols-2 gap-2">
              {items.map((item) => (
                <div key={item.label}>
                  <p className="text-xs text-muted-foreground">{item.label}</p>
                  <p
                    className={
                      item.abnormal
                        ? "text-sm font-semibold text-[color:var(--status-critical)] flex items-center gap-1"
                        : "text-sm font-medium tabular-nums"
                    }
                  >
                    {item.abnormal && (
                      <AlertTriangle
                        className="h-3.5 w-3.5 shrink-0"
                        aria-label="Giá trị bất thường"
                      />
                    )}
                    <span className="tabular-nums">{item.value}</span>
                  </p>
                </div>
              ))}
            </div>
            {measuredAt && (
              <p className="text-xs text-muted-foreground">Đo lúc {formatVnTime(measuredAt)}</p>
            )}
            {onAddNew && (
              <Button
                variant="outline"
                size="sm"
                className="w-full gap-1 min-h-[44px]"
                onClick={onAddNew}
                data-tour="enc-vital"
              >
                <Plus className="h-4 w-4" aria-hidden="true" />
                Ghi sinh hiệu
              </Button>
            )}
          </>
        )}
      </CardContent>
    </Card>
  );
}
