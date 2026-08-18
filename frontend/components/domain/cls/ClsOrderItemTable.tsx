"use client";

import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { HisStatusBadge, type HisStatusVariant } from "@/components/ui/status-badge";
import { formatVnd } from "@/lib/utils/encounter-format";

export interface ClsOrderItemRow {
  id: string;
  kind: "LAB" | "RAD";
  code: string;
  name: string;
  status: string;
  unit_price?: number | null;
}

const STATUS_MAP: Record<string, { label: string; variant: HisStatusVariant }> = {
  ordered: { label: "Chờ thực hiện", variant: "waiting" },
  sample_taken: { label: "Đã lấy mẫu", variant: "progress" },
  scheduled: { label: "Đã lên lịch", variant: "progress" },
  processing: { label: "Đang xử lý", variant: "progress" },
  in_progress: { label: "Đang thực hiện", variant: "progress" },
  done: { label: "Có kết quả", variant: "done" },
  cancelled: { label: "Đã huỷ", variant: "critical" },
};

export interface ClsOrderItemTableProps {
  items: ClsOrderItemRow[];
  /** Số hiệu đợt — dùng cho caption màn hình đọc */
  roundLabel: string;
  showPrice?: boolean;
}

export function ClsOrderItemTable({ items, roundLabel, showPrice = true }: ClsOrderItemTableProps) {
  return (
    <div className="overflow-x-auto">
      <Table>
        <caption className="sr-only">Danh sách dịch vụ chỉ định {roundLabel}</caption>
        <TableHeader>
          <TableRow>
            <TableHead className="text-xs">Mã</TableHead>
            <TableHead className="text-xs">Dịch vụ</TableHead>
            <TableHead className="text-xs">Loại</TableHead>
            <TableHead className="text-xs">Trạng thái</TableHead>
            {showPrice && <TableHead className="text-xs text-right">Giá</TableHead>}
          </TableRow>
        </TableHeader>
        <TableBody>
          {items.map((item) => {
            const cfg = STATUS_MAP[item.status?.toLowerCase()] ?? {
              label: item.status,
              variant: "waiting" as HisStatusVariant,
            };
            return (
              <TableRow key={item.id}>
                <TableCell className="text-xs font-mono tabular-nums">{item.code}</TableCell>
                <TableCell className="text-sm">{item.name}</TableCell>
                <TableCell className="text-xs">{item.kind === "LAB" ? "XN" : "CĐHA"}</TableCell>
                <TableCell>
                  <HisStatusBadge variant={cfg.variant}>{cfg.label}</HisStatusBadge>
                </TableCell>
                {showPrice && (
                  <TableCell className="text-right text-sm font-mono tabular-nums">
                    {item.unit_price != null ? `${formatVnd(item.unit_price)} ₫` : "—"}
                  </TableCell>
                )}
              </TableRow>
            );
          })}
        </TableBody>
      </Table>
    </div>
  );
}
