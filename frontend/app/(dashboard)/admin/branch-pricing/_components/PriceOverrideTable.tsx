"use client";

import { DataTable, type Column } from "@/components/ui/DataTable";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Pencil, Trash2 } from "lucide-react";
import type { ApiMeta } from "@/lib/api/types";
import type { PriceOverrideScope } from "@/lib/api/branch-pricing";

export interface PriceOverrideRow {
  id: string;
  itemLabel: string;
  scope: PriceOverrideScope;
  branch_id: number | null;
  group_id: number | null;
  branchLabel: string;
  price: number;
  is_active: boolean;
  effective_from: string;
  effective_to: string | null;
  note: string | null;
}

function formatVnd(n: number): string {
  return new Intl.NumberFormat("vi-VN").format(n) + "đ";
}

function formatDate(s: string | null): string {
  if (!s) return "-";
  const d = new Date(s);
  if (Number.isNaN(d.getTime())) return s;
  return d.toLocaleDateString("vi-VN");
}

interface PriceOverrideTableProps {
  rows: PriceOverrideRow[];
  isLoading?: boolean;
  meta?: ApiMeta;
  onPageChange?: (page: number) => void;
  onEdit: (row: PriceOverrideRow) => void;
  onDelete: (row: PriceOverrideRow) => void;
  emptyLabel: string;
}

export function PriceOverrideTable({
  rows,
  isLoading,
  meta,
  onPageChange,
  onEdit,
  onDelete,
  emptyLabel,
}: PriceOverrideTableProps) {
  const columns: Column<PriceOverrideRow>[] = [
    { key: "item", header: "Tên item", cell: (r) => <span className="font-medium">{r.itemLabel}</span> },
    {
      key: "scope",
      header: "Phạm vi",
      cell: (r) => (
        <Badge variant="outline">{r.scope === "BRANCH" ? "Chi nhánh" : "Nhóm chi nhánh"}</Badge>
      ),
    },
    { key: "branch", header: "Chi nhánh/Nhóm", cell: (r) => r.branchLabel },
    { key: "price", header: "Giá", cell: (r) => <span className="tabular-nums">{formatVnd(r.price)}</span> },
    {
      key: "status",
      header: "Trạng thái hiển thị",
      cell: (r) =>
        r.is_active ? (
          <Badge className="bg-green-100 text-green-800 border-green-300" variant="outline">
            Hiện
          </Badge>
        ) : (
          <Badge className="bg-gray-100 text-gray-600 border-gray-300" variant="outline">
            Ẩn
          </Badge>
        ),
    },
    { key: "from", header: "Hiệu lực từ", cell: (r) => formatDate(r.effective_from) },
    { key: "to", header: "Hiệu lực đến", cell: (r) => formatDate(r.effective_to) },
    {
      key: "note",
      header: "Ghi chú",
      cell: (r) => <span className="text-muted-foreground">{r.note || "-"}</span>,
    },
    {
      key: "actions",
      header: "Thao tác",
      className: "text-right",
      cell: (r) => (
        <div className="flex justify-end gap-1">
          <Button
            variant="ghost"
            size="icon-sm"
            aria-label="Sửa override"
            onClick={() => onEdit(r)}
          >
            <Pencil className="h-4 w-4" />
          </Button>
          <Button
            variant="ghost"
            size="icon-sm"
            aria-label="Xoá override"
            onClick={() => onDelete(r)}
          >
            <Trash2 className="h-4 w-4 text-destructive" />
          </Button>
        </div>
      ),
    },
  ];

  return (
    <DataTable
      columns={columns}
      data={rows}
      isLoading={isLoading}
      meta={meta}
      onPageChange={onPageChange}
      emptyState={<p className="text-sm text-muted-foreground">{emptyLabel}</p>}
    />
  );
}
