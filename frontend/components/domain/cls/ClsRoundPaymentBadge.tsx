"use client";

import { HisStatusBadge } from "@/components/ui/status-badge";
import type { ClsRoundPaymentStatus } from "@/lib/api/cls-rounds";

export interface ClsRoundPaymentBadgeProps {
  /** null/undefined = đơn cũ không thuộc đợt nào → hiển thị "—", KHÔNG đoán trạng thái */
  status?: ClsRoundPaymentStatus | null;
  cancelled?: boolean;
}

export function ClsRoundPaymentBadge({ status, cancelled }: ClsRoundPaymentBadgeProps) {
  if (cancelled) return <HisStatusBadge variant="critical">Đợt đã huỷ</HisStatusBadge>;
  if (!status) {
    return (
      <span
        className="inline-flex items-center rounded-full border border-border bg-muted/40 px-2 py-0.5 text-xs font-medium text-[color:var(--text-muted)]"
        aria-label="Chưa xác định trạng thái thanh toán"
      >
        —
      </span>
    );
  }
  if (status === "PAID") return <HisStatusBadge variant="done">Đã thanh toán</HisStatusBadge>;
  if (status === "WAIVED") return <HisStatusBadge variant="progress">Miễn viện phí</HisStatusBadge>;
  return <HisStatusBadge variant="warning">Chưa thanh toán</HisStatusBadge>;
}
