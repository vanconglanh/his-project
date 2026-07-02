"use client";

import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { useLabOrders, useRadOrders, useDeleteLabOrder, useDeleteRadOrder } from "@/lib/hooks/use-cls-orders";
import type { LabOrderResponse, RadOrderResponse } from "@/lib/api/types";
import { Skeleton } from "@/components/ui/skeleton";
import { ConfirmDialog } from "./ConfirmDialog";
import { useState } from "react";
import { Trash2 } from "lucide-react";

const LAB_STATUS: Record<string, { label: string; className: string }> = {
  ordered: { label: "Đã chỉ định", className: "bg-yellow-100 text-yellow-800" },
  sample_taken: { label: "Đã lấy mẫu", className: "bg-blue-100 text-blue-800" },
  processing: { label: "Đang xử lý", className: "bg-purple-100 text-purple-800" },
  done: { label: "Có kết quả", className: "bg-green-100 text-green-800" },
  cancelled: { label: "Đã hủy", className: "bg-gray-100 text-gray-600" },
};

const RAD_STATUS: Record<string, { label: string; className: string }> = {
  ordered: { label: "Đã chỉ định", className: "bg-yellow-100 text-yellow-800" },
  scheduled: { label: "Đã lên lịch", className: "bg-blue-100 text-blue-800" },
  in_progress: { label: "Đang thực hiện", className: "bg-purple-100 text-purple-800" },
  done: { label: "Có kết quả", className: "bg-green-100 text-green-800" },
  cancelled: { label: "Đã hủy", className: "bg-gray-100 text-gray-600" },
};

interface Props {
  encounterId: string;
}

export function LabRadOrderList({ encounterId }: Props) {
  const { data: labOrders, isLoading: labLoading } = useLabOrders(encounterId);
  const { data: radOrders, isLoading: radLoading } = useRadOrders(encounterId);
  const deleteLabOrder = useDeleteLabOrder(encounterId);
  const deleteRadOrder = useDeleteRadOrder(encounterId);
  const [pendingDelete, setPendingDelete] = useState<{ id: string; kind: "lab" | "rad" } | null>(null);

  if (labLoading || radLoading) {
    return <div className="space-y-2">{[1, 2].map((i) => <Skeleton key={i} className="h-12 w-full" />)}</div>;
  }

  const allEmpty = (!labOrders || labOrders.length === 0) && (!radOrders || radOrders.length === 0);

  if (allEmpty) {
    return <p className="text-sm text-muted-foreground italic">Chưa có chỉ định CLS</p>;
  }

  return (
    <>
      <div className="space-y-2">
        {labOrders && labOrders.length > 0 && (
          <div>
            <p className="text-xs font-medium text-muted-foreground uppercase tracking-wide mb-1">Xét nghiệm</p>
            {labOrders.map((order) => (
              <OrderRow
                key={order.id}
                code={order.test_code}
                name={order.test_name}
                status={order.status}
                statusMap={LAB_STATUS}
                priority={order.priority}
                onDelete={order.status === "ordered" ? () => setPendingDelete({ id: order.id, kind: "lab" }) : undefined}
              />
            ))}
          </div>
        )}
        {radOrders && radOrders.length > 0 && (
          <div>
            <p className="text-xs font-medium text-muted-foreground uppercase tracking-wide mb-1">CĐHA</p>
            {radOrders.map((order) => (
              <OrderRow
                key={order.id}
                code={order.procedure_code}
                name={order.procedure_name}
                status={order.status}
                statusMap={RAD_STATUS}
                priority={order.priority}
                extra={`${order.modality}${order.contrast ? " (cản quang)" : ""}`}
                onDelete={order.status === "ordered" ? () => setPendingDelete({ id: order.id, kind: "rad" }) : undefined}
              />
            ))}
          </div>
        )}
      </div>

      <ConfirmDialog
        open={!!pendingDelete}
        onOpenChange={(v) => { if (!v) setPendingDelete(null); }}
        title="Hủy chỉ định CLS"
        description="Bạn có chắc muốn hủy chỉ định này?"
        variant="destructive"
        onConfirm={() => {
          if (pendingDelete?.kind === "lab") deleteLabOrder.mutate(pendingDelete.id);
          else if (pendingDelete?.kind === "rad") deleteRadOrder.mutate(pendingDelete.id);
          setPendingDelete(null);
        }}
      />
    </>
  );
}

function OrderRow({
  code,
  name,
  status,
  statusMap,
  priority,
  extra,
  onDelete,
}: {
  code: string;
  name: string;
  status: string;
  statusMap: Record<string, { label: string; className: string }>;
  priority?: string;
  extra?: string;
  onDelete?: () => void;
}) {
  const cfg = statusMap[status];
  return (
    <div className="flex items-center gap-3 p-2.5 rounded-md border bg-card">
      <div className="flex-1 min-w-0">
        <span className="font-mono text-xs text-primary mr-2">{code}</span>
        <span className="text-sm">{name}</span>
        {extra && <span className="text-xs text-muted-foreground ml-2">({extra})</span>}
        {priority && priority !== "NORMAL" && (
          <Badge variant="outline" className="ml-2 text-xs">
            {priority === "URGENT" ? "Khẩn" : "Cấp cứu"}
          </Badge>
        )}
      </div>
      <Badge variant="outline" className={`text-xs ${cfg?.className ?? ""}`}>
        {cfg?.label ?? status}
      </Badge>
      {onDelete && (
        <Button
          variant="ghost"
          size="icon"
          className="h-7 w-7 text-destructive hover:text-destructive"
          onClick={onDelete}
        >
          <Trash2 className="h-3.5 w-3.5" />
        </Button>
      )}
    </div>
  );
}
