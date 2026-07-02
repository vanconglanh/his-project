import { Badge } from "@/components/ui/badge";
import type { EncounterStatus } from "@/lib/api/types";
import { cn } from "@/lib/utils";

interface Props {
  status: EncounterStatus;
  className?: string;
}

const STATUS_MAP: Record<EncounterStatus, { label: string; className: string }> = {
  WAITING: { label: "Chờ khám", className: "bg-yellow-100 text-yellow-800 border-yellow-200 dark:bg-yellow-900/20 dark:text-yellow-400" },
  IN_PROGRESS: { label: "Đang khám", className: "bg-blue-100 text-blue-800 border-blue-200 dark:bg-blue-900/20 dark:text-blue-400" },
  DONE: { label: "Hoàn thành", className: "bg-green-100 text-green-800 border-green-200 dark:bg-green-900/20 dark:text-green-400" },
  CANCELLED: { label: "Đã hủy", className: "bg-gray-100 text-gray-600 border-gray-200 dark:bg-gray-800 dark:text-gray-400" },
};

export function EncounterStatusBadge({ status, className }: Props) {
  const cfg = STATUS_MAP[status] ?? STATUS_MAP.WAITING;
  return (
    <Badge
      variant="outline"
      className={cn("text-xs font-medium", cfg.className, className)}
    >
      {cfg.label}
    </Badge>
  );
}
