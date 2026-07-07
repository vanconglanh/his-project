import { Badge } from "@/components/ui/badge";
import { cn } from "@/lib/utils";
import type { AppointmentStatus, AppointmentSource } from "@/lib/api/appointments";

export const APPOINTMENT_STATUS_LABEL: Record<AppointmentStatus, string> = {
  PENDING: "Chờ xác nhận",
  CONFIRMED: "Đã xác nhận",
  CHECKED_IN: "Đã check-in",
  CANCELLED: "Đã huỷ",
  NO_SHOW: "Không đến",
};

const STATUS_CONFIG: Record<AppointmentStatus, string> = {
  PENDING:
    "bg-[color:var(--status-warning)]/10 text-[color:var(--status-warning)] border-[color:var(--status-warning)]/30",
  CONFIRMED: "bg-primary/10 text-primary border-primary/30",
  CHECKED_IN:
    "bg-blue-500/10 text-blue-600 border-blue-500/30 dark:text-blue-400",
  CANCELLED:
    "bg-[color:var(--status-critical)]/10 text-[color:var(--status-critical)] border-[color:var(--status-critical)]/30",
  NO_SHOW:
    "bg-rose-400/10 text-rose-500 border-rose-400/30 dark:text-rose-400",
};

export function AppointmentStatusBadge({ status }: { status: AppointmentStatus }) {
  return (
    <Badge
      variant="outline"
      className={cn("text-xs font-medium", STATUS_CONFIG[status])}
    >
      {APPOINTMENT_STATUS_LABEL[status]}
    </Badge>
  );
}

export const APPOINTMENT_SOURCE_LABEL: Record<AppointmentSource, string> = {
  WALK_IN: "Vãng lai",
  PHONE: "Điện thoại",
  WEB: "Website",
  API: "API",
  APP: "Ứng dụng",
};
