"use client";

import { useState } from "react";
import { ChevronDown, ChevronRight, Printer, Send, Pencil, BadgePercent } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { ConfirmDialog } from "@/components/domain/ConfirmDialog";
import { ClsRoundPaymentBadge } from "./ClsRoundPaymentBadge";
import { ClsOrderItemTable, type ClsOrderItemRow } from "./ClsOrderItemTable";
import { ClsRoundEditDialog } from "./ClsRoundEditDialog";
import { formatVnd, formatVnDateTime } from "@/lib/utils/encounter-format";
import type { ClsRound } from "@/lib/api/cls-rounds";
import type { LabOrderRequest, RadOrderRequest } from "@/lib/api/types";

export interface ClsRoundCardProps {
  round: ClsRound;
  defaultOpen?: boolean;
  canEdit: boolean;
  isPending?: boolean;
  onPrint: () => void;
  onSubmit?: () => void;
  onCancel?: () => void;
  onPay?: () => void;
  onWaive?: (reason: string) => void;
  // BM-14: "Dieu chinh" dot - them/bot tung dich vu, chi kha dung khi dot chua thanh toan.
  onAddLab?: (tests: LabOrderRequest[]) => void;
  onAddRad?: (orders: RadOrderRequest[]) => void;
  onRemoveItem?: (id: string, kind: "LAB" | "RAD") => void;
  isAddingItem?: boolean;
  isRemovingItem?: boolean;
}

export function ClsRoundCard({
  round,
  defaultOpen = false,
  canEdit,
  isPending,
  onPrint,
  onSubmit,
  onCancel,
  onPay,
  onWaive,
  onAddLab,
  onAddRad,
  onRemoveItem,
  isAddingItem,
  isRemovingItem,
}: ClsRoundCardProps) {
  const [open, setOpen] = useState(defaultOpen);
  const [editOpen, setEditOpen] = useState(false);
  const [confirmWaive, setConfirmWaive] = useState(false);

  const items: ClsOrderItemRow[] = [
    ...(round.lab_orders ?? []).map((o) => ({ ...o, kind: "LAB" as const })),
    ...(round.rad_orders ?? []).map((o) => ({ ...o, kind: "RAD" as const })),
  ];
  const roundLabel = `Đợt #${round.round_no}`;
  const isCancelled = round.status === "CANCELLED";
  const isPaid = round.payment_status === "PAID" || round.payment_status === "WAIVED";
  const canMutate = canEdit && !isCancelled && !isPaid && round.status === "OPEN";
  // Sau khi chốt đợt (SUBMITTED) mà chưa thanh toán -> cho phép thu tiền / miễn phí
  // để mở khoá bước nhập kết quả XN (gate CLS_ORDER_UNPAID).
  const canPay =
    canEdit && !isCancelled && round.payment_status === "UNPAID" && round.status === "SUBMITTED";

  return (
    <Card>
      <CardContent className="p-0">
        <div className="flex flex-wrap items-center gap-2 p-3">
          <button
            type="button"
            onClick={() => setOpen((v) => !v)}
            aria-expanded={open}
            className="flex min-h-[44px] flex-1 items-center gap-2 text-left focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[color:var(--focus-ring)]"
          >
            {open ? (
              <ChevronDown className="h-4 w-4 shrink-0" aria-hidden="true" />
            ) : (
              <ChevronRight className="h-4 w-4 shrink-0" aria-hidden="true" />
            )}
            <span className="text-sm font-semibold">{roundLabel}</span>
            <span className="text-xs text-muted-foreground">
              {formatVnDateTime(round.created_at)} · {items.length} dịch vụ
            </span>
          </button>

          <ClsRoundPaymentBadge status={round.payment_status} cancelled={isCancelled} />

          <span className="font-mono tabular-nums text-sm font-semibold">
            {formatVnd(round.total_amount)} <span aria-label="đồng">₫</span>
          </span>

          <Button
            variant="ghost"
            size="sm"
            className="min-h-[44px] gap-1"
            onClick={onPrint}
            aria-label={`In phiếu chỉ định ${roundLabel}`}
          >
            <Printer className="h-4 w-4" aria-hidden="true" />
            In phiếu
          </Button>

          {canMutate && onSubmit && (
            <Button
              variant="outline"
              size="sm"
              className="min-h-[44px] gap-1"
              onClick={onSubmit}
              disabled={isPending}
            >
              <Send className="h-4 w-4" aria-hidden="true" />
              Chốt đợt
            </Button>
          )}

          {/* BM-16: bo nut "Thu tien" khoi man Kham benh (role Bac si) - day la nghiep vu
              cua Thu ngan. Sau khi Bac si "Chot dot", dot chuyen sang man Thu ngan de xu
              ly thanh toan (khong con thu tien tai day nua). Giu nguyen nut "Mien phi" -
              PO xac nhan van giu ca 2 co che (mien phi ca dot + khong thu phi tung dich
              vu luc tao dot) song song. */}
          {canPay && onWaive && (
            <Button
              variant="outline"
              size="sm"
              className="min-h-[44px] gap-1"
              onClick={() => setConfirmWaive(true)}
              disabled={isPending}
            >
              <BadgePercent className="h-4 w-4" aria-hidden="true" />
              Miễn phí
            </Button>
          )}

          {/* BM-14: doi nut "Xoa" (huy ca dot) -> "Dieu chinh" (them/bot tung dich vu).
              Huy ca dot (BM-15) van con nhung chuyen thanh hanh dong phu ben trong dialog
              Dieu chinh, khong con la nut chinh o toolbar. An hoan toan khi da thanh toan/
              huy (khong con canMutate) de dam bao toan ven du lieu - bac si van xem lai
              duoc thong tin dot (dich vu, gia, trang thai) qua bang ben duoi, chi khong
              thao tac chinh sua duoc nua. */}
          {canMutate && (
            <Button
              variant="ghost"
              size="sm"
              className="min-h-[44px] gap-1"
              onClick={() => setEditOpen(true)}
              disabled={isPending}
            >
              <Pencil className="h-4 w-4" aria-hidden="true" />
              Điều chỉnh
            </Button>
          )}
        </div>

        {open && (
          <div className="border-t border-border">
            {items.length === 0 ? (
              <p className="p-4 text-sm text-muted-foreground">Đợt này chưa có dịch vụ nào.</p>
            ) : (
              <ClsOrderItemTable items={items} roundLabel={roundLabel} />
            )}
          </div>
        )}
      </CardContent>

      {editOpen && (
        <ClsRoundEditDialog
          open={editOpen}
          onOpenChange={setEditOpen}
          round={round}
          isAdding={isAddingItem}
          isRemoving={isRemovingItem}
          isCancelling={isPending}
          onAddLab={(tests) => onAddLab?.(tests)}
          onAddRad={(orders) => onAddRad?.(orders)}
          onRemove={(id, kind) => onRemoveItem?.(id, kind)}
          onCancelRound={onCancel ? () => onCancel() : undefined}
        />
      )}

      <ConfirmDialog
        open={confirmWaive}
        onOpenChange={setConfirmWaive}
        title={`Miễn phí đợt chỉ định #${round.round_no}?`}
        description={`Đợt ${formatVnd(round.total_amount)} ₫ sẽ được đánh dấu miễn phí và mở khoá bước nhập kết quả, không thu tiền bệnh nhân.`}
        confirmLabel="Miễn phí đợt"
        cancelLabel="Huỷ"
        isLoading={isPending}
        onConfirm={() => {
          setConfirmWaive(false);
          onWaive?.("Miễn phí theo chỉ định lâm sàng");
        }}
      />
    </Card>
  );
}
