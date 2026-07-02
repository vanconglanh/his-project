"use client";

import { useState } from "react";
import { Sheet, SheetContent, SheetHeader, SheetTitle } from "@/components/ui/sheet";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";
import { useVitalSigns } from "@/lib/hooks/use-vital-signs";
import type { VitalSignsResponse } from "@/lib/api/types";
import { Activity } from "lucide-react";

interface Props {
  encounterId: string;
  open: boolean;
  onClose: () => void;
  onEdit?: (record: VitalSignsResponse) => void;
}

function canEdit(createdAt: string): boolean {
  const diff = Date.now() - new Date(createdAt).getTime();
  return diff < 24 * 60 * 60 * 1000;
}

export function VitalSignsHistoryDrawer({ encounterId, open, onClose, onEdit }: Props) {
  const { data, isLoading } = useVitalSigns(encounterId);

  return (
    <Sheet open={open} onOpenChange={(v) => !v && onClose()}>
      <SheetContent side="right" className="w-full sm:max-w-xl overflow-y-auto">
        <SheetHeader>
          <SheetTitle className="flex items-center gap-2">
            <Activity className="h-5 w-5 text-primary" />
            Nhật ký sinh hiệu
          </SheetTitle>
        </SheetHeader>
        <div className="mt-4 space-y-3">
          {isLoading ? (
            [1, 2, 3].map((i) => <Skeleton key={i} className="h-24 w-full" />)
          ) : !data || data.length === 0 ? (
            <p className="text-sm text-muted-foreground text-center py-8">Chưa có sinh hiệu</p>
          ) : (
            data.map((record) => (
              <VitalCard
                key={record.id}
                record={record}
                onEdit={canEdit(record.created_at) && onEdit ? () => onEdit(record) : undefined}
              />
            ))
          )}
        </div>
      </SheetContent>
    </Sheet>
  );
}

function VitalCard({ record, onEdit }: { record: VitalSignsResponse; onEdit?: () => void }) {
  return (
    <div className="rounded-lg border bg-card p-3 space-y-2">
      <div className="flex items-center justify-between">
        <div>
          <span className="text-xs text-muted-foreground">
            {new Date(record.created_at).toLocaleString("vi-VN")} — {record.recorded_by_name}
          </span>
          <Badge variant="outline" className="ml-2 text-xs">
            #{record.record_sequence}
          </Badge>
        </div>
        {onEdit && (
          <Button variant="ghost" size="sm" onClick={onEdit}>
            Sửa
          </Button>
        )}
      </div>
      <div className="grid grid-cols-3 gap-2 text-sm">
        {record.temperature_c != null && (
          <VitalItem label="Nhiệt độ" value={`${record.temperature_c}°C`} />
        )}
        {record.heart_rate_bpm != null && (
          <VitalItem label="Mạch" value={`${record.heart_rate_bpm} lần/phút`} />
        )}
        {record.bp_systolic != null && record.bp_diastolic != null && (
          <VitalItem label="HA" value={`${record.bp_systolic}/${record.bp_diastolic} mmHg`} />
        )}
        {record.spo2_percent != null && (
          <VitalItem label="SpO2" value={`${record.spo2_percent}%`} />
        )}
        {record.weight_kg != null && (
          <VitalItem label="Cân nặng" value={`${record.weight_kg} kg`} />
        )}
        {record.bmi != null && (
          <VitalItem label="BMI" value={String(record.bmi)} />
        )}
        {record.glucose_mg_dl != null && (
          <VitalItem label="Đường huyết" value={`${record.glucose_mg_dl} mg/dL`} />
        )}
        {record.pain_scale != null && (
          <VitalItem label="Đau" value={`${record.pain_scale}/10`} />
        )}
      </div>
      {record.note && (
        <p className="text-xs text-muted-foreground">{record.note}</p>
      )}
    </div>
  );
}

function VitalItem({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <p className="text-xs text-muted-foreground">{label}</p>
      <p className="font-medium">{value}</p>
    </div>
  );
}
