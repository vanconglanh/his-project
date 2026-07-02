import { Badge } from "@/components/ui/badge";
import { cn } from "@/lib/utils";
import type { TenantStatus, UserStatus } from "@/lib/api/types";

type Status = TenantStatus | UserStatus;

const STATUS_CONFIG: Record<Status, { label: string; className: string }> = {
  ACTIVE: {
    label: "Hoạt động",
    className:
      "bg-[color:var(--status-done)]/10 text-[color:var(--status-done)] border border-[color:var(--status-done)]/30",
  },
  SUSPENDED: {
    label: "Tạm ngưng",
    className:
      "bg-[color:var(--status-warning)]/10 text-[color:var(--status-warning)] border border-[color:var(--status-warning)]/30",
  },
  TERMINATED: {
    label: "Chấm dứt",
    className:
      "bg-[color:var(--status-critical)]/10 text-[color:var(--status-critical)] border border-[color:var(--status-critical)]/30",
  },
  PENDING: {
    label: "Chờ kích hoạt",
    className:
      "bg-[color:var(--status-waiting)]/10 text-[color:var(--status-waiting)] border border-[color:var(--status-waiting)]/30",
  },
  LOCKED: {
    label: "Bị khoá",
    className:
      "bg-[color:var(--status-critical)]/10 text-[color:var(--status-critical)] border border-[color:var(--status-critical)]/30",
  },
  DISABLED: {
    label: "Vô hiệu",
    className:
      "text-[color:var(--text-muted)] bg-[color:var(--bg-elevated)] border border-[color:var(--border-default)]",
  },
};

interface StatusBadgeProps {
  status: Status;
  className?: string;
}

export function StatusBadge({ status, className }: StatusBadgeProps) {
  const config = STATUS_CONFIG[status] ?? { label: status, className: "" };
  return (
    <Badge
      variant="secondary"
      className={cn("text-xs font-medium", config.className, className)}
    >
      {config.label}
    </Badge>
  );
}
