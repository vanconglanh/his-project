"use client";

import { useState } from "react";
import Link from "next/link";
import { toast } from "sonner";
import { format } from "date-fns";
import { vi } from "date-fns/locale";
import { ChevronLeft, Send, CheckCircle2, XCircle, Truck, PackageCheck, PackageX, Ban, Archive, AlertTriangle } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Separator } from "@/components/ui/separator";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { Input } from "@/components/ui/input";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
  DialogDescription,
} from "@/components/ui/dialog";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { ConfirmDialog } from "@/components/domain/ConfirmDialog";
import { StockTransferStatusBadge } from "@/components/domain/StockTransferStatusBadge";
import {
  useStockTransfer,
  useSubmitStockTransfer,
  useApproveStockTransfer,
  useRejectStockTransfer,
  useShipStockTransfer,
  useReceiveStockTransfer,
  usePartialReceiveStockTransfer,
  useCloseStockTransfer,
  useCancelStockTransfer,
} from "@/lib/hooks/use-stock-transfers";
import { usePermissions } from "@/lib/hooks/use-permissions";
import { STOCK_TRANSFER_APPROVAL_THRESHOLD } from "@/lib/api/stock-transfers";

interface Props {
  id: string;
}

export function StockTransferDetailClient({ id }: Props) {
  const { data: transfer, isLoading } = useStockTransfer(id);
  const { has } = usePermissions();

  const submit = useSubmitStockTransfer();
  const approve = useApproveStockTransfer();
  const reject = useRejectStockTransfer();
  const ship = useShipStockTransfer();
  const receive = useReceiveStockTransfer();
  const partialReceive = usePartialReceiveStockTransfer();
  const close = useCloseStockTransfer();
  const cancel = useCancelStockTransfer();

  const [rejectOpen, setRejectOpen] = useState(false);
  const [rejectReason, setRejectReason] = useState("");
  const [cancelOpen, setCancelOpen] = useState(false);
  const [receiveOpen, setReceiveOpen] = useState(false);
  const [receiveQty, setReceiveQty] = useState<Record<string, number>>({});
  const [overrideExpiry, setOverrideExpiry] = useState(false);

  if (isLoading) {
    return (
      <div className="space-y-4">
        <Skeleton className="h-8 w-64" />
        <Skeleton className="h-48 w-full" />
      </div>
    );
  }

  if (!transfer) {
    return (
      <div className="flex h-64 items-center justify-center text-muted-foreground">
        Không tìm thấy phiếu điều chuyển
      </div>
    );
  }

  const status = transfer.status;

  // ─── State machine — chỉ hiện nút hành động đúng trạng thái + đúng quyền ────
  const canSubmit = status === "DRAFT" && has("stock_transfer.create");
  const canCancel = (status === "DRAFT" || status === "PENDING_APPROVAL") && has("stock_transfer.create");
  const canApproveReject = status === "PENDING_APPROVAL" && has("stock_transfer.approve");
  const canShip = status === "APPROVED" && has("stock_transfer.ship");
  const canReceive = status === "IN_TRANSIT" && has("stock_transfer.receive");
  const canClose = status === "PARTIALLY_RECEIVED" && has("stock_transfer.receive");

  const overThreshold = transfer.total_value > STOCK_TRANSFER_APPROVAL_THRESHOLD;

  function openReceiveDialog() {
    const defaults: Record<string, number> = {};
    transfer!.items.forEach((it) => {
      defaults[it.id] = it.qty_shipped;
    });
    setReceiveQty(defaults);
    setReceiveOpen(true);
  }

  async function handleSubmit() {
    try {
      await submit.mutateAsync(id);
    } catch {
      toast.error("Gửi duyệt thất bại");
    }
  }

  async function handleApprove() {
    try {
      await approve.mutateAsync({ id, body: { override_expiry_guard: overrideExpiry } });
    } catch {
      toast.error("Duyệt phiếu thất bại");
    }
  }

  async function handleReject() {
    if (rejectReason.trim().length < 3) {
      toast.error("Vui lòng nhập lý do từ chối (tối thiểu 3 ký tự)");
      return;
    }
    try {
      await reject.mutateAsync({ id, body: { reason: rejectReason } });
      setRejectOpen(false);
      setRejectReason("");
    } catch {
      toast.error("Từ chối phiếu thất bại");
    }
  }

  async function handleShip() {
    try {
      await ship.mutateAsync(id);
    } catch {
      toast.error("Xuất hàng thất bại");
    }
  }

  async function handleCancel() {
    try {
      await cancel.mutateAsync(id);
      setCancelOpen(false);
    } catch {
      toast.error("Huỷ phiếu thất bại");
    }
  }

  async function handleConfirmReceive() {
    const items = transfer!.items.map((it) => ({
      item_id: it.id,
      qty_received: receiveQty[it.id] ?? 0,
    }));
    const isFull = items.every((it) => {
      const original = transfer!.items.find((i) => i.id === it.item_id);
      return original && it.qty_received === original.qty_shipped;
    });
    try {
      if (isFull) {
        await receive.mutateAsync({ id, body: { items } });
      } else {
        await partialReceive.mutateAsync({ id, body: { items } });
      }
      setReceiveOpen(false);
    } catch {
      toast.error("Xác nhận nhận hàng thất bại");
    }
  }

  async function handleClose() {
    try {
      await close.mutateAsync(id);
    } catch {
      toast.error("Đóng phiếu thất bại");
    }
  }

  return (
    <div className="space-y-6">
      <div className="flex items-start justify-between gap-4">
        <div className="flex items-center gap-3">
          <Link
            href="/pharmacy/stock-transfers"
            className="inline-flex h-8 w-8 items-center justify-center rounded-lg hover:bg-muted"
          >
            <ChevronLeft className="h-5 w-5" />
          </Link>
          <div>
            <div className="flex items-center gap-2">
              <h2 className="text-xl font-bold">{transfer.transfer_no}</h2>
              <StockTransferStatusBadge status={transfer.status} />
            </div>
            <p className="text-sm text-muted-foreground">
              {transfer.from_branch_name ?? `CN #${transfer.from_branch_id}`} → {transfer.to_branch_name ?? `CN #${transfer.to_branch_id}`}
              {" — "}
              {format(new Date(transfer.created_at), "dd/MM/yyyy HH:mm", { locale: vi })}
            </p>
          </div>
        </div>

        <div className="flex flex-wrap items-center gap-2">
          {canSubmit && (
            <Button size="sm" onClick={handleSubmit} disabled={submit.isPending}>
              <Send className="mr-2 h-4 w-4" />
              Gửi duyệt
            </Button>
          )}
          {canApproveReject && (
            <>
              <Button size="sm" onClick={handleApprove} disabled={approve.isPending}>
                <CheckCircle2 className="mr-2 h-4 w-4" />
                Duyệt
              </Button>
              <Button
                variant="outline"
                size="sm"
                className="text-destructive border-destructive hover:bg-destructive/10"
                onClick={() => setRejectOpen(true)}
              >
                <XCircle className="mr-2 h-4 w-4" />
                Từ chối
              </Button>
            </>
          )}
          {canShip && (
            <Button size="sm" onClick={handleShip} disabled={ship.isPending}>
              <Truck className="mr-2 h-4 w-4" />
              Xuất hàng
            </Button>
          )}
          {canReceive && (
            <Button size="sm" onClick={openReceiveDialog}>
              <PackageCheck className="mr-2 h-4 w-4" />
              Nhận hàng
            </Button>
          )}
          {canClose && (
            <Button size="sm" onClick={handleClose} disabled={close.isPending}>
              <Archive className="mr-2 h-4 w-4" />
              Đóng phiếu
            </Button>
          )}
          {canCancel && (
            <Button
              variant="outline"
              size="sm"
              className="text-destructive border-destructive hover:bg-destructive/10"
              onClick={() => setCancelOpen(true)}
            >
              <Ban className="mr-2 h-4 w-4" />
              Huỷ phiếu
            </Button>
          )}
        </div>
      </div>

      {status === "PENDING_APPROVAL" && overThreshold && (
        <div className="flex items-start gap-2 rounded-md border border-amber-300 bg-amber-50 px-4 py-3 text-sm text-amber-800">
          <AlertTriangle className="h-4 w-4 mt-0.5 shrink-0" />
          <p>
            Giá trị phiếu {transfer.total_value.toLocaleString("vi-VN")}đ vượt ngưỡng{" "}
            {STOCK_TRANSFER_APPROVAL_THRESHOLD.toLocaleString("vi-VN")}đ (BR-58) — cần Quản lý vùng/Admin duyệt.
          </p>
        </div>
      )}

      {status === "REJECTED" && transfer.rejected_reason && (
        <div className="flex items-start gap-2 rounded-md border border-red-300 bg-red-50 px-4 py-3 text-sm text-red-800">
          <PackageX className="h-4 w-4 mt-0.5 shrink-0" />
          <p>Lý do từ chối: {transfer.rejected_reason}</p>
        </div>
      )}

      <div className="grid gap-6 lg:grid-cols-3">
        <div className="space-y-6 lg:col-span-2">
          <Card>
            <CardHeader>
              <CardTitle className="text-base">Danh sách thuốc/vật tư</CardTitle>
            </CardHeader>
            <CardContent className="p-0">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Thuốc</TableHead>
                    <TableHead>Số lô</TableHead>
                    <TableHead>HSD</TableHead>
                    <TableHead className="text-right">SL yêu cầu</TableHead>
                    <TableHead className="text-right">SL xuất</TableHead>
                    <TableHead className="text-right">SL nhận</TableHead>
                    <TableHead className="text-right">Đơn giá</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {transfer.items.map((item) => (
                    <TableRow key={item.id}>
                      <TableCell className="font-medium text-sm">{item.drug_name ?? item.drug_id}</TableCell>
                      <TableCell className="text-sm text-muted-foreground">{item.lot_no ?? "—"}</TableCell>
                      <TableCell className="text-sm text-muted-foreground">
                        {item.expiry_date ? format(new Date(item.expiry_date), "dd/MM/yyyy") : "—"}
                      </TableCell>
                      <TableCell className="text-right">{item.qty_requested}</TableCell>
                      <TableCell className="text-right">{item.qty_shipped}</TableCell>
                      <TableCell className="text-right">{item.qty_received}</TableCell>
                      <TableCell className="text-right">{item.unit_cost.toLocaleString("vi-VN")}đ</TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </CardContent>
          </Card>
        </div>

        <div className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle className="text-base">Thông tin phiếu</CardTitle>
            </CardHeader>
            <CardContent className="space-y-2 text-sm">
              <InfoRow label="Tổng giá trị" value={`${transfer.total_value.toLocaleString("vi-VN")}đ`} bold />
              <InfoRow label="Lý do" value={transfer.reason ?? "—"} />
              <Separator />
              <InfoRow label="Người tạo" value={transfer.requested_by ?? "—"} />
              <InfoRow
                label="Ngày tạo"
                value={transfer.requested_at ? format(new Date(transfer.requested_at), "dd/MM/yyyy HH:mm") : "—"}
              />
              {transfer.approved_by && (
                <>
                  <InfoRow label="Người duyệt" value={transfer.approved_by} />
                  <InfoRow
                    label="Ngày duyệt"
                    value={transfer.approved_at ? format(new Date(transfer.approved_at), "dd/MM/yyyy HH:mm") : "—"}
                  />
                </>
              )}
              {transfer.shipped_by && (
                <>
                  <InfoRow label="Người xuất" value={transfer.shipped_by} />
                  <InfoRow
                    label="Ngày xuất"
                    value={transfer.shipped_at ? format(new Date(transfer.shipped_at), "dd/MM/yyyy HH:mm") : "—"}
                  />
                </>
              )}
              {transfer.received_by && (
                <>
                  <InfoRow label="Người nhận" value={transfer.received_by} />
                  <InfoRow
                    label="Ngày nhận"
                    value={transfer.received_at ? format(new Date(transfer.received_at), "dd/MM/yyyy HH:mm") : "—"}
                  />
                </>
              )}
            </CardContent>
          </Card>
        </div>
      </div>

      {/* Dialog từ chối */}
      <Dialog open={rejectOpen} onOpenChange={setRejectOpen}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>Từ chối phiếu điều chuyển</DialogTitle>
            <DialogDescription>Vui lòng nhập lý do từ chối để dược sĩ chi nhánh gửi được biết</DialogDescription>
          </DialogHeader>
          <Textarea
            placeholder="Lý do từ chối..."
            value={rejectReason}
            onChange={(e) => setRejectReason(e.target.value)}
            rows={3}
          />
          <DialogFooter>
            <Button variant="outline" onClick={() => setRejectOpen(false)} disabled={reject.isPending}>
              Huỷ
            </Button>
            <Button variant="destructive" onClick={handleReject} disabled={reject.isPending}>
              Từ chối phiếu
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Dialog nhận hàng */}
      <Dialog open={receiveOpen} onOpenChange={setReceiveOpen}>
        <DialogContent className="sm:max-w-lg">
          <DialogHeader>
            <DialogTitle>Xác nhận nhận hàng</DialogTitle>
            <DialogDescription>
              Nhập số lượng thực nhận cho từng dòng. Nếu khác số lượng xuất, phiếu sẽ chuyển trạng thái "Nhận thiếu"
              và mở bản ghi chênh lệch để xử lý.
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-3 max-h-80 overflow-y-auto">
            {transfer.items.map((item) => (
              <div key={item.id} className="flex items-center justify-between gap-3">
                <div className="min-w-0 flex-1">
                  <p className="text-sm font-medium truncate">{item.drug_name ?? item.drug_id}</p>
                  <p className="text-xs text-muted-foreground">Đã xuất: {item.qty_shipped}</p>
                </div>
                <Input
                  type="number"
                  step="0.5"
                  className="w-28"
                  value={receiveQty[item.id] ?? item.qty_shipped}
                  onChange={(e) =>
                    setReceiveQty((prev) => ({ ...prev, [item.id]: Number(e.target.value) }))
                  }
                />
              </div>
            ))}
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setReceiveOpen(false)} disabled={receive.isPending || partialReceive.isPending}>
              Huỷ
            </Button>
            <Button onClick={handleConfirmReceive} disabled={receive.isPending || partialReceive.isPending}>
              Xác nhận nhận hàng
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <ConfirmDialog
        open={cancelOpen}
        onOpenChange={setCancelOpen}
        title="Huỷ phiếu điều chuyển"
        description="Bạn có chắc muốn huỷ phiếu điều chuyển này? Hành động không thể hoàn tác."
        confirmLabel="Huỷ phiếu"
        variant="destructive"
        isLoading={cancel.isPending}
        onConfirm={handleCancel}
      />
    </div>
  );
}

function InfoRow({ label, value, bold }: { label: string; value: string; bold?: boolean }) {
  return (
    <div className="flex items-center justify-between">
      <span className="text-muted-foreground">{label}</span>
      <span className={bold ? "font-bold" : ""}>{value}</span>
    </div>
  );
}
