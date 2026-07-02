"use client";

import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { Badge } from "@/components/ui/badge";
import { cn } from "@/lib/utils";
import type { StockResponse } from "@/lib/api/pharmacy-warehouse";
import { format, parseISO } from "date-fns";
import { vi } from "date-fns/locale";

interface Props {
  stocks: StockResponse[];
}

export function StockTable({ stocks }: Props) {
  if (stocks.length === 0) {
    return (
      <div className="flex flex-col items-center py-12 text-muted-foreground text-sm gap-2">
        <p>Không có dữ liệu tồn kho</p>
      </div>
    );
  }

  function expiryClass(days: number): string {
    if (days <= 30) return "text-red-700 font-semibold bg-red-50";
    if (days <= 90) return "text-yellow-700 bg-yellow-50";
    return "";
  }

  return (
    <div className="rounded-md border overflow-x-auto">
      <Table>
        <TableHeader>
          <TableRow>
            <TableHead>Tên thuốc</TableHead>
            <TableHead>Số lô</TableHead>
            <TableHead>HSD</TableHead>
            <TableHead className="text-center">Còn lại (ngày)</TableHead>
            <TableHead className="text-right">Tồn sẵn</TableHead>
            <TableHead className="text-right">Đặt trước</TableHead>
            <TableHead className="text-right">Đơn giá</TableHead>
            <TableHead>Trạng thái</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {stocks.map((s) => (
            <TableRow key={s.id}>
              <TableCell className="font-medium">{s.drug_name}</TableCell>
              <TableCell className="font-mono text-sm">{s.batch_no}</TableCell>
              <TableCell
                className={cn("text-sm", expiryClass(s.days_to_expiry))}
              >
                {s.expiry_date ? format(parseISO(s.expiry_date), "dd/MM/yyyy", { locale: vi }) : "—"}
              </TableCell>
              <TableCell
                className={cn("text-center text-sm", expiryClass(s.days_to_expiry))}
              >
                {s.days_to_expiry} ngày
              </TableCell>
              <TableCell className="text-right">{s.quantity_available}</TableCell>
              <TableCell className="text-right text-muted-foreground">{s.quantity_reserved}</TableCell>
              <TableCell className="text-right text-sm">
                {(s.unit_cost ?? 0).toLocaleString("vi-VN")}đ
              </TableCell>
              <TableCell>
                <div className="flex gap-1 flex-wrap">
                  {s.is_low_stock && (
                    <Badge variant="destructive" className="text-[10px]">Tồn thấp</Badge>
                  )}
                  {s.is_near_expiry && (
                    <Badge
                      className={cn(
                        "text-[10px]",
                        s.days_to_expiry <= 30
                          ? "bg-red-100 text-red-800 border-red-300"
                          : "bg-yellow-100 text-yellow-800 border-yellow-300"
                      )}
                      variant="outline"
                    >
                      {s.days_to_expiry <= 30 ? "Sắp hết hạn" : "Gần hết hạn"}
                    </Badge>
                  )}
                </div>
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </div>
  );
}
