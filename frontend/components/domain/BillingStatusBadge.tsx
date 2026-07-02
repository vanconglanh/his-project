import { Badge } from "@/components/ui/badge";
import { cn } from "@/lib/utils";
import type { BillingStatus } from "@/lib/api/billing";

const CONFIG: Record<BillingStatus, { label: string; className: string }> = {
  DRAFT: { label: "Nháp", className: "bg-gray-100 text-gray-700 border-gray-200" },
  FINALIZED: { label: "Đã xác nhận", className: "bg-blue-100 text-blue-700 border-blue-200" },
  PARTIAL_PAID: { label: "Thanh toán một phần", className: "bg-amber-100 text-amber-700 border-amber-200" },
  PAID: { label: "Đã thanh toán", className: "bg-green-100 text-green-700 border-green-200" },
  VOID: { label: "Đã huỷ", className: "bg-red-100 text-red-700 border-red-200" },
};

export function BillingStatusBadge({ status }: { status: BillingStatus }) {
  const config = CONFIG[status] ?? { label: status, className: "" };
  return (
    <Badge variant="outline" className={cn("text-xs font-medium", config.className)}>
      {config.label}
    </Badge>
  );
}
