"use client";

import { UserPlus, Stethoscope, Pill, Receipt, FlaskConical, Clock } from "lucide-react";
import { formatDistanceToNow } from "date-fns";
import { vi } from "date-fns/locale";

type ActivityType = "PATIENT_REGISTERED" | "ENCOUNTER_OPENED" | "PRESCRIPTION_ISSUED" | "PAYMENT_RECEIVED" | "LAB_RESULT_READY";

export interface ActivityItem {
  id: string;
  type: ActivityType;
  description: string;
  actor: string;
  created_at: string; // ISO 8601
}

const typeConfig: Record<ActivityType, { icon: React.ElementType; color: string }> = {
  PATIENT_REGISTERED: { icon: UserPlus, color: "bg-emerald-100 text-emerald-700" },
  ENCOUNTER_OPENED: { icon: Stethoscope, color: "bg-blue-100 text-blue-700" },
  PRESCRIPTION_ISSUED: { icon: Pill, color: "bg-purple-100 text-purple-700" },
  PAYMENT_RECEIVED: { icon: Receipt, color: "bg-amber-100 text-amber-700" },
  LAB_RESULT_READY: { icon: FlaskConical, color: "bg-rose-100 text-rose-700" },
};

interface Props {
  items?: ActivityItem[];
}

export function RecentActivityTimeline({ items }: Props) {
  if (!items || items.length === 0) {
    return (
      <div className="flex flex-col items-center justify-center py-6 text-muted-foreground gap-2">
        <Clock className="h-8 w-8 opacity-40" />
        <p className="text-sm">Chưa có hoạt động gần đây</p>
      </div>
    );
  }

  return (
    <div className="space-y-3">
      {items.map((item) => {
        const cfg = typeConfig[item.type];
        const Icon = cfg.icon;
        const ts = new Date(item.created_at);
        return (
          <div key={item.id} className="flex items-start gap-3">
            <div className={`flex h-8 w-8 shrink-0 items-center justify-center rounded-full text-xs ${cfg.color}`}>
              <Icon className="h-4 w-4" />
            </div>
            <div className="flex-1 min-w-0">
              <p className="text-sm text-foreground leading-snug">{item.description}</p>
              <p className="text-xs text-muted-foreground mt-0.5" suppressHydrationWarning>
                {item.actor} &middot;{" "}
                {formatDistanceToNow(ts, { addSuffix: true, locale: vi })}
              </p>
            </div>
          </div>
        );
      })}
    </div>
  );
}
