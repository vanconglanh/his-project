"use client";

import { Card, CardContent } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Users, Clock, ShieldCheck, Package } from "lucide-react";
import { format, parseISO } from "date-fns";
import { vi } from "date-fns/locale";
import type { DispenseQueueItem } from "@/lib/api/pharmacy-dispensing";

interface Props {
  item: DispenseQueueItem;
  onDispense: (item: DispenseQueueItem) => void;
  onReject: (item: DispenseQueueItem) => void;
}

export function DispenseQueueCard({ item, onDispense, onReject }: Props) {
  return (
    <Card className="hover:shadow-md transition-shadow">
      <CardContent className="p-4 space-y-3">
        <div className="flex items-start justify-between">
          <div>
            <p className="font-semibold">{item.patient_name}</p>
            <p className="text-sm text-muted-foreground">BS: {item.doctor_name}</p>
          </div>
          <div className="flex gap-1">
            {item.is_bhyt && (
              <Badge variant="outline" className="text-[10px] flex items-center gap-1">
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
          <span className="flex items-center gap-1">
            <Clock className="h-3.5 w-3.5" />
            {item.signed_at ? format(parseISO(item.signed_at), "HH:mm", { locale: vi }) : "—"}
          </span>
          <span className="font-medium text-foreground">
            {(item.total_amount ?? 0).toLocaleString("vi-VN")}đ
          </span>
        </div>

        <div className="flex gap-2">
          <Button
            size="sm"
            className="flex-1"
            onClick={() => onDispense(item)}
          >
            Phát thuốc
          </Button>
          <Button
            size="sm"
            variant="ghost"
            className="text-destructive hover:text-destructive"
            onClick={() => onReject(item)}
          >
            Từ chối
          </Button>
        </div>
      </CardContent>
    </Card>
  );
}
