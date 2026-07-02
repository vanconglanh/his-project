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
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { Printer } from "lucide-react";
import { useClosingHistory } from "@/lib/hooks/use-cashier";
import { printShiftPdf } from "@/lib/api/cashier";
import { formatCurrency } from "@/lib/utils/format";
import { format } from "date-fns";
import type { CashierClosingResponse } from "@/lib/api/cashier";
import { cn } from "@/lib/utils";

interface Props {
  currentShift?: CashierClosingResponse;
}

export function ShiftHistoryTab({ currentShift }: Props) {
  const { data, isLoading } = useClosingHistory({ page_size: 20 });
  const rows = data?.data ?? [];

  return (
    <div className="rounded-lg border">
      <Table>
        <TableHeader>
          <TableRow>
            <TableHead>Ngày</TableHead>
            <TableHead>Thu ngân</TableHead>
            <TableHead>Giờ mở</TableHead>
            <TableHead>Giờ đóng</TableHead>
            <TableHead className="text-right">Thu ròng</TableHead>
            <TableHead>GD</TableHead>
            <TableHead>Chênh lệch</TableHead>
            <TableHead>Trạng thái</TableHead>
            <TableHead className="w-10" />
          </TableRow>
        </TableHeader>
        <TableBody>
          {isLoading
            ? Array.from({ length: 5 }).map((_, i) => (
                <TableRow key={i}>
                  {Array.from({ length: 9 }).map((_, j) => (
                    <TableCell key={j}><Skeleton className="h-5 w-full" /></TableCell>
                  ))}
                </TableRow>
              ))
            : rows.length === 0
            ? (
              <TableRow>
                <TableCell colSpan={9} className="h-32 text-center text-muted-foreground">
                  Chưa có ca làm việc
                </TableCell>
              </TableRow>
            )
            : rows.map((shift) => {
              const diff = shift.difference ?? 0;
              return (
                <TableRow key={shift.id}>
                  <TableCell>{format(new Date(shift.shift_date), "dd/MM/yyyy")}</TableCell>
                  <TableCell className="text-sm">{shift.cashier_name}</TableCell>
                  <TableCell className="text-xs">{format(new Date(shift.shift_start), "HH:mm")}</TableCell>
                  <TableCell className="text-xs">
                    {shift.shift_end ? format(new Date(shift.shift_end), "HH:mm") : "—"}
                  </TableCell>
                  <TableCell className="text-right font-semibold">
                    {formatCurrency(shift.summary?.net_collected ?? 0)}
                  </TableCell>
                  <TableCell>{shift.summary?.count_transactions ?? 0}</TableCell>
                  <TableCell>
                    {shift.difference != null ? (
                      <span className={cn("font-medium text-sm", diff >= 0 ? "text-green-600" : "text-destructive")}>
                        {diff >= 0 ? "+" : ""}{formatCurrency(diff)}
                      </span>
                    ) : "—"}
                  </TableCell>
                  <TableCell>
                    <Badge
                      variant="outline"
                      className={cn(
                        shift.status === "OPEN"
                          ? "border-green-300 text-green-700"
                          : "border-gray-200 text-gray-600"
                      )}
                    >
                      {shift.status === "OPEN" ? "Đang mở" : "Đã đóng"}
                    </Badge>
                  </TableCell>
                  <TableCell>
                    {shift.status === "CLOSED" && (
                      <Button variant="ghost" size="icon" className="h-8 w-8" onClick={() => printShiftPdf(shift.id)}>
                        <Printer className="h-4 w-4" />
                      </Button>
                    )}
                  </TableCell>
                </TableRow>
              );
            })}
        </TableBody>
      </Table>
    </div>
  );
}
