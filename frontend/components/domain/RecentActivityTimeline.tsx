"use client";

import { useState, useEffect } from "react";
import { UserPlus, Stethoscope, Pill, Receipt, FlaskConical } from "lucide-react";
import { formatDistanceToNow } from "date-fns";
import { vi } from "date-fns/locale";

type ActivityType = "PATIENT_REGISTERED" | "ENCOUNTER_OPENED" | "PRESCRIPTION_ISSUED" | "PAYMENT_RECEIVED" | "LAB_RESULT_READY";

interface ActivityItem {
  id: string;
  type: ActivityType;
  description: string;
  actor: string;
  offsetMs: number; // offset từ "now" tính lúc mount
}

const typeConfig: Record<ActivityType, { icon: React.ElementType; color: string }> = {
  PATIENT_REGISTERED: { icon: UserPlus, color: "bg-emerald-100 text-emerald-700" },
  ENCOUNTER_OPENED: { icon: Stethoscope, color: "bg-blue-100 text-blue-700" },
  PRESCRIPTION_ISSUED: { icon: Pill, color: "bg-purple-100 text-purple-700" },
  PAYMENT_RECEIVED: { icon: Receipt, color: "bg-amber-100 text-amber-700" },
  LAB_RESULT_READY: { icon: FlaskConical, color: "bg-rose-100 text-rose-700" },
};

// Mock recent activity — will be replaced when audit-log API is ready
// Dùng offsetMs thay vì new Date(Date.now() - ...) ở module scope để tránh hydration mismatch
const MOCK_ACTIVITY_DEFS: ActivityItem[] = [
  { id: "1", type: "PATIENT_REGISTERED", description: "BN Nguyễn Văn A đăng ký mới", actor: "LT. Hương", offsetMs: 4 * 60 * 1000 },
  { id: "2", type: "ENCOUNTER_OPENED", description: "Khám bệnh #00234 — BS. Minh", actor: "BS. Minh", offsetMs: 12 * 60 * 1000 },
  { id: "3", type: "PRESCRIPTION_ISSUED", description: "Đơn thuốc #Rx00189 đã ký & đẩy ĐTQG", actor: "BS. Lan", offsetMs: 25 * 60 * 1000 },
  { id: "4", type: "PAYMENT_RECEIVED", description: "Thanh toán 450.000 ₫ — BN Trần B", actor: "KT. Nam", offsetMs: 40 * 60 * 1000 },
  { id: "5", type: "LAB_RESULT_READY", description: "Kết quả XN HbA1c sẵn sàng — BN Lê C", actor: "KTV. Hà", offsetMs: 58 * 60 * 1000 },
];

export function RecentActivityTimeline() {
  // timestamps chỉ được tính ở client sau mount để tránh hydration mismatch
  const [now, setNow] = useState<number | null>(null);

  useEffect(() => {
    setNow(Date.now());
  }, []);

  return (
    <div className="space-y-3">
      {MOCK_ACTIVITY_DEFS.map((item) => {
        const cfg = typeConfig[item.type];
        const Icon = cfg.icon;
        const ts = now !== null ? new Date(now - item.offsetMs) : null;
        return (
          <div key={item.id} className="flex items-start gap-3">
            <div className={`flex h-8 w-8 shrink-0 items-center justify-center rounded-full text-xs ${cfg.color}`}>
              <Icon className="h-4 w-4" />
            </div>
            <div className="flex-1 min-w-0">
              <p className="text-sm text-foreground leading-snug">{item.description}</p>
              <p className="text-xs text-muted-foreground mt-0.5" suppressHydrationWarning>
                {item.actor} &middot;{" "}
                {ts
                  ? formatDistanceToNow(ts, { addSuffix: true, locale: vi })
                  : "—"}
              </p>
            </div>
          </div>
        );
      })}
    </div>
  );
}
