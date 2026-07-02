"use client";

import { useState } from "react";
import { useNearExpiryAlerts, useLowStockAlerts } from "@/lib/hooks/use-pharmacy-warehouse";
import { ExpiryAlertCard } from "@/components/domain/ExpiryAlertCard";
import { LowStockAlertCard } from "@/components/domain/LowStockAlertCard";
import { Skeleton } from "@/components/ui/skeleton";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Label } from "@/components/ui/label";

export function AlertsTab() {
  const [days, setDays] = useState<30 | 60 | 90>(60);

  const { data: expiryAlerts, isLoading: loadingExpiry } = useNearExpiryAlerts(days);
  const { data: lowStockAlerts, isLoading: loadingLow } = useLowStockAlerts();

  return (
    <div className="space-y-8">
      {/* Near expiry */}
      <div className="space-y-4">
        <div className="flex items-center justify-between">
          <h3 className="font-semibold text-base">Sắp hết hạn sử dụng</h3>
          <div className="flex items-center gap-2">
            <Label className="text-sm">Trong vòng</Label>
            <Select
              value={String(days)}
              onValueChange={(v) => setDays(Number(v) as 30 | 60 | 90)}
            >
              <SelectTrigger className="w-28">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="30">30 ngày</SelectItem>
                <SelectItem value="60">60 ngày</SelectItem>
                <SelectItem value="90">90 ngày</SelectItem>
              </SelectContent>
            </Select>
          </div>
        </div>

        {loadingExpiry ? (
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-3">
            {Array.from({ length: 4 }).map((_, i) => <Skeleton key={i} className="h-28" />)}
          </div>
        ) : !expiryAlerts || expiryAlerts.length === 0 ? (
          <p className="text-sm text-muted-foreground">Không có lô thuốc sắp hết hạn</p>
        ) : (
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-3">
            {expiryAlerts.map((s) => (
              <ExpiryAlertCard key={s.id} stock={s} />
            ))}
          </div>
        )}
      </div>

      {/* Low stock */}
      <div className="space-y-4">
        <h3 className="font-semibold text-base">Tồn kho thấp</h3>

        {loadingLow ? (
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-3">
            {Array.from({ length: 4 }).map((_, i) => <Skeleton key={i} className="h-24" />)}
          </div>
        ) : !lowStockAlerts || lowStockAlerts.length === 0 ? (
          <p className="text-sm text-muted-foreground">Không có tồn kho thấp</p>
        ) : (
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-3">
            {lowStockAlerts.map((s) => (
              <LowStockAlertCard key={s.id} stock={s} />
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
