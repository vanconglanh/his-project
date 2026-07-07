"use client";

import { Fragment } from "react";
import { ChevronLeft, ChevronRight } from "lucide-react";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Skeleton } from "@/components/ui/skeleton";
import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/ui/EmptyState";
import { cn } from "@/lib/utils";
import type { ReportColumn, ReportColumnAlign, ReportEngineMeta, ReportGroupData, ReportRow } from "@/lib/api/reports";
import { formatNumberVi, formatReportCell } from "./report-format";

const PAGE_SIZE = 100;

interface ReportGridProps {
  columns: ReportColumn[];
  groups: ReportGroupData[] | null;
  rows: ReportRow[] | null;
  totals: Record<string, number>;
  meta: ReportEngineMeta;
  page: number;
  onPageChange: (page: number) => void;
  isLoading: boolean;
}

function alignClass(align: ReportColumnAlign): string {
  if (align === "Right") return "text-right";
  if (align === "Center") return "text-center";
  return "text-left";
}

export function ReportGrid({ columns, groups, rows, totals, meta, page, onPageChange, isLoading }: ReportGridProps) {
  if (isLoading) {
    return <ReportGridSkeleton columnCount={Math.max(columns.length || 6, 6)} />;
  }

  const totalDetailRows = groups ? groups.reduce((sum, g) => sum + g.rows.length, 0) : rows?.length ?? 0;

  if (totalDetailRows === 0) {
    return (
      <EmptyState
        variant="generic"
        title="Không có dữ liệu"
        description="Không có dữ liệu trong khoảng thời gian đã chọn."
      />
    );
  }

  const zebra = totalDetailRows > 10;
  const freezeFirst = columns.length > 8;
  const totalPages = Math.max(1, Math.ceil(meta.total / PAGE_SIZE));

  return (
    <div className="space-y-3">
      <div className="rounded-md border overflow-auto max-h-[65vh]">
        <Table className="min-w-full">
          <TableHeader>
            <TableRow className="hover:bg-transparent">
              {columns.map((col, idx) => (
                <TableHead
                  key={col.key}
                  className={cn(
                    "sticky top-0 z-20 bg-[#01645A] text-white whitespace-nowrap",
                    alignClass(col.align),
                    idx === 0 && freezeFirst && "left-0 z-30"
                  )}
                >
                  {col.label}
                </TableHead>
              ))}
            </TableRow>
          </TableHeader>
          <TableBody>
            {groups
              ? groups.map((group) => (
                  <Fragment key={group.key}>
                    <TableRow className="bg-primary/10 hover:bg-primary/10">
                      <TableCell colSpan={columns.length} className="font-semibold text-primary">
                        {group.label}{" "}
                        <span className="font-normal text-muted-foreground">({group.count} phiếu)</span>
                      </TableCell>
                    </TableRow>
                    {group.rows.map((row, rowIdx) => (
                      <ReportDataRow
                        key={rowIdx}
                        row={row}
                        columns={columns}
                        zebra={zebra}
                        zebraIndex={rowIdx}
                        freezeFirst={freezeFirst}
                      />
                    ))}
                    <TableRow className="border-t-2 hover:bg-transparent">
                      {columns.map((col, idx) => (
                        <TableCell
                          key={col.key}
                          className={cn(
                            "font-semibold",
                            alignClass(col.align),
                            idx === 0 && freezeFirst && "sticky left-0 z-10 bg-background"
                          )}
                        >
                          {idx === 0
                            ? `Cộng: ${group.label}`
                            : col.is_group_subtotal && group.subtotals[col.key] !== undefined
                              ? formatReportCell(group.subtotals[col.key], col.type)
                              : ""}
                        </TableCell>
                      ))}
                    </TableRow>
                  </Fragment>
                ))
              : rows?.map((row, rowIdx) => (
                  <ReportDataRow
                    key={rowIdx}
                    row={row}
                    columns={columns}
                    zebra={zebra}
                    zebraIndex={rowIdx}
                    freezeFirst={freezeFirst}
                  />
                ))}

            {/* Dòng TỔNG CỘNG cuối bảng — luôn tính trên toàn bộ tập kết quả (data.totals), không chỉ trang hiện tại */}
            <TableRow className="bg-[#014A42] hover:bg-[#014A42]">
              {columns.map((col, idx) => (
                <TableCell
                  key={col.key}
                  className={cn(
                    "font-bold text-white",
                    alignClass(col.align),
                    idx === 0 && freezeFirst && "sticky left-0 z-10 bg-[#014A42]"
                  )}
                >
                  {idx === 0
                    ? "TỔNG CỘNG"
                    : col.is_group_subtotal && totals[col.key] !== undefined
                      ? formatReportCell(totals[col.key], col.type)
                      : ""}
                </TableCell>
              ))}
            </TableRow>
          </TableBody>
        </Table>
      </div>

      {/* Phân trang chỉ áp dụng cho chế độ phẳng (groups == null) — chế độ group trả toàn bộ dữ liệu 1 lần */}
      {!groups && totalPages > 1 && (
        <div className="flex items-center justify-between px-1">
          <p className="text-sm text-muted-foreground">
            Trang {meta.page}/{totalPages} · {formatNumberVi(meta.total)} dòng
          </p>
          <div className="flex items-center gap-2">
            <Button
              variant="outline"
              size="sm"
              onClick={() => onPageChange(page - 1)}
              disabled={page <= 1}
              aria-label="Trang trước"
            >
              <ChevronLeft className="h-4 w-4" />
            </Button>
            <Button
              variant="outline"
              size="sm"
              onClick={() => onPageChange(page + 1)}
              disabled={page >= totalPages}
              aria-label="Trang sau"
            >
              <ChevronRight className="h-4 w-4" />
            </Button>
          </div>
        </div>
      )}
    </div>
  );
}

interface ReportDataRowProps {
  row: ReportRow;
  columns: ReportColumn[];
  zebra: boolean;
  zebraIndex: number;
  freezeFirst: boolean;
}

function ReportDataRow({ row, columns, zebra, zebraIndex, freezeFirst }: ReportDataRowProps) {
  const isZebraRow = zebra && zebraIndex % 2 === 1;
  return (
    <TableRow className={cn(isZebraRow && "bg-[#F3F8F7] hover:bg-[#F3F8F7]")}>
      {columns.map((col, idx) => (
        <TableCell
          key={col.key}
          className={cn(
            alignClass(col.align),
            idx === 0 && freezeFirst && "sticky left-0 z-10",
            idx === 0 && freezeFirst && (isZebraRow ? "bg-[#F3F8F7]" : "bg-background")
          )}
        >
          {formatReportCell(row[col.key], col.type)}
        </TableCell>
      ))}
    </TableRow>
  );
}

function ReportGridSkeleton({ columnCount }: { columnCount: number }) {
  return (
    <div className="rounded-md border overflow-hidden">
      <Table>
        <TableHeader>
          <TableRow>
            {Array.from({ length: columnCount }).map((_, i) => (
              <TableHead key={i}>
                <Skeleton className="h-4 w-20" />
              </TableHead>
            ))}
          </TableRow>
        </TableHeader>
        <TableBody>
          {Array.from({ length: 8 }).map((_, r) => (
            <TableRow key={r}>
              {Array.from({ length: columnCount }).map((_, c) => (
                <TableCell key={c}>
                  <Skeleton className="h-5 w-full" />
                </TableCell>
              ))}
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </div>
  );
}
