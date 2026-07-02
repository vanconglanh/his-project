"use client";

import { Card, CardContent } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { AlertCircle } from "lucide-react";
import type { StockResponse } from "@/lib/api/pharmacy-warehouse";

interface Props {
  stock: StockResponse;
}

export function LowStockAlertCard({ stock }: Props) {
  return (
    <Card className="border border-orange-300 bg-orange-50">
      <CardContent className="p-3 space-y-2">
        <div className="flex items-start justify-between gap-2">
          <div className="flex-1 min-w-0">
            <p className="font-medium text-sm truncate">{stock.drug_name}</p>
            <p className="text-xs text-muted-foreground font-mono">Lô: {stock.batch_no}</p>
          </div>
          <AlertCircle className="h-4 w-4 shrink-0 mt-0.5 text-orange-600" />
        </div>
        <div className="flex items-center justify-between">
          <span className="text-xs text-orange-700 font-semibold">
            Tồn: {stock.quantity_available}
          </span>
          <Badge className="text-[10px] bg-orange-100 text-orange-800 border-orange-300" variant="outline">
            Tồn thấp
          </Badge>
        </div>
      </CardContent>
    </Card>
  );
}
