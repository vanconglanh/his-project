"use client";

import { Card, CardContent } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { User, Clock, ShieldCheck, Package, Stethoscope } from "lucide-react";
import { format, parseISO } from "date-fns";
import { vi } from "date-fns/locale";
import type { DispenseQueueItem } from "@/lib/api/pharmacy-dispensing";

interface Props {
  item: DispenseQueueItem;
  onDispense: (item: DispenseQueueItem) => void;
  onReject: (item: DispenseQueueItem) => void;
  /** BM-82: bam vao the mo chi tiet don o che do xem. */
  onView?: (item: DispenseQueueItem) => void;
}

export function DispenseQueueCard({ item, onDispense, onReject, onView }: Props) {
  return (
    <Card
      className="hover:shadow-md transition-shadow cursor-pointer"
      role={onView ? "button" : undefined}
      tabIndex={onView ? 0 : undefined}
      aria-label={onView ? `Xem chi tiết đơn của ${item.patient_name}` : undefined}
      onClick={() => onView?.(item)}
      onKeyDown={(e) => {
        if (onView && (e.key === "Enter" || e.key === " ")) {
          e.preventDefault();
          onView(item);
        }
      }}
    >
      <CardContent className="p-4 space-y-3">
        <div className="flex items-start justify-between">
          <div>
            <p className="font-semibold">{item.patient_name}</p>
            {/* BM-82: hiển thị mã bệnh nhân */}
            {item.patient_code && (
              <p className="text-xs text-muted-foreground font-mono">{item.patient_code}</p>
            )}
            <p className="text-sm text-muted-foreground flex items-center gap-1">
              <Stethoscope className="h-3 w-3" />
              {item.doctor_name}
            </p>
          </div>
          <div className="flex gap-1">
            {item.is_bhyt && (
              <Badge variant="outline" className="text-xs flex items-center gap-1">
                <ShieldCheck className="h-3 w-3" />
                BHYT
              </Badge>
            )}
          </div>
        </div>

        <div className="flex items-center gap-4 text-sm text-muted-foreground">
          <span className="flex items-center gap-1">
            <Package className="h-3.5 w-3.5" />
            {item.items_count} thuốc
          </span>
          {/* BM-82: hiển thị đầy đủ ngày giờ kê đơn */}
          <span className="flex items-center gap-1">
            <Clock className="h-3.5 w-3.5" />
            {item.signed_at
              ? format(parseISO(item.signed_at), "dd/MM HH:mm", { locale: vi })
              : "—"}
          </span>
          <span className="font-medium text-foreground">
            {(item.total_amount ?? 0).toLocaleString("vi-VN")}đ
          </span>
        </div>

        <div className="flex gap-2">
          <Button
            size="sm"
            className="flex-1"
            onClick={(e) => { e.stopPropagation(); onDispense(item); }}
          >
            Phát thuốc
          </Button>
          <Button
            size="sm"
            variant="ghost"
            className="text-destructive hover:text-destructive"
            onClick={(e) => { e.stopPropagation(); onReject(item); }}
          >
            Từ chối
          </Button>
        </div>
      </CardContent>
    </Card>
  );
}
