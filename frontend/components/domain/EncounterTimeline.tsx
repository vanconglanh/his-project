"use client";

import {
  Activity,
  FlaskConical,
  ImageIcon,
  Pill,
  FileText,
  PenTool,
  Stethoscope,
  MessageSquare,
} from "lucide-react";
import { useEncounterTimeline } from "@/lib/hooks/use-encounters";
import { Skeleton } from "@/components/ui/skeleton";
import { Badge } from "@/components/ui/badge";
import { cn } from "@/lib/utils";
import type { TimelineEvent } from "@/lib/api/types";

interface Props {
  encounterId: string;
}

const EVENT_CONFIG: Record<
  TimelineEvent["event_type"],
  { icon: React.ElementType; label: string; color: string }
> = {
  VITAL: { icon: Activity, label: "Sinh hiệu", color: "text-blue-500" },
  LAB_ORDER: { icon: FlaskConical, label: "XN", color: "text-purple-500" },
  RAD_ORDER: { icon: ImageIcon, label: "CĐHA", color: "text-indigo-500" },
  PRESCRIPTION: { icon: Pill, label: "Đơn thuốc", color: "text-green-500" },
  NOTE: { icon: MessageSquare, label: "Ghi chú", color: "text-gray-500" },
  DIAGNOSIS: { icon: Stethoscope, label: "Chẩn đoán", color: "text-orange-500" },
  EMR_SAVED: { icon: FileText, label: "Lưu BA", color: "text-cyan-500" },
  EMR_SIGNED: { icon: PenTool, label: "Ký BA", color: "text-emerald-600" },
};

export function EncounterTimeline({ encounterId }: Props) {
  const { data, isLoading } = useEncounterTimeline(encounterId);

  if (isLoading) {
    return (
      <div className="space-y-4">
        {[1, 2, 3, 4].map((i) => (
          <div key={i} className="flex gap-3">
            <Skeleton className="h-8 w-8 rounded-full shrink-0" />
            <div className="flex-1 space-y-1">
              <Skeleton className="h-4 w-2/3" />
              <Skeleton className="h-3 w-1/2" />
            </div>
          </div>
        ))}
      </div>
    );
  }

  if (!data || data.length === 0) {
    return (
      <p className="text-sm text-muted-foreground text-center py-8">
        Chưa có hoạt động nào được ghi nhận
      </p>
    );
  }

  const sorted = [...data].sort(
    (a, b) => new Date(b.timestamp).getTime() - new Date(a.timestamp).getTime()
  );

  return (
    <div className="relative">
      <div className="absolute left-4 top-0 bottom-0 w-px bg-border" />
      <div className="space-y-4 pl-10">
        {sorted.map((event, idx) => {
          const cfg = EVENT_CONFIG[event.event_type];
          const Icon = cfg?.icon ?? FileText;
          return (
            <div key={`${event.timestamp}-${idx}`} className="relative">
              <div
                className={cn(
                  "absolute -left-10 flex h-8 w-8 items-center justify-center rounded-full border bg-card",
                  cfg?.color ?? "text-muted-foreground"
                )}
              >
                <Icon className="h-4 w-4" />
              </div>
              <div className="rounded-lg border bg-card p-3">
                <div className="flex items-center justify-between gap-2 flex-wrap">
                  <div className="flex items-center gap-2">
                    <Badge variant="outline" className="text-xs">
                      {cfg?.label ?? event.event_type}
                    </Badge>
                    <span className="text-sm font-medium">{event.summary}</span>
                  </div>
                  <span className="text-xs text-muted-foreground">
                    {new Date(event.timestamp).toLocaleString("vi-VN")}
                  </span>
                </div>
                <p className="mt-0.5 text-xs text-muted-foreground">
                  {event.actor}
                  {event.actor_role && ` — ${event.actor_role}`}
                </p>
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
}
