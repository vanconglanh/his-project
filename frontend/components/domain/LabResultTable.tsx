"use client";

import { format } from "date-fns";
import { vi } from "date-fns/locale";
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
import { FlagBadge } from "./FlagBadge";
import type { LabResultResponse } from "@/lib/api/lab-results";
import { cn } from "@/lib/utils";

const STATUS_LABELS: Record<string, string> = {
  DRAFT: "Nháp",
  VERIFIED: "Đã xác thực",
  AMENDED: "Đã sửa",
};

const STATUS_VARIANT: Record<string, "default" | "secondary" | "outline"> = {
  DRAFT: "secondary",
  VERIFIED: "default",
  AMENDED: "outline",
};

interface LabResultTableProps {
  data: LabResultResponse[];
  loading?: boolean;
  onEnterResult?: (result: LabResultResponse) => void;
  onVerify?: (result: LabResultResponse) => void;
  onUnverify?: (result: LabResultResponse) => void;
  onPrint?: (result: LabResultResponse) => void;
  onViewDetail?: (result: LabResultResponse) => void;
}

export function LabResultTable({
  data,
  loading,
  onEnterResult,
  onVerify,
  onUnverify,
  onPrint,
  onViewDetail,
}: LabResultTableProps) {
  if (loading) {
    return (
      <div className="space-y-2">
        {Array.from({ length: 5 }).map((_, i) => (
          <Skeleton key={i} className="h-12 w-full rounded-md" />
        ))}
      </div>
    );
  }

  if (!data.length) {
    return (
      <div className="flex flex-col items-center justify-center py-16 text-muted-foreground">
        <span className="text-4xl mb-3">🧪</span>
        <p className="font-medium">Chưa có kết quả xét nghiệm</p>
        <p className="text-sm mt-1">Nhập kết quả để bắt đầu</p>
      </div>
    );
  }

  return (
    <div className="rounded-md border overflow-auto">
      <Table>
        <TableHeader>
          <TableRow>
            <TableHead>Chỉ số</TableHead>
            <TableHead>Giá trị</TableHead>
            <TableHead>Đơn vị</TableHead>
            <TableHead>KTTC</TableHead>
            <TableHead>Cờ</TableHead>
            <TableHead>Trạng thái</TableHead>
            <TableHead>Thực hiện</TableHead>
            <TableHead className="text-right">Thao tác</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {data.map((row) => {
            const isAbnormal = row.flag !== "NORMAL";
            return (
              <TableRow
                key={row.id}
                className={cn(isAbnormal && "bg-red-50/50 dark:bg-red-900/10")}
              >
                <TableCell className="font-medium">
                  <div>{row.test_name}</div>
                  <div className="text-xs text-muted-foreground">{row.test_code}</div>
                </TableCell>
                <TableCell className={cn("font-mono", isAbnormal && "font-bold text-red-600 dark:text-red-400")}>
                  {row.value}
                </TableCell>
                <TableCell className="text-muted-foreground">{row.unit}</TableCell>
                <TableCell className="text-xs text-muted-foreground">
                  {row.reference_range_low != null && row.reference_range_high != null
                    ? `${row.reference_range_low} – ${row.reference_range_high}`
                    : "—"}
                </TableCell>
                <TableCell>
                  <FlagBadge flag={row.flag} />
                </TableCell>
                <TableCell>
                  <Badge variant={STATUS_VARIANT[row.status] ?? "secondary"}>
                    {STATUS_LABELS[row.status] ?? row.status}
                  </Badge>
                </TableCell>
                <TableCell className="text-xs text-muted-foreground">
                  {format(new Date(row.performed_at), "dd/MM/yyyy HH:mm", { locale: vi })}
                </TableCell>
                <TableCell className="text-right">
                  <div className="flex gap-1 justify-end flex-wrap">
                    {onViewDetail && (
                      <Button variant="ghost" size="sm" onClick={() => onViewDetail(row)}>
                        Chi tiết
                      </Button>
                    )}
                    {onEnterResult && row.status === "DRAFT" && (
                      <Button variant="outline" size="sm" onClick={() => onEnterResult(row)}>
                        Nhập KQ
                      </Button>
                    )}
                    {onVerify && row.status === "DRAFT" && (
                      <Button variant="default" size="sm" onClick={() => onVerify(row)}>
                        Xác thực
                      </Button>
                    )}
                    {onUnverify && row.status === "VERIFIED" && (
                      <Button variant="destructive" size="sm" onClick={() => onUnverify(row)}>
                        Hủy XN
                      </Button>
                    )}
                    {onPrint && row.status === "VERIFIED" && (
                      <Button variant="outline" size="sm" onClick={() => onPrint(row)}>
                        In PDF
                      </Button>
                    )}
                  </div>
                </TableCell>
              </TableRow>
            );
          })}
        </TableBody>
      </Table>
    </div>
  );
}
