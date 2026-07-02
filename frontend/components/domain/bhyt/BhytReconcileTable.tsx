"use client";

import { useState } from "react";
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
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
} from "@/components/ui/dialog";
import { Textarea } from "@/components/ui/textarea";
import { Label } from "@/components/ui/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import {
  useReconcileItems,
  useDisputeReconcileItem,
  useAcceptReconcileItem,
} from "@/lib/hooks/use-bhyt-reconcile";
import type { ReconcileItemResponse, ReconcileStatusFilter } from "@/lib/api/bhyt-reconcile";
import { toast } from "sonner";
import { cn } from "@/lib/utils";

const STATUS_LABELS: Record<string, { label: string; className: string }> = {
  APPROVED: { label: "Được duyệt", className: "bg-green-100 text-green-700" },
  REJECTED: { label: "Từ chối", className: "bg-red-100 text-red-700" },
  ADJUSTED: { label: "Đã điều chỉnh", className: "bg-yellow-100 text-yellow-700" },
  DISPUTED: { label: "Đang khiếu nại", className: "bg-orange-100 text-orange-700" },
  ACCEPTED: { label: "Chấp nhận", className: "bg-blue-100 text-blue-700" },
};

function formatVnd(n: number) {
  return new Intl.NumberFormat("vi-VN").format(n) + " ₫";
}

interface DisputeDialogProps {
  item: ReconcileItemResponse;
  exportId: string;
  open: boolean;
  onOpenChange: (v: boolean) => void;
}

function DisputeDialog({ item, exportId, open, onOpenChange }: DisputeDialogProps) {
  const [reason, setReason] = useState("");
  const dispute = useDisputeReconcileItem(exportId);

  function handleDispute() {
    if (!reason.trim()) return;
    dispute.mutate(
      { itemId: item.id, reason },
      {
        onSuccess: () => {
          toast.success("Đã gửi khiếu nại");
          onOpenChange(false);
          setReason("");
        },
        onError: () => toast.error("Gửi khiếu nại thất bại"),
      }
    );
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>Khiếu nại dòng {item.ma_lien_ket}</DialogTitle>
        </DialogHeader>
        <div className="space-y-3">
          <div className="rounded-md bg-muted p-3 text-sm space-y-1">
            <p>Mã từ chối: <span className="font-medium">{item.rejection_code}</span></p>
            <p>Lý do: {item.rejection_reason}</p>
            <p>Số tiền từ chối: <span className="font-medium text-red-600">{formatVnd(item.rejected_amount)}</span></p>
          </div>
          <div className="space-y-1.5">
            <Label htmlFor="reason">Lý do khiếu nại <span className="text-destructive">*</span></Label>
            <Textarea
              id="reason"
              rows={4}
              placeholder="Mô tả lý do khiếu nại..."
              value={reason}
              onChange={(e) => setReason(e.target.value)}
            />
          </div>
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>Huỷ</Button>
          <Button onClick={handleDispute} disabled={!reason.trim() || dispute.isPending}>
            {dispute.isPending ? "Đang gửi..." : "Gửi khiếu nại"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

interface Props {
  exportId: string;
}

export function BhytReconcileTable({ exportId }: Props) {
  const [statusFilter, setStatusFilter] = useState<ReconcileStatusFilter>("ALL");
  const [disputeItem, setDisputeItem] = useState<ReconcileItemResponse | null>(null);
  const accept = useAcceptReconcileItem(exportId);

  const { data, isLoading } = useReconcileItems(exportId, {
    status_filter: statusFilter,
    page: 1,
    page_size: 50,
  });

  function handleAccept(item: ReconcileItemResponse) {
    accept.mutate(
      { itemId: item.id },
      {
        onSuccess: () => toast.success("Đã chấp nhận kết quả giám định"),
        onError: () => toast.error("Thao tác thất bại"),
      }
    );
  }

  const rows = data?.data ?? [];

  return (
    <div className="space-y-3">
      <div className="flex items-center gap-3">
        <Select
          value={statusFilter}
          onValueChange={(v) => setStatusFilter(v as ReconcileStatusFilter)}
        >
          <SelectTrigger className="w-44 h-8">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="ALL">Tất cả trạng thái</SelectItem>
            <SelectItem value="APPROVED">Được duyệt</SelectItem>
            <SelectItem value="REJECTED">Từ chối</SelectItem>
            <SelectItem value="ADJUSTED">Đã điều chỉnh</SelectItem>
            <SelectItem value="DISPUTED">Đang khiếu nại</SelectItem>
          </SelectContent>
        </Select>
        <span className="text-xs text-muted-foreground">{data?.meta?.total ?? rows.length} dòng</span>
      </div>

      {isLoading ? (
        <div className="space-y-2">
          {Array.from({ length: 5 }).map((_, i) => <Skeleton key={i} className="h-12 w-full" />)}
        </div>
      ) : rows.length === 0 ? (
        <div className="py-12 text-center text-sm text-muted-foreground">Chưa có dữ liệu đối soát</div>
      ) : (
        <div className="rounded-md border overflow-x-auto">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead className="text-xs">Mã liên kết</TableHead>
                <TableHead className="text-xs">Bảng</TableHead>
                <TableHead className="text-xs text-right">Yêu cầu</TableHead>
                <TableHead className="text-xs text-right">Được duyệt</TableHead>
                <TableHead className="text-xs text-right">Từ chối</TableHead>
                <TableHead className="text-xs">Mã TC</TableHead>
                <TableHead className="text-xs">Trạng thái</TableHead>
                <TableHead className="text-xs text-right">Thao tác</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {rows.map((row) => {
                const cfg = STATUS_LABELS[row.status] ?? { label: row.status, className: "" };
                const canAction = row.status === "REJECTED" || row.status === "ADJUSTED";

                return (
                  <TableRow key={row.id}>
                    <TableCell className="text-xs font-mono">{row.ma_lien_ket}</TableCell>
                    <TableCell className="text-xs">Bảng {row.table_no}</TableCell>
                    <TableCell className="text-xs text-right">{formatVnd(row.request_amount)}</TableCell>
                    <TableCell className="text-xs text-right text-green-700">{formatVnd(row.approved_amount)}</TableCell>
                    <TableCell className="text-xs text-right text-red-600">{formatVnd(row.rejected_amount)}</TableCell>
                    <TableCell className="text-xs">{row.rejection_code ?? "-"}</TableCell>
                    <TableCell>
                      <Badge variant="outline" className={cn("text-xs border-0 font-medium", cfg.className)}>
                        {cfg.label}
                      </Badge>
                    </TableCell>
                    <TableCell className="text-right">
                      {canAction && (
                        <div className="flex justify-end gap-1">
                          <Button
                            size="sm"
                            variant="ghost"
                            className="h-7 text-xs"
                            onClick={() => setDisputeItem(row)}
                          >
                            Khiếu nại
                          </Button>
                          <Button
                            size="sm"
                            variant="ghost"
                            className="h-7 text-xs text-green-700"
                            onClick={() => handleAccept(row)}
                            disabled={accept.isPending}
                          >
                            Chấp nhận
                          </Button>
                        </div>
                      )}
                    </TableCell>
                  </TableRow>
                );
              })}
            </TableBody>
          </Table>
        </div>
      )}

      {disputeItem && (
        <DisputeDialog
          item={disputeItem}
          exportId={exportId}
          open={!!disputeItem}
          onOpenChange={(v) => !v && setDisputeItem(null)}
        />
      )}
    </div>
  );
}
