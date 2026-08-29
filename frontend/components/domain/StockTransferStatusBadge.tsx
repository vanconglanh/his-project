import { Badge } from "@/components/ui/badge";
import { cn } from "@/lib/utils";
import type { StockTransferStatus } from "@/lib/api/stock-transfers";

const CONFIG: Record<StockTransferStatus, { label: string; className: string }> = {
  DRAFT: { label: "Nháp", className: "bg-gray-100 text-gray-700 border-gray-200" },
  PENDING_APPROVAL: { label: "Chờ duyệt", className: "bg-amber-100 text-amber-700 border-amber-200" },
  APPROVED: { label: "Đã duyệt", className: "bg-blue-100 text-blue-700 border-blue-200" },
  REJECTED: { label: "Từ chối", className: "bg-red-100 text-red-700 border-red-200" },
  IN_TRANSIT: { label: "Đang vận chuyển", className: "bg-purple-100 text-purple-700 border-purple-200" },
  RECEIVED: { label: "Đã nhận đủ", className: "bg-green-100 text-green-700 border-green-200" },
  PARTIALLY_RECEIVED: { label: "Nhận thiếu", className: "bg-orange-100 text-orange-700 border-orange-200" },
  CLOSED: { label: "Đã đóng", className: "bg-slate-200 text-slate-700 border-slate-300" },
  CANCELLED: { label: "Đã huỷ", className: "bg-red-100 text-red-700 border-red-200" },
};

export function StockTransferStatusBadge({ status }: { status: StockTransferStatus }) {
  const config = CONFIG[status] ?? { label: status, className: "" };
  return (
    <Badge variant="outline" className={cn("text-xs font-medium", config.className)}>
      {config.label}
    </Badge>
  );
}
