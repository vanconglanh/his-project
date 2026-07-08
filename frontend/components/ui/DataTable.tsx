"use client";

import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { Skeleton } from "@/components/ui/skeleton";
import { Button } from "@/components/ui/button";
import { ChevronLeft, ChevronRight } from "lucide-react";
import type { ApiMeta } from "@/lib/api/types";

export interface Column<T> {
  key: string;
  header: string;
  cell: (row: T) => React.ReactNode;
  className?: string;
}

interface DataTableProps<T> {
  columns: Column<T>[];
  data: T[];
  isLoading?: boolean;
  meta?: ApiMeta;
  onPageChange?: (page: number) => void;
  onRowClick?: (row: T) => void;
  /** Thao tác nhanh: double-click vào hàng để chạy action dùng nhiều nhất. */
  onRowDoubleClick?: (row: T) => void;
  emptyState?: React.ReactNode;
  skeletonRows?: number;
}

export function DataTable<T>({
  columns,
  data,
  isLoading,
  meta,
  onPageChange,
  onRowClick,
  onRowDoubleClick,
  emptyState,
  skeletonRows = 5,
}: DataTableProps<T>) {
  return (
    <div className="space-y-4">
      <div className="rounded-md border overflow-hidden overflow-x-auto w-full">
        <Table className="min-w-full">
          <TableHeader>
            <TableRow>
              {columns.map((col) => (
                <TableHead key={col.key} className={col.className}>
                  {col.header}
                </TableHead>
              ))}
            </TableRow>
          </TableHeader>
          <TableBody>
            {isLoading ? (
              Array.from({ length: skeletonRows }).map((_, i) => (
                <TableRow key={i}>
                  {columns.map((col) => (
                    <TableCell key={col.key}>
                      <Skeleton className="h-5 w-full" />
                    </TableCell>
                  ))}
                </TableRow>
              ))
            ) : data.length === 0 ? (
              <TableRow>
                <TableCell colSpan={columns.length} className="h-48 text-center">
                  {emptyState ?? (
                    <div className="flex flex-col items-center gap-2 text-muted-foreground">
                      <p className="text-sm">Không có dữ liệu</p>
                    </div>
                  )}
                </TableCell>
              </TableRow>
            ) : (
              data.map((row, i) => (
                <TableRow
                  key={i}
                  className={
                    onRowClick || onRowDoubleClick
                      ? "cursor-pointer hover:bg-muted/50"
                      : ""
                  }
                  onClick={() => onRowClick?.(row)}
                  onDoubleClick={() => onRowDoubleClick?.(row)}
                >
                  {columns.map((col) => (
                    <TableCell key={col.key} className={col.className}>
                      {col.cell(row)}
                    </TableCell>
                  ))}
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </div>

      {meta && meta.total_pages > 1 && onPageChange && (
        <div className="flex items-center justify-between px-2">
          <p className="text-sm text-muted-foreground">
            Trang {meta.page}/{meta.total_pages} &bull; {meta.total} bản ghi
          </p>
          <div className="flex items-center gap-2">
            <Button
              variant="outline"
              size="sm"
              onClick={() => onPageChange(meta.page - 1)}
              disabled={meta.page <= 1}
              aria-label="Trang trước"
            >
              <ChevronLeft className="h-4 w-4" />
            </Button>
            <Button
              variant="outline"
              size="sm"
              onClick={() => onPageChange(meta.page + 1)}
              disabled={meta.page >= meta.total_pages}
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
