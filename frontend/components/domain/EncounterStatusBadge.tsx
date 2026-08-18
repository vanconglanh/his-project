import { HisStatusBadge, type HisStatusVariant } from "@/components/ui/status-badge";
import type { EncounterStatus } from "@/lib/api/types";

interface Props {
  status: EncounterStatus;
  className?: string;
}

/** Ánh xạ trạng thái lượt khám sang variant token của HisStatusBadge (không hardcode màu) */
const STATUS_MAP: Record<string, { label: string; variant: HisStatusVariant }> = {
  WAITING: { label: "Chờ khám", variant: "waiting" },
  WAITING_CLS: { label: "Chờ kết quả CLS", variant: "waiting" },
  IN_PROGRESS: { label: "Đang khám", variant: "progress" },
  DONE: { label: "Hoàn thành", variant: "done" },
  CANCELLED: { label: "Đã huỷ", variant: "critical" },
};

export function EncounterStatusBadge({ status, className }: Props) {
  const cfg = STATUS_MAP[status] ?? STATUS_MAP.WAITING;
  return (
    <HisStatusBadge variant={cfg.variant} className={className}>
      {cfg.label}
    </HisStatusBadge>
  );
}
