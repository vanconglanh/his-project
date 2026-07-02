"use client";

import { useState } from "react";
import { Plus } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { Badge } from "@/components/ui/badge";
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { AdjustmentForm } from "@/components/domain/AdjustmentForm";
import { useMovements } from "@/lib/hooks/use-pharmacy-warehouse";

const REASON_LABELS: Record<string, string> = {
  STOCKTAKE: "Kiểm kê",
  DAMAGED: "Hư hỏng",
  EXPIRED: "Hết hạn",
  LOST: "Thất thoát",
  OTHER: "Khác",
};

function formatDate(s: string) {
  return new Date(s).toLocaleDateString("vi-VN", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  });
}

interface AdjustmentTabProps {
  externalOpen?: boolean;
  onExternalOpenChange?: (open: boolean) => void;
}

export function AdjustmentTab({ externalOpen, onExternalOpenChange }: AdjustmentTabProps = {}) {
  const [internalOpen, setInternalOpen] = useState(false);

  // If parent controls the dialog, use parent state; otherwise use local state
  const createOpen = externalOpen !== undefined ? externalOpen : internalOpen;
  const setCreateOpen = onExternalOpenChange !== undefined ? onExternalOpenChange : setInternalOpen;

  const { data, isLoading } = useMovements({ movement_type: "ADJUSTMENT", page_size: 50 });
  const rows = data?.data ?? [];

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <p className="text-sm text-muted-foreground">
            Lịch sử điều chỉnh tồn kho (kiểm kê, hư hỏng, hết hạn...)
          </p>
        </div>
        <Button onClick={() => setCreateOpen(true)} className="min-h-[44px]">
          <Plus className="h-4 w-4 mr-2" />
          Tạo điều chỉnh
        </Button>
      </div>

      {/* List */}
      <Card>
        <CardHeader>
          <CardTitle className="text-base">Danh sách điều chỉnh</CardTitle>
        </CardHeader>
        <CardContent className="p-0">
          {isLoading ? (
            <div className="p-4 space-y-2">
              {Array.from({ length: 5 }).map((_, i) => (
                <Skeleton key={i} className="h-12 w-full" />
              ))}
            </div>
          ) : rows.length === 0 ? (
            <div className="flex flex-col items-center justify-center py-16 gap-3">
              <div className="h-16 w-16 rounded-full bg-muted flex items-center justify-center">
                <Plus className="h-7 w-7 text-muted-foreground" />
              </div>
              <p className="text-sm font-medium">Chưa có điều chỉnh nào</p>
              <p className="text-xs text-muted-foreground">
                Tạo điều chỉnh để ghi nhận kiểm kê hoặc hao hụt tồn kho
              </p>
              <Button size="sm" onClick={() => setCreateOpen(true)}>
                Tạo điều chỉnh đầu tiên
              </Button>
            </div>
          ) : (
            <div className="overflow-x-auto">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Thời gian</TableHead>
                    <TableHead>Thuốc</TableHead>
                    <TableHead>Lô</TableHead>
                    <TableHead>Lý do</TableHead>
                    <TableHead className="text-right">Chênh lệch</TableHead>
                    <TableHead>Người thực hiện</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {rows.map((row) => (
                    <TableRow key={row.id}>
                      <TableCell className="text-sm text-muted-foreground whitespace-nowrap">
                        {formatDate(row.movement_at)}
                      </TableCell>
                      <TableCell className="font-medium">{row.drug_name ?? row.drug_id}</TableCell>
                      <TableCell className="font-mono text-sm">{row.batch_no ?? "—"}</TableCell>
                      <TableCell>
                        <Badge variant="outline">
                          {REASON_LABELS[row.reason ?? "OTHER"] ?? row.reason ?? "—"}
                        </Badge>
                      </TableCell>
                      <TableCell className="text-right">
                        <span
                          className={
                            (row.quantity ?? 0) >= 0
                              ? "text-green-600 font-medium"
                              : "text-red-600 font-medium"
                          }
                        >
                          {(row.quantity ?? 0) >= 0 ? "+" : ""}
                          {row.quantity ?? 0}
                        </span>
                      </TableCell>
                      <TableCell className="text-sm text-muted-foreground">
                        {row.performed_by ?? "—"}
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </div>
          )}
        </CardContent>
      </Card>

      {/* Create dialog */}
      <Dialog open={createOpen} onOpenChange={setCreateOpen}>
        <DialogContent className="max-w-2xl max-h-[90vh] overflow-y-auto">
          <DialogHeader>
            <DialogTitle>Tạo điều chỉnh tồn kho</DialogTitle>
          </DialogHeader>
          <AdjustmentForm onSuccess={() => setCreateOpen(false)} />
        </DialogContent>
      </Dialog>
    </div>
  );
}
