"use client";

import { Badge } from "@/components/ui/badge";
import type { BhytExportStatus } from "@/lib/api/bhyt-export";
import { cn } from "@/lib/utils";

const STATUS_CONFIG: Record<BhytExportStatus, { label: string; className: string }> = {
  DRAFT: { label: "Nháp", className: "bg-slate-100 text-slate-700" },
  GENERATED: { label: "Đã sinh XML", className: "bg-blue-100 text-blue-700" },
  VALIDATED: { label: "Đã validate", className: "bg-cyan-100 text-cyan-700" },
  SIGNED: { label: "Đã ký số", className: "bg-violet-100 text-violet-700" },
  SUBMITTED: { label: "Đã gửi", className: "bg-orange-100 text-orange-700" },
  APPROVED: { label: "Được duyệt", className: "bg-green-100 text-green-700" },
  PARTIALLY_REJECTED: { label: "Từ chối 1 phần", className: "bg-yellow-100 text-yellow-700" },
  REJECTED: { label: "Bị từ chối", className: "bg-red-100 text-red-700" },
};

interface Props {
  status: BhytExportStatus;
  className?: string;
}

export function BhytExportStatusBadge({ status, className }: Props) {
  const cfg = STATUS_CONFIG[status] ?? { label: status, className: "" };
  return (
    <Badge variant="outline" className={cn("font-medium border-0", cfg.className, className)}>
      {cfg.label}
    </Badge>
  );
}
