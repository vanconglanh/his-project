"use client";

import { Card, CardContent } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { AlertTriangle, ShoppingCart } from "lucide-react";
import { format, parseISO } from "date-fns";
import { vi } from "date-fns/locale";
import type { StockResponse } from "@/lib/api/pharmacy-warehouse";
import { cn } from "@/lib/utils";

interface Props {
  stock: StockResponse;
  onCreatePo?: (stock: StockResponse) => void;
}

export function ExpiryAlertCard({ stock, onCreatePo }: Props) {
  // BM-59: lo da qua han (is_expired) phai hien "Het han" (do dam), khong phai "Sap het han".
  const isExpired = stock.is_expired;
  const isUrgent = !isExpired && stock.days_to_expiry <= 30;

  return (
    <Card
      className={cn(
        "border",
        isExpired
          ? "border-red-500 bg-red-100"
          : isUrgent
            ? "border-red-300 bg-red-50"
            : "border-yellow-300 bg-yellow-50"
      )}
    >
      <CardContent className="p-3 space-y-2">
        <div className="flex items-start justify-between gap-2">
          <div className="flex-1 min-w-0">
            <p className="font-medium text-sm truncate">{stock.drug_name}</p>
            <p className="text-xs text-muted-foreground font-mono">Lô: {stock.batch_no}</p>
          </div>
          <AlertTriangle
            className={cn(
              "h-4 w-4 shrink-0 mt-0.5",
              isExpired ? "text-red-700" : isUrgent ? "text-red-600" : "text-yellow-600"
            )}
          />
        </div>
        <div className="flex items-center justify-between text-xs">
          <span
            className={cn(
              "font-semibold",
              isExpired ? "text-red-800" : isUrgent ? "text-red-700" : "text-yellow-700"
            )}
          >
            HSD: {stock.expiry_date ? format(parseISO(stock.expiry_date), "dd/MM/yyyy", { locale: vi }) : "—"}
          </span>
          <Badge
            className={cn(
              "text-xs",
              isExpired
                ? "bg-red-200 text-red-900 border-red-500"
                : isUrgent
                  ? "bg-red-100 text-red-800 border-red-300"
                  : "bg-yellow-100 text-yellow-800 border-yellow-300"
            )}
            variant="outline"
          >
            {isExpired ? `Hết hạn ${Math.abs(stock.days_to_expiry)} ngày trước` : `còn ${stock.days_to_expiry} ngày`}
          </Badge>
        </div>
        <p className="text-xs text-muted-foreground">Tồn: {stock.quantity_available}</p>
      </CardContent>
    </Card>
  );
}
