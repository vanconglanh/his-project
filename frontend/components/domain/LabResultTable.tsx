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
  /**
   * Khi true, khung bảng tự co giãn chiếm hết chiều cao còn lại của parent
   * (flex-1 + cuộn nội bộ, header dính) để trang không phải cuộn dọc.
   * Parent phải là flex-col có chiều cao xác định.
   */
  fillHeight?: boolean;
}

export function LabResultTable({
  data,
  loading,
  onEnterResult,
  onVerify,
  onUnverify,
  onPrint,
  onViewDetail,
  fillHeight,
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
    <div
      className={cn(
        "rounded-md border overflow-auto",
        fillHeight && "flex-1 min-h-0"
      )}
    >
      <Table>
        <TableHeader className="sticky top-0 z-10 bg-background shadow-[inset_0_-1px_0_var(--border)]">
          <TableRow>
            <TableHead className="py-2">Bệnh nhân</TableHead>
            <TableHead className="py-2">Chỉ số</TableHead>
            <TableHead className="py-2">Giá trị</TableHead>
            <TableHead className="py-2">Đơn vị</TableHead>
            <TableHead className="py-2">KTTC</TableHead>
            <TableHead className="py-2">Cờ</TableHead>
            <TableHead className="py-2">Trạng thái</TableHead>
            <TableHead className="py-2 whitespace-nowrap">Thực hiện</TableHead>
            <TableHead className="py-2 text-right">Thao tác</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {data.map((row) => {
            const isAbnormal = row.flag !== "NORMAL";
            const canOpen = Boolean(onViewDetail);
            return (
              <TableRow
                key={row.id}
                className={cn(isAbnormal && "bg-red-50/50 dark:bg-red-900/10")}
              >
                <TableCell className="py-1.5 max-w-[180px]">
                  <div className="font-medium truncate leading-tight" title={row.patient_name ?? undefined}>
                    {row.patient_name ?? "(chưa rõ tên)"}
                  </div>
                  {row.patient_code && (
                    <div className="text-xs text-muted-foreground leading-tight">{row.patient_code}</div>
                  )}
                </TableCell>
                <TableCell className="py-1.5 font-medium max-w-[200px]">
                  <div className="truncate leading-tight" title={row.test_name}>{row.test_name}</div>
                  <div className="text-xs font-normal text-muted-foreground leading-tight">{row.test_code}</div>
                </TableCell>
                <TableCell
                  className={cn(
                    "py-1.5 font-mono",
                    isAbnormal && "font-bold text-red-600 dark:text-red-400"
                  )}
                >
                  {/*
                    Cot "Gia tri" co the chua chuoi dai (vd ket qua kham chuyen gia migrate
                    tu diab_his) -> gioi han be rong + truncate + tooltip, click mo chi tiet,
                    tranh keo rong bang gay scroll ngang.
                  */}
                  <span
                    className={cn(
                      "block max-w-[220px] truncate",
                      canOpen && "cursor-pointer hover:underline"
                    )}
                    title={row.value}
                    onClick={canOpen ? () => onViewDetail?.(row) : undefined}
                  >
                    {row.value}
                  </span>
                </TableCell>
                <TableCell className="py-1.5 text-muted-foreground">{row.unit}</TableCell>
                <TableCell className="py-1.5 text-xs text-muted-foreground whitespace-nowrap">
                  {row.reference_range_low != null && row.reference_range_high != null
                    ? `${row.reference_range_low} – ${row.reference_range_high}`
                    : "—"}
                </TableCell>
                <TableCell className="py-1.5">
                  <FlagBadge flag={row.flag} />
                </TableCell>
                <TableCell className="py-1.5">
                  <Badge variant={STATUS_VARIANT[row.status] ?? "secondary"}>
                    {STATUS_LABELS[row.status] ?? row.status}
                  </Badge>
                </TableCell>
                <TableCell className="py-1.5 text-xs text-muted-foreground whitespace-nowrap">
                  {format(new Date(row.performed_at), "dd/MM/yyyy HH:mm", { locale: vi })}
                </TableCell>
                <TableCell className="py-1.5 text-right">
                  <div className="flex gap-1 justify-end flex-nowrap">
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
